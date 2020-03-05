/*
 * Copyright 2020 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SeaPeaYou.PeaPdf
{
    partial class Renderer
    {

        public readonly SKImage SKImage;

        PDF pdf;
        GraphicsState gs = new GraphicsState
        {
            StrokePaint = new SKPaint { Style = SKPaintStyle.Stroke, IsAntialias = true },
            OtherPaint = new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = true, TextEncoding = SKTextEncoding.Utf16, TextSize = 1 },
            TextStateMatrix = SKMatrix.MakeIdentity()
        };
        PdfDict _resources;
        SKCanvas canvas;
        SKMatrix textBaseMatrix, textMatrix, textLineMatrix, flipOutMatrix = SKMatrix.MakeScale(1, -1), flipInMatrix;
        Stack<GraphicsState> graphicsStateStack = new Stack<GraphicsState>();
        Dictionary<PdfName, Font> fontDict = new Dictionary<PdfName, Font>();

        public Renderer(PDF pdf, PdfDict page, float scale)
        {
            this.pdf = pdf;
            var contentBytes = new List<byte>();
            var contents = page["Contents"];
            var contentsArr = contents.AsArray<PdfStream>();
            foreach (var pdfStream in contentsArr)
            {
                contentBytes.AddRange(pdfStream.GetBytes());
            }

            //File.WriteAllBytes(@"d:\tmp\content-latest.txt", contentBytes.ToArray());

            _resources = (PdfDict)pdf.GetPageObj(page, "Resources");

            var mediaBox = new PdfRectangle((PdfArray)pdf.GetPageObj(page, "MediaBox"));
            int pageWidth = (int)(mediaBox.UpperRightX - mediaBox.LowerLeftX), pageHeight = (int)(mediaBox.UpperRightY - mediaBox.LowerLeftY);

            var canvasInfo = new SKImageInfo((int)(pageWidth * scale), (int)(pageHeight * scale));
            var surface = SKSurface.Create(canvasInfo);
            canvas = surface.Canvas;
            canvas.DrawColor(SKColors.White);


            SKMatrix _baseMatrix = SKMatrix.MakeIdentity();
            flipInMatrix = Utils.MatrixConcat(SKMatrix.MakeTranslation(0, 1), flipOutMatrix);

            _baseMatrix.ScaleX = scale;
            _baseMatrix.ScaleY = -scale;
            _baseMatrix.TransY = pageHeight * scale;
            if (mediaBox.LowerLeftY != 0)
                SKMatrix.PreConcat(ref _baseMatrix, SKMatrix.MakeTranslation(0, (float)-mediaBox.LowerLeftY));
            canvas.SetMatrix(_baseMatrix);

            var cropBoxArr = (PdfArray)page["CropBox"];
            var cropBox = cropBoxArr != null ? new PdfRectangle(cropBoxArr) : mediaBox;
            canvas.ClipRect(new SKRect((float)cropBox.LowerLeftX, (float)cropBox.UpperRightY, (float)cropBox.UpperRightX, (float)cropBox.LowerLeftY));
            //int biNum = 0, keywordIX = 0;
            new DrawContext(this, contentBytes.ToArray(), _resources);

            var annots = (PdfArray)page["Annots"];
            if (annots != null)
            {
                foreach (PdfDict annot in annots)
                {
                    var ap = annot["AP"];
                    if (ap == null)
                        continue;
                    var normalAP = ap.As<PdfDict>()["N"].As<PdfStream>();
                    if (normalAP == null)
                        continue;
                    var rect = new PdfRectangle((PdfArray)annot["Rect"]);
                    var bBox = (PdfArray)normalAP.Dict["BBox"];
                    var corners = new SKPoint[] {
                        new SKPoint((float)bBox[0], (float)bBox[1]),
                        new SKPoint((float)bBox[2], (float)bBox[1]),
                        new SKPoint((float)bBox[2], (float)bBox[3]),
                        new SKPoint((float)bBox[0], (float)bBox[3]),
                    };
                    var matrix = MatrixFromArray((PdfArray)normalAP.Dict["Matrix"]);
                    var transCorners = corners.Select(x => matrix.MapPoint(x)).ToArray();
                    float minX = transCorners.Min(x => x.X), maxX = transCorners.Max(x => x.X), minY = transCorners.Min(x => x.Y), maxY = transCorners.Max(x => x.Y),
                        width = maxX - minX, height = maxY - minY, rectWidth = rect.UpperRightX - rect.LowerLeftX, rectHeight = rect.UpperRightY - rect.LowerLeftY;
                    var finalMatrix = Utils.MatrixConcat(SKMatrix.MakeTranslation(-minX, -minY), SKMatrix.MakeScale(rectWidth / width, rectHeight / height),
                        SKMatrix.MakeTranslation(rect.LowerLeftX, rect.LowerLeftY));
                    SKMatrix.PreConcat(ref finalMatrix, matrix);
                    SKMatrix.PostConcat(ref finalMatrix, _baseMatrix);
                    canvas.SetMatrix(finalMatrix);
                    new DrawContext(this, normalAP.GetBytes(), normalAP.Dict["Resources"].As<PdfDict>() ?? _resources);
                }
            }

            SKImage = surface.Snapshot();
        }

        static SKMatrix MatrixFromArray(IEnumerable<PdfObject> arr)
        {
            var nums = arr.Select(x => (float)x).ToList();
            var matrix = SKMatrix.MakeIdentity();
            matrix.ScaleX = nums[0];
            matrix.SkewY = nums[1];
            matrix.SkewX = nums[2];
            matrix.ScaleY = nums[3];
            matrix.TransX = nums[4];
            matrix.TransY = nums[5];
            return matrix;
        }

        ColorSpace GetColorSpace(PdfName name)
        {
            if (colorSpaceDict.TryGetValue(name, out var cs))
                return cs;
            var csObj = _resources["ColorSpace"].As<PdfDict>()[name];
            if (csObj == null)
                return ColorSpace.DeviceRGB;
            if (csObj is PdfArray arr)
            {
                name = arr[0] as PdfName;
                var nameStr = name.ToString();
                if (nameStr == "ICCBased")
                {
                    var stream = (PdfStream)arr[1];
                    var N = (int)stream.Dict["N"];
                    switch (N)
                    {
                        case 1: return ColorSpace.DeviceGray;
                        case 3: return ColorSpace.DeviceRGB;
                        case 4: return ColorSpace.DeviceCMYK;
                        default: throw new NotSupportedException("N");
                    }
                }
                else if (nameStr == "Separation")
                {
                    var separationName = arr[1].ToString();
                    switch (separationName)
                    {
                        case "All": return ColorSpace.DeviceGray;
                        case "None": return ColorSpace.Blank;
                        default: throw new NotImplementedException("Non-special separation.");
                    }
                }
            }
            else
                name = csObj as PdfName;
            if (name == null)
                return ColorSpace.DeviceRGB;
            colorSpaceDict.TryGetValue(name, out cs);
            return cs;
        }

        class GraphicsState
        {
            public SKPaint StrokePaint;
            public SKPaint OtherPaint;
            public Font TextFont;
            public float TextTfs = 1, TextTth = 1, TextTrise = 0, TextWordSpacing = 0, TextCharSpacing = 0, TextLeading = 0;
            public ColorSpace StrokeColorSpace, OtherColorSpace;
            public SKMatrix TextStateMatrix;

            public GraphicsState Clone()
            {
                var n = (GraphicsState)MemberwiseClone();
                n.StrokePaint = StrokePaint.Clone();
                n.OtherPaint = OtherPaint.Clone();
                return n;
            }
        }

        class Font
        {
            public SKTypeface Typeface;
            public PdfDict Type3Font;
            public CharEncoding Encoding;
            public Dictionary<byte, PdfName> Code2Names;
            public bool Type0;
            public List<float> Widths;
            public int? FirstChar;
            public byte[] CIDToGID;
            public PdfName FontName;
            public List<int> CodeMap;
        }

        enum ColorSpace { DeviceRGB, DeviceGray, DeviceCMYK, Blank }
        Dictionary<PdfName, ColorSpace> colorSpaceDict = new Dictionary<PdfName, ColorSpace>
        {
            { "DeviceRGB", ColorSpace.DeviceRGB },
            { "DeviceGray", ColorSpace.DeviceGray },
            { "DeviceCMYK", ColorSpace.DeviceCMYK },
            { "Pattern", ColorSpace.Blank },
        };

        enum FontDescriptorFlags { FixedPitch = 0b1, Serif = 0b10, Symbolic = 0b100, Script = 0b1000, NonSymbolic = 0b10_0000, Italic = 0b100_0000, AllCap = 0x10000, SmallCap = 0x20000, ForceBold = 0x40000 };

    }
}
