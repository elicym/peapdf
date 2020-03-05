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
        class DrawContext
        {

            public DrawContext(Renderer renderer, byte[] bytes, PdfDict resources)
            {
                this.renderer = renderer;
                this.resources = resources;
                //File.WriteAllBytes(@"d:\tmp\content-latest-inner.txt", bytes);

                fParse = new FParse(bytes);

                while (true)
                {
                    fParse.SkipWhiteSpace();
                    if (fParse.AtEnd)
                        break;
                    var b = fParse.PeekByte;
                    if ((b >= 'a' && b <= 'z') || (b >= 'A' && b <= 'Z') || b == '\'' || b == '"')
                    {
                        //if (++loopC > 5290)
                        //    goto after;
                        var keyword = fParse.ReadStringUntilDelimiter();
                        DoKeyword(keyword);
                        operands.Clear();
                        //if (++keywordIX == 5)
                        //    break;
                        //Debug.WriteLine($"{sw.ElapsedMilliseconds} {keyword}");
                        //sw.Restart();
                    }
                    else
                    {
                        operands.Add(fParse.ReadPdfObject(null));
                    }
                }
            }

            Renderer renderer;
            List<PdfObject> operands = new List<PdfObject>();
            PdfDict resources;
            SKPath curPath = new SKPath();
            FParse fParse;

            void DoKeyword(string keyword)
            {
                switch (keyword)
                {
                    //graphics state
                    case "q":
                        renderer.graphicsStateStack.Push(renderer.gs.Clone());
                        renderer.canvas.Save();
                        break;
                    case "Q":
                        if (renderer.graphicsStateStack.Count > 0)
                        {
                            renderer.gs = renderer.graphicsStateStack.Pop();
                            renderer.canvas.Restore();
                        }
                        break;
                    case "gs":
                        {
                            var extGState = (PdfDict)resources["ExtGState"];
                            var prms = (PdfDict)extGState[(PdfName)operands[0]];
                            foreach (var prm in prms)
                            {
                                switch (prm.key)
                                {
                                    case "LW":
                                        renderer.gs.StrokePaint.StrokeWidth = (float)prm.value;
                                        break;
                                    case "LC":
                                        renderer.gs.StrokePaint.StrokeCap = (SKStrokeCap)(int)prm.value;
                                        break;
                                    case "LJ":
                                        renderer.gs.StrokePaint.StrokeJoin = (SKStrokeJoin)(int)prm.value;
                                        break;
                                    case "ML":
                                        renderer.gs.StrokePaint.StrokeMiter = (float)prm.value;
                                        break;
                                    case "D":
                                        {
                                            var arr = (PdfArray)prm.value;
                                            var dashArray = (PdfArray)arr[0];
                                            renderer.gs.StrokePaint.PathEffect = SKPathEffect.CreateDash(dashArray.Select(x => (float)x).ToArray(), (float)arr[1]);
                                            break;
                                        }
                                    case "CA":
                                        renderer.gs.StrokePaint.ColorFilter = SKColorFilter.CreateBlendMode(SKColors.White.WithAlpha((byte)((float)prm.value * 255)), SKBlendMode.DstIn);
                                        break;
                                    case "ca":
                                        renderer.gs.OtherPaint.ColorFilter = SKColorFilter.CreateBlendMode(SKColors.White.WithAlpha((byte)((float)prm.value * 255)), SKBlendMode.DstIn);
                                        break;
                                    case "RI":
                                    case "OP":
                                    case "Font":
                                    case "BG":
                                    case "BG2":
                                    case "UCR":
                                    case "UCR2":
                                    case "TR":
                                    case "TR2":
                                    case "HT":
                                    case "FL":
                                    case "SM":
                                    case "SA":
                                    case "BM":
                                    case "SMask":
                                    case "AIS":
                                    case "TK":
                                        break;

                                }
                            }
                            break;
                        }
                    case "M":
                        renderer.gs.StrokePaint.StrokeMiter = (float)operands[0];
                        break;
                    case "cm":
                        renderer.canvas.SetMatrix(Utils.MatrixConcat(renderer.canvas.TotalMatrix, MatrixFromOperands()));
                        break;
                    case "w":
                        renderer.gs.StrokePaint.StrokeWidth = (float)operands[0];
                        break;
                    case "J":
                        renderer.gs.StrokePaint.StrokeJoin = (SKStrokeJoin)(int)operands[0];
                        break;
                    case "j":
                        renderer.gs.StrokePaint.StrokeCap = (SKStrokeCap)(int)operands[0];
                        break;
                    case "d":
                        {
                            var dashArray = operands[0].As<PdfArray>().Select(x => (float)x).ToArray();
                            var dashPhase = (float)operands[1];
                            renderer.gs.StrokePaint.PathEffect = SKPathEffect.CreateDash(dashArray, dashPhase);
                            break;
                        }
                    case "i":
                        break;
                    //clipping paths
                    case "W":
                        curPath.FillType = SKPathFillType.Winding;
                        renderer.canvas.ClipPath(curPath);
                        break;
                    case "W*":
                        curPath.FillType = SKPathFillType.EvenOdd;
                        renderer.canvas.ClipPath(curPath);
                        break;
                    //path construction
                    case "re":
                        {
                            var nums = operands.Select(o => (float)o).ToList();
                            float x = nums[0], y = nums[1], width = nums[2], height = nums[3];
                            curPath.AddRect(new SKRect(x, y + height, x + width, y));
                            break;
                        }
                    case "m":
                        curPath.MoveTo((float)operands[0], (float)operands[1]);
                        break;
                    case "l":
                        curPath.LineTo((float)operands[0], (float)operands[1]);
                        break;
                    case "c":
                        {
                            var nums = operands.Select(x => (float)x).ToList();
                            curPath.CubicTo(nums[0], nums[1], nums[2], nums[3], nums[4], nums[5]);
                            break;
                        }
                    case "v":
                        {
                            var nums = operands.Select(x => (float)x).ToList();
                            curPath.CubicTo(curPath.LastPoint.X, curPath.LastPoint.Y, nums[0], nums[1], nums[2], nums[3]);
                            break;
                        }
                    case "y":
                        {
                            var nums = operands.Select(x => (float)x).ToList();
                            curPath.CubicTo(nums[0], nums[1], nums[2], nums[3], nums[2], nums[3]);
                            break;
                        }
                    case "h":
                        curPath.Close();
                        break;
                    //path painting
                    case "n":
                        EndPath();
                        break;
                    case "S":
                        renderer.canvas.DrawPath(curPath, renderer.gs.StrokePaint);
                        EndPath();
                        break;
                    case "f":
                    case "F":
                        curPath.FillType = SKPathFillType.Winding;
                        renderer.canvas.DrawPath(curPath, renderer.gs.OtherPaint);
                        EndPath();
                        break;
                    case "f*":
                        curPath.FillType = SKPathFillType.EvenOdd;
                        renderer.canvas.DrawPath(curPath, renderer.gs.OtherPaint);
                        EndPath();
                        break;
                    case "B":
                        curPath.FillType = SKPathFillType.Winding;
                        renderer.canvas.DrawPath(curPath, renderer.gs.OtherPaint);
                        renderer.canvas.DrawPath(curPath, renderer.gs.StrokePaint);
                        EndPath();
                        break;
                    case "B*":
                        curPath.FillType = SKPathFillType.EvenOdd;
                        renderer.canvas.DrawPath(curPath, renderer.gs.OtherPaint);
                        renderer.canvas.DrawPath(curPath, renderer.gs.StrokePaint);
                        EndPath();
                        break;
                    //color
                    case "CS":
                        renderer.gs.StrokeColorSpace = renderer.GetColorSpace((PdfName)operands[0]);
                        renderer.gs.StrokePaint.Color = SKColors.Black;
                        break;
                    case "cs":
                        renderer.gs.OtherColorSpace = renderer.GetColorSpace((PdfName)operands[0]);
                        renderer.gs.OtherPaint.Color = SKColors.Black;
                        break;
                    case "SC":
                    case "SCN":
                        switch (renderer.gs.StrokeColorSpace)
                        {
                            case ColorSpace.DeviceRGB:
                                renderer.gs.StrokePaint.Color = RGBFromOperands();
                                break;
                            case ColorSpace.DeviceGray:
                                renderer.gs.StrokePaint.Color = GrayFromOperands();
                                break;
                            case ColorSpace.DeviceCMYK:
                                renderer.gs.StrokePaint.Color = CMYKFromOperands();
                                break;
                            case ColorSpace.Blank:
                                renderer.gs.StrokePaint.Color = SKColors.Transparent;
                                break;
                        }
                        break;
                    case "sc":
                    case "scn":
                        switch (renderer.gs.OtherColorSpace)
                        {
                            case ColorSpace.DeviceRGB:
                                renderer.gs.OtherPaint.Color = RGBFromOperands();
                                break;
                            case ColorSpace.DeviceGray:
                                renderer.gs.OtherPaint.Color = GrayFromOperands();
                                break;
                            case ColorSpace.DeviceCMYK:
                                renderer.gs.OtherPaint.Color = CMYKFromOperands();
                                break;
                            case ColorSpace.Blank:
                                renderer.gs.OtherPaint.Color = SKColors.Transparent;
                                break;
                        }
                        break;
                    case "K":
                        renderer.gs.StrokeColorSpace = ColorSpace.DeviceCMYK;
                        renderer.gs.StrokePaint.Color = CMYKFromOperands();
                        break;
                    case "k":
                        renderer.gs.OtherColorSpace = ColorSpace.DeviceCMYK;
                        renderer.gs.OtherPaint.Color = CMYKFromOperands();
                        break;
                    case "G":
                        renderer.gs.StrokeColorSpace = ColorSpace.DeviceGray;
                        renderer.gs.StrokePaint.Color = GrayFromOperands();
                        break;
                    case "g":
                        renderer.gs.OtherColorSpace = ColorSpace.DeviceGray;
                        renderer.gs.OtherPaint.Color = GrayFromOperands();
                        break;
                    case "RG":
                        renderer.gs.StrokeColorSpace = ColorSpace.DeviceRGB;
                        renderer.gs.StrokePaint.Color = RGBFromOperands();
                        break;
                    case "rg":
                        renderer.gs.OtherColorSpace = ColorSpace.DeviceRGB;
                        renderer.gs.OtherPaint.Color = RGBFromOperands();
                        break;
                    //marked content
                    case "BDC":
                    case "EMC":
                        break;
                    //text
                    case "BT":
                        {
                            renderer.canvas.Save();
                            renderer.textBaseMatrix = renderer.canvas.TotalMatrix;
                            renderer.textMatrix = renderer.textLineMatrix = SKMatrix.MakeIdentity();
                            break;
                        }
                    case "ET":
                        renderer.canvas.Restore();
                        break;
                    case "Tf":
                        {
                            renderer.gs.TextTfs = (float)operands[1];
                            SetTextStateMatrix();

                            var fontName = (PdfName)operands[0];
                            if (!renderer.fontDict.TryGetValue(fontName, out renderer.gs.TextFont))
                            {
                                renderer.fontDict.Add(fontName, renderer.gs.TextFont = new Font { FontName = fontName });
                                renderer.gs.TextFont.Encoding = CharEncoding.StdEncoding;

                                var fontObj = resources["Font"].As<PdfDict>()[fontName].As<PdfDict>();
                                var subtype = fontObj["Subtype"].ToString();
                                var _fontObj = subtype == "Type0" ? fontObj["DescendantFonts"].As<PdfArray>()[0].As<PdfDict>() : fontObj;

                                var encodingObj = fontObj["Encoding"];
                                if (encodingObj != null)
                                {
                                    if (encodingObj is PdfName encodingName)
                                    {
                                        renderer.gs.TextFont.Encoding = CharEncoding.FromName(encodingName.ToString());
                                    }
                                    else if (encodingObj is PdfDict encodingDict)
                                    {
                                        var baseEncoding = (PdfName)encodingDict["BaseEncoding"];
                                        if (baseEncoding != null)
                                            renderer.gs.TextFont.Encoding = CharEncoding.FromName(baseEncoding.ToString());
                                        var differences = (PdfArray)encodingDict["Differences"];
                                        if (differences != null)
                                        {
                                            renderer.gs.TextFont.Code2Names = new Dictionary<byte, PdfName>();
                                            int code = 0;
                                            foreach (var item in differences)
                                            {
                                                if (item is PdfNumeric)
                                                    code = (int)item;
                                                else
                                                {
                                                    renderer.gs.TextFont.Code2Names.Add((byte)code, (PdfName)item);
                                                    code++;
                                                }
                                            }
                                        }
                                    }
                                }

                                var widthsObj = (PdfArray)fontObj["Widths"];
                                if (widthsObj != null)
                                {
                                    var divisor = subtype == "Type3" ? 1 : 1000;
                                    renderer.gs.TextFont.Widths = widthsObj.Select(x => (float)x / divisor).ToList();
                                }
                                renderer.gs.TextFont.FirstChar = (int?)fontObj["FirstChar"];
                                var fontDescriptor = _fontObj["FontDescriptor"]?.As<PdfDict>();

                                if (subtype == "Type3")
                                {
                                    renderer.gs.TextFont.Type3Font = fontObj;
                                }
                                else if (renderer.gs.TextFont.Typeface == null)
                                {
                                    if (fontDescriptor != null)
                                    {
                                        OTFFont otfFont = null;
                                        var fontFile = (PdfStream)fontDescriptor["FontFile2"];
                                        if (fontFile != null)
                                        {
                                            var fontBytes = fontFile.GetBytes();
                                            otfFont = OTFFont.FromTT(fontBytes);
                                            if (subtype == "Type0")
                                                otfFont.AddIdentityCMap();
                                        }
                                        else
                                        {
                                            fontFile = (PdfStream)fontDescriptor["FontFile3"];
                                            if (fontFile != null)
                                            {
                                                var fontBytes = fontFile.GetBytes();
                                                otfFont = OTFFont.FromCFF(fontBytes, renderer.gs.TextFont.Encoding, renderer.gs.TextFont.Code2Names);
                                                renderer.gs.TextFont.Code2Names = null;
                                                renderer.gs.TextFont.Encoding = null; //since we created the cmap
                                            }
                                        }
                                        if (otfFont != null)
                                        {

                                            if (subtype == "Type0")
                                            {
                                                renderer.gs.TextFont.Encoding = null;
                                                renderer.gs.TextFont.Type0 = true;
                                                var cid2GIDObj = _fontObj["CIDToGIDMap"] as PdfStream;
                                                if (cid2GIDObj != null)
                                                {
                                                    renderer.gs.TextFont.CIDToGID = cid2GIDObj.GetBytes();
                                                }
                                                else
                                                {
                                                    var cidSetObj = _fontObj["CIDSet"];
                                                    if (cidSetObj != null)
                                                    {
                                                        renderer.gs.TextFont.CodeMap = new List<int>();
                                                        var cidSetBytes = cidSetObj.As<PdfStream>().GetBytes();
                                                        var bitStream = new BitReader(new ByteReader(cidSetBytes));
                                                        for (int i = 0; i < cidSetBytes.Length * 8; i++)
                                                        {
                                                            if (bitStream.ReadBit())
                                                                renderer.gs.TextFont.CodeMap.Add(i);
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                if (otfFont.NoUnicodeCMap)
                                                {
                                                    renderer.gs.TextFont.Encoding = null;
                                                }
                                            }
                                            MemoryStream ms = new MemoryStream();
                                            otfFont.Write(ms);
                                            ms.Seek(0, SeekOrigin.Begin);
                                            renderer.gs.TextFont.Typeface = SKTypeface.FromStream(ms);
                                        }
                                    }

                                }
                                var baseFont = _fontObj["BaseFont"]?.ToString();
                                if (renderer.gs.TextFont.Typeface == null)
                                {
                                    if (baseFont != null)
                                    {
                                        switch (baseFont)
                                        {
                                            case "Helvetica":
                                                renderer.gs.TextFont.Typeface = getHelvetica(SKFontStyle.Normal);
                                                break;
                                            case "Helvetica-Bold":
                                                renderer.gs.TextFont.Typeface = getHelvetica(SKFontStyle.Bold);
                                                break;
                                            case "Helvetica-Oblique":
                                                renderer.gs.TextFont.Typeface = getHelvetica(SKFontStyle.Italic);
                                                break;
                                            case "Helvetica-BoldOblique":
                                                renderer.gs.TextFont.Typeface = getHelvetica(SKFontStyle.BoldItalic);
                                                break;
                                            case "Times-Roman":
                                                renderer.gs.TextFont.Typeface = SKTypeface.FromFamilyName("Times New Roman", SKFontStyle.Normal);
                                                break;
                                            case "Times-Bold":
                                                renderer.gs.TextFont.Typeface = SKTypeface.FromFamilyName("Times New Roman", SKFontStyle.Bold);
                                                break;
                                            case "Times-Italic":
                                                renderer.gs.TextFont.Typeface = SKTypeface.FromFamilyName("Times New Roman", SKFontStyle.Italic);
                                                break;
                                            case "Times-BoldItalic":
                                                renderer.gs.TextFont.Typeface = SKTypeface.FromFamilyName("Times New Roman", SKFontStyle.BoldItalic);
                                                break;
                                            case "Courier":
                                                renderer.gs.TextFont.Typeface = SKTypeface.FromFamilyName("Courier New", SKFontStyle.Normal);
                                                break;
                                            case "Courier-Bold":
                                                renderer.gs.TextFont.Typeface = SKTypeface.FromFamilyName("Courier New", SKFontStyle.Bold);
                                                break;
                                            case "Courier-Oblique":
                                                renderer.gs.TextFont.Typeface = SKTypeface.FromFamilyName("Courier New", SKFontStyle.Italic);
                                                break;
                                            case "Courier-BoldOblique":
                                                renderer.gs.TextFont.Typeface = SKTypeface.FromFamilyName("Courier New", SKFontStyle.BoldItalic);
                                                break;
                                            case "Symbol":
                                                renderer.gs.TextFont.Typeface = SKTypeface.FromFamilyName("Symbol");
                                                renderer.gs.TextFont.Encoding = null;
                                                break;
                                        }
                                        SKTypeface getHelvetica(SKFontStyle style)
                                        {
                                            var typeface = SKTypeface.FromFamilyName("Helvetica", style);
                                            if (typeface.FamilyName != "Helvetica")
                                                typeface = SKTypeface.FromFamilyName("Arial", style);
                                            return typeface;
                                        }
                                    }
                                }
                                if (renderer.gs.TextFont.Typeface == null && fontDescriptor != null)
                                {
                                    var fontFlags = (FontDescriptorFlags)(int)fontDescriptor["Flags"];
                                    string fontFamily = ((fontFlags & FontDescriptorFlags.Serif) > 0 || (baseFont != null && baseFont.StartsWith("Times"))) ? "Times New Roman" : "Arial";
                                    var isItalic = (fontFlags & FontDescriptorFlags.Italic) > 0;
                                    var isBold = (int?)fontDescriptor["FontWeight"] >= 700;
                                    var style = (!isItalic && !isBold) ? SKFontStyle.Normal : (isItalic && isBold ? SKFontStyle.BoldItalic : (isItalic ? SKFontStyle.Italic : SKFontStyle.Bold));
                                    renderer.gs.TextFont.Typeface = SKTypeface.FromFamilyName(fontFamily, style);
                                }
                            }
                            if (renderer.gs.TextFont.Typeface != null)
                                renderer.gs.OtherPaint.Typeface = renderer.gs.TextFont.Typeface;
                            break;
                        }
                    case "Tc":
                        renderer.gs.TextCharSpacing = (float)operands[0];
                        break;
                    case "Tz":
                        renderer.gs.TextTth = (float)operands[0] / 100;
                        SetTextStateMatrix();
                        break;
                    case "Tm":
                        {
                            renderer.textLineMatrix = MatrixFromOperands();
                            renderer.textMatrix = renderer.textLineMatrix;
                            break;
                        }
                    case "Tj":
                        {
                            ShowText((PdfString)operands[0]);
                            break;
                        }
                    case "TJ":
                        {
                            var arr = (PdfArray)operands[0];
                            foreach (var item in arr)
                            {
                                if (item is PdfString pdfString)
                                {
                                    ShowText(pdfString);
                                }
                                else
                                {
                                    var point = renderer.gs.TextStateMatrix.MapPoint(-(float)item / 1000, 0);
                                    SKMatrix.PreConcat(ref renderer.textMatrix, SKMatrix.MakeTranslation(point.X, 0));
                                }
                            }
                            break;
                        }
                    case "TL":
                        renderer.gs.TextLeading = (float)operands[0];
                        break;
                    case "TD":
                        renderer.gs.TextLeading = -(float)operands[1];
                        goto case "Td";
                    case "Td":
                        SKMatrix.PreConcat(ref renderer.textLineMatrix, SKMatrix.MakeTranslation((float)operands[0], (float)operands[1]));
                        renderer.textMatrix = renderer.textLineMatrix;
                        break;
                    case "T*":
                        SKMatrix.PreConcat(ref renderer.textLineMatrix, SKMatrix.MakeTranslation(0, -renderer.gs.TextLeading));
                        renderer.textMatrix = renderer.textLineMatrix;
                        break;
                    case "'":
                        SKMatrix.PreConcat(ref renderer.textLineMatrix, SKMatrix.MakeTranslation(0, -renderer.gs.TextLeading));
                        renderer.textMatrix = renderer.textLineMatrix;
                        ShowText((PdfString)operands[0]);
                        break;
                    case "\"":
                        renderer.gs.TextWordSpacing = (float)operands[0];
                        renderer.gs.TextCharSpacing = (float)operands[1];
                        SKMatrix.PreConcat(ref renderer.textLineMatrix, SKMatrix.MakeTranslation(0, -renderer.gs.TextLeading));
                        renderer.textMatrix = renderer.textLineMatrix;
                        ShowText((PdfString)operands[2]);
                        break;
                    case "Tw":
                        renderer.gs.TextWordSpacing = (float)operands[0];
                        break;
                    //xobjects
                    case "Do":
                        {
                            var imgStream = resources["XObject"].As<PdfDict>()[(PdfName)operands[0]].As<PdfStream>();
                            var subtype = imgStream.Dict["Subtype"].ToString();
                            if (subtype == "Image")
                            {
                                int width = (int)imgStream.Dict["Width"], height = (int)imgStream.Dict["Height"],
                                    numPixels = width * height, bpc = (int)imgStream.Dict["BitsPerComponent"];
                                var bytes = imgStream.GetBytes();
                                var colorSpace = imgStream.Dict["ColorSpace"];
                                SKColorSpace skColorSpace = null;
                                SKAlphaType alphaType;
                                byte[] imgBytes = new byte[numPixels * 4];

                                if ((bool?)imgStream.Dict["ImageMask"] == true)
                                {
                                    var decodeArr = (PdfArray)imgStream.Dict["Decode"];
                                    var markVal = decodeArr != null ? (int)decodeArr[0] == 1 : false;
                                    var byteReader = new ByteReader(bytes);
                                    int ix = 0;
                                    for (int y = 0; y < height; y++)
                                    {
                                        var bitReader = new BitReader(byteReader);
                                        for (int x = 0; x < width; x++)
                                        {
                                            if (bitReader.ReadBit() == markVal)
                                            {
                                                imgBytes[ix++] = renderer.gs.OtherPaint.Color.Red;
                                                imgBytes[ix++] = renderer.gs.OtherPaint.Color.Blue;
                                                imgBytes[ix++] = renderer.gs.OtherPaint.Color.Green;
                                                imgBytes[ix++] = 255;
                                            }
                                            else
                                            {
                                                ix += 3;
                                                imgBytes[ix++] = 0;
                                            }
                                        }

                                    }
                                    alphaType = SKAlphaType.Unpremul;
                                }
                                else
                                {
                                    int numComponents;

                                    if (colorSpace is PdfArray colorSpaceArr)
                                    {
                                        switch (colorSpaceArr[0].ToString())
                                        {
                                            case "Indexed":
                                                {
                                                    var indexObj = colorSpaceArr[3];
                                                    byte[] indexData = indexObj is PdfStream indexStream ? indexStream.GetBytes() : ((PdfString)indexObj).Value;
                                                    var baseColorSpace = colorSpaceArr[1];
                                                    var colorSpaceStr = baseColorSpace.ToString();
                                                    numComponents = colorSpaceStr == "DeviceGray" ? 1 : (colorSpaceStr == "DeviceCMYK" ? 4 : 3);
                                                    var hival = (int)colorSpaceArr[2];
                                                    var byteReader = new ByteReader(bytes);
                                                    var i = 0;
                                                    for (int y = 0; y < height; y++)
                                                    {
                                                        var bitStream = new BitReader(byteReader);
                                                        for (int x = 0; x < width; x++)
                                                        {
                                                            Array.Copy(indexData, bitStream.ReadBits(bpc) * numComponents, imgBytes, i * 4, numComponents);
                                                            if (numComponents == 1)
                                                                imgBytes[i * 4 + 1] = imgBytes[i * 4 + 2] = imgBytes[i * 4];
                                                            i++;
                                                        }
                                                    }
                                                    if (colorSpaceStr == "DeviceCMYK")
                                                    {
                                                        throw new NotImplementedException("Indexed DeviceCMYK");
                                                    }
                                                    break;
                                                }
                                            case "CalRGB":
                                                {
                                                    var dict = (PdfDict)colorSpaceArr[1];
                                                    var matrixArr = dict["Matrix"].As<PdfArray>().Select(x => (float)x).ToArray();
                                                    var curMatrix = new ColorMatrix(matrixArr, 3, 3);
                                                    numComponents = 3;
                                                    for (int i = 0; i < numPixels; i++)
                                                    {
                                                        var pixBytes = new[] { bytes[i * 3 + 0] / 255f, bytes[i * 3 + 1] / 255f, bytes[i * 3 + 2] / 255f };
                                                        var xyz = curMatrix.MultipleVectorWith(pixBytes);
                                                        var rgb = ColorMatrix.sRGB.MultipleWithVector(xyz);

                                                        imgBytes[i * 4 + 0] = (byte)Math.Max(0, Math.Min(255, (rgb[0] * 255)));
                                                        imgBytes[i * 4 + 1] = (byte)Math.Max(0, Math.Min(255, (rgb[1] * 255)));
                                                        imgBytes[i * 4 + 2] = (byte)Math.Max(0, Math.Min(255, (rgb[2] * 255)));
                                                    }

                                                    break;
                                                }
                                            case "ICCBased":
                                                {
                                                    var stream = (PdfStream)colorSpaceArr[1];
                                                    var iccBytes = stream.GetBytes();
                                                    skColorSpace = SKColorSpace.CreateIcc(iccBytes);
                                                    for (int i = 0; i < numPixels; i++)
                                                    {
                                                        imgBytes[i * 4 + 0] = bytes[i * 3 + 0];
                                                        imgBytes[i * 4 + 1] = bytes[i * 3 + 1];
                                                        imgBytes[i * 4 + 2] = bytes[i * 3 + 2];
                                                    }
                                                    numComponents = 3;
                                                    break;
                                                }
                                            default: throw new NotImplementedException();
                                        }
                                    }
                                    else
                                    {
                                        switch (colorSpace.ToString())
                                        {
                                            case "DeviceGray":
                                                {
                                                    int imgBytesIX = 0;
                                                    for (int i = 0; i < numPixels; i++)
                                                    {
                                                        var _b = bytes[i];
                                                        imgBytes[imgBytesIX + 0] = imgBytes[imgBytesIX + 1] = imgBytes[imgBytesIX + 2] = _b;
                                                        imgBytesIX += 4;
                                                    }
                                                    numComponents = 1;
                                                    break;
                                                }
                                            case "DeviceRGB":
                                                {
                                                    for (int i = 0; i < numPixels; i++)
                                                    {
                                                        imgBytes[i * 4 + 0] = bytes[i * 3 + 0];
                                                        imgBytes[i * 4 + 1] = bytes[i * 3 + 1];
                                                        imgBytes[i * 4 + 2] = bytes[i * 3 + 2];
                                                    }
                                                    numComponents = 3;
                                                    break;
                                                }
                                            case "DeviceCMYK":
                                                {
                                                    imgBytes = ColorHelper.CMYK2RGB(bytes);
                                                    numComponents = 4;
                                                    break;
                                                }
                                            default: throw new NotImplementedException();
                                        }
                                    }

                                    alphaType = SKAlphaType.Opaque;

                                    byte[] sMaskBytes = null;
                                    var sMask = (PdfStream)imgStream.Dict["SMask"];

                                    if (sMask != null)
                                    {
                                        var bitsPerComponent = (int)(sMask.Dict["BitsPerComponent"] ?? sMask.Dict["N"]);
                                        switch (bitsPerComponent)
                                        {
                                            case 8: sMaskBytes = sMask.GetBytes(); break;
                                            case 1:
                                                {
                                                    var _sMaskBytes = sMask.GetBytes();
                                                    var sMaskByteReader = new ByteReader(_sMaskBytes);
                                                    sMaskBytes = new byte[numPixels];
                                                    int i = 0;
                                                    for (int y = 0; y < height; y++)
                                                    {
                                                        var sMaskBitStream = new BitReader(sMaskByteReader);
                                                        for (int x = 0; x < width; x++)
                                                        {
                                                            sMaskBytes[i++] = (byte)(sMaskBitStream.ReadBit() ? 255 : 0);
                                                        }
                                                    }
                                                    break;
                                                }
                                        }
                                    }
                                    else
                                    {
                                        var mask = imgStream.Dict["Mask"]; //TODO color key masking
                                        if (mask is PdfStream maskStream)
                                        {
                                            var decode = (PdfArray)maskStream.Dict["Decode"];
                                            bool invert = decode != null && decode.Length == 2 && (int)decode[0] == 1;
                                            var maskByteReader = new ByteReader(maskStream.GetBytes());
                                            sMaskBytes = new byte[numPixels];
                                            for (int y = 0; y < height; y++)
                                            {
                                                var bitReader = new BitReader(maskByteReader);
                                                for (int x = 0; x < width; x++)
                                                {
                                                    if (bitReader.ReadBit() == invert)
                                                        sMaskBytes[y * width + x] = 255;
                                                }
                                            }
                                        }
                                        else if (mask is PdfArray maskArr)
                                        {
                                            alphaType = SKAlphaType.Unpremul;
                                            List<byte[]> maskColors = new List<byte[]>();
                                            var arrIX = 0;
                                            while (arrIX < maskArr.Length)
                                            {
                                                var maskColor = new byte[3];
                                                for (int i = 0; i < numComponents; i++)
                                                {
                                                    maskColor[i] = (byte)(int)maskArr[arrIX++];
                                                }
                                                if (numComponents == 1)
                                                    maskColor[2] = maskColor[1] = maskColor[0];
                                                maskColors.Add(maskColor);
                                            }
                                            for (int i = 0; i < numPixels; i++)
                                            {
                                                bool masked = false;
                                                var pixelIX = i * 4;
                                                foreach (var maskColor in maskColors)
                                                {
                                                    int j;
                                                    for (j = 0; j < 3; j++)
                                                    {
                                                        if (imgBytes[pixelIX + j] != maskColor[j])
                                                            break;
                                                    }
                                                    if (j == 3)
                                                    {
                                                        masked = true;
                                                        break;
                                                    }
                                                }
                                                if (!masked)
                                                    imgBytes[pixelIX + 3] = 255;
                                            }
                                        }
                                    }

                                    if (sMaskBytes != null)
                                    {
                                        for (int i = 0; i < numPixels; i++)
                                        {
                                            imgBytes[i * 4 + 3] = sMaskBytes[i];
                                        }
                                        alphaType = SKAlphaType.Unpremul;
                                    }
                                    if (alphaType == SKAlphaType.Opaque)
                                    {
                                        var ix = -1;
                                        for (int i = 0; i < numPixels; i++)
                                        {
                                            ix += 4;
                                            imgBytes[ix] = 255;
                                        }
                                    }
                                }

                                renderer.canvas.Save();
                                renderer.canvas.SetMatrix(Utils.MatrixConcat(renderer.canvas.TotalMatrix, renderer.flipInMatrix));
                                var bmpInfo = new SKImageInfo { Width = width, Height = height, ColorType = SKColorType.Rgba8888, AlphaType = alphaType };
                                bmpInfo.ColorSpace = skColorSpace;
                                var imgHandle = System.Runtime.InteropServices.GCHandle.Alloc(imgBytes, System.Runtime.InteropServices.GCHandleType.Pinned);
                                using (var img = SKImage.FromPixels(bmpInfo, imgHandle.AddrOfPinnedObject()))
                                    renderer.canvas.DrawImage(img, new SKRect(0, 0, 1, 1));
                                imgHandle.Free();
                                renderer.canvas.Restore();

                            }
                            else if (subtype == "Form")
                            {
                                var stream = (PdfStream)resources["XObject"].As<PdfDict>()[(PdfName)operands[0]];
                                if (stream != null)
                                {
                                    var matrixArr = (PdfArray)stream.Dict["Matrix"];
                                    renderer.canvas.Save();
                                    if (matrixArr != null)
                                    {
                                        var matrix = MatrixFromArray(matrixArr);
                                        renderer.canvas.SetMatrix(Utils.MatrixConcat(renderer.canvas.TotalMatrix, matrix));
                                    }
                                    new DrawContext(renderer, stream.GetBytes(), (PdfDict)stream.Dict["Resources"]);
                                    renderer.canvas.Restore();
                                }
                            }
                            break;
                        }
                    case "BI":
                        {
                            fParse.SkipWhiteSpace();
                            var dict = new Dictionary<PdfName, PdfObject>();
                            while (!fParse.ReadString("ID"))
                            {
                                var name = new PdfName(fParse);
                                fParse.SkipWhiteSpace();
                                var obj = fParse.ReadPdfObject(null);
                                fParse.SkipWhiteSpace();
                                dict.Add(name, obj);
                            }
                            var decode = (PdfArray)(dict.GetValueOrDefault("Decode") ?? dict.GetValueOrDefault("D"));
                            bool invert = decode != null && decode.Length == 2 && (int)decode[0] == 1;

                            fParse.Pos++;
                            var imgBytes = new List<byte>();
                            while (fParse.PeekByte != 'E' || fParse.PeekByteAtOffset(1) != 'I')
                            {
                                imgBytes.Add(fParse.ReadByte());
                            }
                            fParse.ReadString("EI");
                            int width = (int)(dict["W"] ?? dict["Width"]),
                                height = (int)(dict["H"] ?? dict["Height"]);
                            var bmp = new SKBitmap(new SKImageInfo { Width = width, Height = height, ColorType = SKColorType.Rgba8888, AlphaType = SKAlphaType.Unpremul });
                            bmp.Erase(SKColors.Transparent);
                            var imgByteReader = new ByteReader(imgBytes.ToArray());
                            for (int y = 0; y < height; y++)
                            {
                                var bitStream = new BitReader(imgByteReader);
                                for (int x = 0; x < width; x++)
                                {
                                    if (bitStream.ReadBit() == invert)
                                        bmp.SetPixel(x, y, renderer.gs.OtherPaint.Color);
                                }
                            }
                            var img = SKImage.FromBitmap(bmp);
                            renderer.canvas.Save();
                            renderer.canvas.SetMatrix(Utils.MatrixConcat(renderer.canvas.TotalMatrix, renderer.flipInMatrix));
                            renderer.canvas.DrawImage(img, new SKRect(0, 0, 1, 1));
                            renderer.canvas.Restore();
                            break;
                        }
                    case "Tr":
                        break;
                    default:
                        break;
                }

            }

            SKMatrix MatrixFromOperands() => MatrixFromArray(operands);
            SKColor RGBFromOperands()
            {
                var colors = operands.Select(x => (byte)Math.Round((float)x * 255)).ToArray();
                return new SKColor(colors[0], colors[1], colors[2]);
            }
            SKColor GrayFromOperands()
            {
                var c = (byte)((float)operands[0] * 255);
                return new SKColor(c, c, c);
            }
            SKColor CMYKFromOperands() => ColorHelper.CMYK2RGB(operands.Select(x => (float)x).ToArray());
            void EndPath()
            {
                curPath = new SKPath();
            }
            void SetTextStateMatrix()
            {
                renderer.gs.TextStateMatrix.ScaleX = renderer.gs.TextTfs * renderer.gs.TextTth;
                renderer.gs.TextStateMatrix.ScaleY = renderer.gs.TextTfs;
                renderer.gs.TextStateMatrix.TransY = renderer.gs.TextTrise;
            }
            void ShowText(PdfString str)
            {
                if (renderer.gs.TextFont == null)
                    return;
                if (renderer.gs.TextFont.Type3Font != null)
                {
                    var charProcs = (PdfDict)renderer.gs.TextFont.Type3Font["CharProcs"];
                    var fontMatrix = MatrixFromArray(renderer.gs.TextFont.Type3Font["FontMatrix"].As<PdfArray>());
                    var textStateFontMatrix = Utils.MatrixConcat(renderer.gs.TextStateMatrix, fontMatrix);
                    for (var i = 0; i < str.Value.Length; i += 1)
                    {
                        renderer.canvas.SetMatrix(Utils.MatrixConcat(renderer.textBaseMatrix, renderer.textMatrix, textStateFontMatrix));
                        var b = str.Value[i];
                        if (b == 0)
                            continue;
                        if (renderer.gs.TextFont.Code2Names == null || !renderer.gs.TextFont.Code2Names.TryGetValue(b, out var name))
                            name = renderer.gs.TextFont.Encoding.Code2Name(b);
                        var glyphDesc = (PdfStream)charProcs[name];
                        var glyphContents = glyphDesc.GetBytes();
                        new DrawContext(renderer, glyphContents, resources);
                        float glyphWidth = 0;
                        if (renderer.gs.TextFont.Widths != null && renderer.gs.TextFont.FirstChar != null)
                        {
                            glyphWidth = renderer.gs.TextFont.Widths[b - renderer.gs.TextFont.FirstChar.Value];
                        }
                        var effWidth = textStateFontMatrix.MapPoint(glyphWidth, 0).X + renderer.gs.TextCharSpacing;
                        SKMatrix.PreConcat(ref renderer.textMatrix, SKMatrix.MakeTranslation(effWidth, 0));
                    }
                }
                else
                {
                    for (var i = 0; i < str.Value.Length; i += 1)
                    {
                        renderer.canvas.SetMatrix(Utils.MatrixConcat(renderer.textBaseMatrix, renderer.textMatrix, renderer.gs.TextStateMatrix, renderer.flipOutMatrix));
                        byte[] strBytes = null;
                        var b = str.Value[i];
                        if (renderer.gs.TextFont.Type0)
                        {
                            if (renderer.gs.TextFont.CIDToGID != null)
                            {
                                if (b == 0)
                                    continue;
                                strBytes = new[] { renderer.gs.TextFont.CIDToGID[b * 2 + 1], renderer.gs.TextFont.CIDToGID[b * 2] };
                            }
                            else if (renderer.gs.TextFont.CodeMap != null)
                            {
                                var code = renderer.gs.TextFont.CodeMap[b | (str.Value[++i] << 8)];
                                strBytes = new byte[] { (byte)code, (byte)(code >> 8) };
                            }
                            else
                            {
                                strBytes = new[] { i == str.Value.Length - 1 ? (byte)0 : str.Value[i + 1], b };
                                i++;
                            }
                        }
                        else
                        {
                            if (renderer.gs.TextFont.Encoding != null)
                            {
                                int? unicode = null;
                                if (renderer.gs.TextFont.Code2Names != null && renderer.gs.TextFont.Code2Names.TryGetValue(b, out var bName))
                                {
                                    if (CharEncoding.UnicodeByName.TryGetValue(bName.ToString(), out var _unicode))
                                        unicode = _unicode;
                                }
                                if (unicode == null)
                                    unicode = renderer.gs.TextFont.Encoding.Code2Unicode(b);
                                if (unicode != null)
                                    strBytes = new byte[] { (byte)(unicode), (byte)(unicode >> 8) };
                            }
                            if (strBytes == null)
                                strBytes = new byte[] { b, 0 };
                        }
                        renderer.canvas.DrawText(strBytes, 0, 0, renderer.gs.OtherPaint);
                        float glyphWidth;
                        if (renderer.gs.TextFont.Widths != null && renderer.gs.TextFont.FirstChar != null && renderer.gs.TextFont.Widths.Count > b - renderer.gs.TextFont.FirstChar.Value)
                        {
                            glyphWidth = renderer.gs.TextFont.Widths[b - renderer.gs.TextFont.FirstChar.Value];
                        }
                        else
                        {
                            var glyphWidths = renderer.gs.OtherPaint.GetGlyphWidths(strBytes);
                            glyphWidth = (glyphWidths.Length > 0 ? glyphWidths[0] : 0);
                        }
                        var effWidth = renderer.gs.TextStateMatrix.MapPoint(glyphWidth, 0).X + renderer.gs.TextCharSpacing;
                        if (b == 32)
                            effWidth += renderer.gs.TextWordSpacing;
                        SKMatrix.PreConcat(ref renderer.textMatrix, SKMatrix.MakeTranslation(effWidth, 0));
                    }
                }
            }

        }
    }
}
