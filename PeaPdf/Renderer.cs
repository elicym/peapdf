/*
 * Copyright 2021 Elliott Cymerman
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
        W.ResourceDictionary _resources;
        SKCanvas canvas;
        SKMatrix textBaseMatrix, textMatrix, textLineMatrix, flipOutMatrix = SKMatrix.MakeScale(1, -1), flipInMatrix;
        Stack<GraphicsState> graphicsStateStack = new Stack<GraphicsState>();
        Dictionary<string, Font> fontDict = new Dictionary<string, Font>();

        public Renderer(PDF pdf, Page page, float scale)
        {
            this.pdf = pdf;

            //File.WriteAllBytes(@"d:\tmp\content-latest.txt", contentBytes.ToArray());

            _resources = page.Resources;

            var mediaBox = new W.Rectangle((PdfArray)pdf.GetPageObj(page.Dict, "MediaBox"));
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
                _baseMatrix= _baseMatrix.PreConcat(SKMatrix.MakeTranslation(0, (float)-mediaBox.LowerLeftY));
            canvas.SetMatrix(_baseMatrix);

            var cropBoxArr = (PdfArray)page.Dict["CropBox"];
            var cropBox = cropBoxArr != null ? new W.Rectangle(cropBoxArr) : mediaBox;
            canvas.ClipRect(new SKRect((float)cropBox.LowerLeftX, (float)cropBox.UpperRightY, (float)cropBox.UpperRightX, (float)cropBox.LowerLeftY));
            //int biNum = 0, keywordIX = 0;
            new DrawContext(this, page.GetContents());

            var annots = page.GetAnnots();
            if (annots != null && false)
            {
                foreach (W.Annotation annot in annots)
                {
                    var ap = annot.AP;
                    if (ap == null)
                        continue;
                    if (ap.N == null)
                        continue;
                    var rect = ap.N.GetFormXObject().BBox;
                    var corners = new SKPoint[] {
                        new SKPoint(rect.LowerLeftX, rect.LowerLeftY),
                        new SKPoint(rect.UpperRightX, rect.LowerLeftY),
                        new SKPoint(rect.UpperRightX, rect.UpperRightY),
                        new SKPoint(rect.LowerLeftX, rect.UpperRightY),
                    };
                    var matrixArr = (PdfArray)(ap.N.GetFormXObject().PdfStream.Dict["Matrix"]);
                    var matrix = Utils.MatrixFromArray(matrixArr);
                    var transCorners = corners.Select(x => matrix.MapPoint(x)).ToArray();
                    float minX = transCorners.Min(x => x.X), maxX = transCorners.Max(x => x.X), minY = transCorners.Min(x => x.Y), maxY = transCorners.Max(x => x.Y),
                        width = maxX - minX, height = maxY - minY, rectWidth = rect.UpperRightX - rect.LowerLeftX, rectHeight = rect.UpperRightY - rect.LowerLeftY;
                    var finalMatrix = Utils.MatrixConcat(SKMatrix.MakeTranslation(-minX, -minY), SKMatrix.MakeScale(rectWidth / width, rectHeight / height),
                        SKMatrix.MakeTranslation(rect.LowerLeftX, rect.LowerLeftY));
                    finalMatrix= finalMatrix.PreConcat(matrix);
                    finalMatrix= finalMatrix.PostConcat(_baseMatrix);
                    canvas.SetMatrix(finalMatrix);
                    new DrawContext(this, ap.N.GetFormXObject());
                }
            }

            SKImage = surface.Snapshot();
        }

        ColorSpace GetColorSpace(string name)
        {
            if (colorSpaceDict.TryGetValue(name, out var cs))
                return cs;
            var csObj = _resources.PdfDict["ColorSpace"].As<PdfDict>()[name];
            if (csObj == null)
                return ColorSpace.DeviceRGB;
            if (csObj is PdfArray arr)
            {
                name = arr[0].As<PdfName>().String;
                if (name == "ICCBased")
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
                else if (name == "Separation")
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
                name = csObj.As<PdfName>().String;
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

        Dictionary<string, ColorSpace> colorSpaceDict = new Dictionary<string, ColorSpace>
        {
            { "DeviceRGB", ColorSpace.DeviceRGB },
            { "DeviceGray", ColorSpace.DeviceGray },
            { "DeviceCMYK", ColorSpace.DeviceCMYK },
            { "Pattern", ColorSpace.Blank },
        };


    }
}
