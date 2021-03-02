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
        class DrawContext
        {

            public DrawContext(Renderer renderer, W.ContentStream cs)
            {
                this.renderer = renderer;
                this.resources = cs.Resources;
                //File.WriteAllBytes(@"d:\tmp\content-latest-inner.txt", bytes);

                //var cs=new CS.ContentStream()

                foreach (var instruction in cs.Instructions)
                {
                    DoKeyword(instruction);
                }

                //r = new PdfReader(bytes);

                //while (true)
                //{
                //    r.SkipWhiteSpace();
                //    if (r.AtEnd)
                //        break;
                //    var b = r.PeekByte;
                //    if ((b >= 'a' && b <= 'z') || (b >= 'A' && b <= 'Z') || b == '\'' || b == '"')
                //    {
                //        var keyword = r.ReadStringUntilDelimiter();
                //        DoKeyword(keyword);
                //        operands.Clear();
                //    }
                //    else
                //    {
                //        operands.Add(r.ReadPdfObject(null));
                //    }
                //}
            }

            readonly Renderer renderer;
            readonly W.ResourceDictionary resources;
            SKPath curPath = new SKPath();

            void DoKeyword(CS.Instruction keyword)
            {
                switch (keyword)
                {

                    //graphics state
                    case CS.q inst:
                        renderer.graphicsStateStack.Push(renderer.gs.Clone());
                        renderer.canvas.Save();
                        break;
                    case CS.Q inst:
                        if (renderer.graphicsStateStack.Count > 0)
                        {
                            renderer.gs = renderer.graphicsStateStack.Pop();
                            renderer.canvas.Restore();
                        }
                        break;
                    case CS.gs inst:
                        {
                            var prms = (PdfDict)resources.ExtGState[inst.dictName];
                            foreach (var (key, value) in prms)
                            {
                                switch (key)
                                {
                                    case "LW":
                                        renderer.gs.StrokePaint.StrokeWidth = (float)value;
                                        break;
                                    case "LC":
                                        renderer.gs.StrokePaint.StrokeCap = (SKStrokeCap)(int)value;
                                        break;
                                    case "LJ":
                                        renderer.gs.StrokePaint.StrokeJoin = (SKStrokeJoin)(int)value;
                                        break;
                                    case "ML":
                                        renderer.gs.StrokePaint.StrokeMiter = (float)value;
                                        break;
                                    case "D":
                                        {
                                            var arr = (PdfArray)value;
                                            var dashArray = (PdfArray)arr[0];
                                            renderer.gs.StrokePaint.PathEffect = SKPathEffect.CreateDash(dashArray.Select(x => (float)x).ToArray(), (float)arr[1]);
                                            break;
                                        }
                                    case "CA":
                                        renderer.gs.StrokePaint.ColorFilter = SKColorFilter.CreateBlendMode(SKColors.White.WithAlpha((byte)((float)value * 255)), SKBlendMode.DstIn);
                                        break;
                                    case "ca":
                                        renderer.gs.OtherPaint.ColorFilter = SKColorFilter.CreateBlendMode(SKColors.White.WithAlpha((byte)((float)value * 255)), SKBlendMode.DstIn);
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
                    case CS.M inst:
                        renderer.gs.StrokePaint.StrokeMiter = inst.miterLimit;
                        break;
                    case CS.cm inst:
                        renderer.canvas.SetMatrix(Utils.MatrixConcat(renderer.canvas.TotalMatrix, inst.Matrix));
                        break;
                    case CS.w inst:
                        renderer.gs.StrokePaint.StrokeWidth = inst.lineWidth;
                        break;
                    case CS.J inst:
                        renderer.gs.StrokePaint.StrokeCap = inst.lineCap;
                        break;
                    case CS.j inst:
                        renderer.gs.StrokePaint.StrokeJoin = inst.lineJoin;
                        break;
                    case CS.d inst:
                        {
                            renderer.gs.StrokePaint.PathEffect = SKPathEffect.CreateDash(inst.dashArray, inst.dashPhase);
                            break;
                        }
                    case CS.i inst:
                        break;
                    //clipping paths
                    case CS.W inst:
                        curPath.FillType = SKPathFillType.Winding;
                        renderer.canvas.ClipPath(curPath);
                        break;
                    case CS.W_ inst:
                        curPath.FillType = SKPathFillType.EvenOdd;
                        renderer.canvas.ClipPath(curPath);
                        break;
                    //path construction
                    case CS.re inst:
                        {
                            curPath.AddRect(new SKRect(inst.x, inst.y + inst.height, inst.x + inst.width, inst.y));
                            break;
                        }
                    case CS.m inst:
                        curPath.MoveTo(inst.x, inst.y);
                        break;
                    case CS.l inst:
                        curPath.LineTo(inst.x, inst.y);
                        break;
                    case CS.c inst:
                        {
                            curPath.CubicTo(inst.x1, inst.y1, inst.x2, inst.y2, inst.x3, inst.y3);
                            break;
                        }
                    case CS.v inst:
                        {
                            curPath.CubicTo(curPath.LastPoint.X, curPath.LastPoint.Y, inst.x2, inst.y2, inst.x3, inst.y3);
                            break;
                        }
                    case CS.y inst:
                        {
                            curPath.CubicTo(inst.x1, inst.y1, inst.x3, inst.y3, inst.x3, inst.y3);
                            break;
                        }
                    case CS.h inst:
                        curPath.Close();
                        break;
                    //path painting
                    case CS.n inst:
                        EndPath();
                        break;
                    case CS.s inst:
                        curPath.Close();
                        renderer.canvas.DrawPath(curPath, renderer.gs.StrokePaint);
                        EndPath();
                        break;
                    case CS.S inst:
                        renderer.canvas.DrawPath(curPath, renderer.gs.StrokePaint);
                        EndPath();
                        break;
                    case CS.f inst_f:
                    case CS.F inst_F:
                        curPath.FillType = SKPathFillType.Winding;
                        renderer.canvas.DrawPath(curPath, renderer.gs.OtherPaint);
                        EndPath();
                        break;
                    case CS.f_ inst:
                        curPath.FillType = SKPathFillType.EvenOdd;
                        renderer.canvas.DrawPath(curPath, renderer.gs.OtherPaint);
                        EndPath();
                        break;
                    case CS.B inst:
                        curPath.FillType = SKPathFillType.Winding;
                        renderer.canvas.DrawPath(curPath, renderer.gs.OtherPaint);
                        renderer.canvas.DrawPath(curPath, renderer.gs.StrokePaint);
                        EndPath();
                        break;
                    case CS.B_ inst:
                        curPath.FillType = SKPathFillType.EvenOdd;
                        renderer.canvas.DrawPath(curPath, renderer.gs.OtherPaint);
                        renderer.canvas.DrawPath(curPath, renderer.gs.StrokePaint);
                        EndPath();
                        break;
                    //color
                    case CS.CS inst:
                        renderer.gs.StrokeColorSpace = renderer.GetColorSpace(inst.name);
                        renderer.gs.StrokePaint.Color = SKColors.Black;
                        break;
                    case CS.cs inst:
                        renderer.gs.OtherColorSpace = renderer.GetColorSpace(inst.name);
                        renderer.gs.OtherPaint.Color = SKColors.Black;
                        break;
                    case CS.ColorFamily inst:
                        {
                            var colorSpace = inst.ColorSpace ?? (inst.IsStroke ? renderer.gs.StrokeColorSpace : renderer.gs.OtherColorSpace);
                            SKColor color = default;
                            switch (colorSpace)
                            {
                                case ColorSpace.DeviceRGB: color = RGBFromOperands(inst.GetOperands()); break;
                                case ColorSpace.DeviceGray: color = GrayFromOperands(inst.GetOperands()); break;
                                case ColorSpace.DeviceCMYK: color = CMYKFromOperands(inst.GetOperands()); break;
                                case ColorSpace.Blank: color = SKColors.Transparent; break;
                            }
                            var paint = inst.IsStroke ? renderer.gs.StrokePaint : renderer.gs.OtherPaint;
                            paint.Color = color;
                            break;
                        }
                    //marked content
                    case CS.BDC inst:
                        break;
                    case CS.EMC inst:
                        break;
                    //text
                    case CS.BT inst:
                        {
                            renderer.canvas.Save();
                            renderer.textBaseMatrix = renderer.canvas.TotalMatrix;
                            renderer.textMatrix = renderer.textLineMatrix = SKMatrix.CreateIdentity();
                            break;
                        }
                    case CS.ET inst:
                        renderer.canvas.Restore();
                        break;
                    case CS.Tf inst:
                        {
                            renderer.gs.TextTfs = inst.size;
                            SetTextStateMatrix();

                            if (!renderer.fontDict.TryGetValue(inst.font, out renderer.gs.TextFont))
                            {
                                var fontPdfDict = resources.Font[inst.font].As<PdfDict>();
                                renderer.fontDict.Add(inst.font, renderer.gs.TextFont = new Font(fontPdfDict));
                            }
                            if (renderer.gs.TextFont.Typeface != null)
                                renderer.gs.OtherPaint.Typeface = renderer.gs.TextFont.Typeface;
                            break;
                        }
                    case CS.Tc inst:
                        renderer.gs.TextCharSpacing = inst.charSpace;
                        break;
                    case CS.Tz inst:
                        renderer.gs.TextTth = inst.scale / 100;
                        SetTextStateMatrix();
                        break;
                    case CS.Tm inst:
                        {
                            renderer.textLineMatrix = inst.Matrix;
                            renderer.textMatrix = renderer.textLineMatrix;
                            break;
                        }
                    case CS.Tj inst:
                        {
                            ShowText(inst.@string);
                            break;
                        }
                    case CS.TJ inst:
                        {
                            foreach (var item in inst.Array)
                            {
                                if (item is PdfString pdfString)
                                {
                                    ShowText(pdfString);
                                }
                                else
                                {
                                    var point = renderer.gs.TextStateMatrix.MapPoint(-(float)item / 1000, 0);
                                    renderer.textMatrix = renderer.textMatrix.PreConcat(SKMatrix.CreateTranslation(point.X, 0));
                                }
                            }
                            break;
                        }
                    case CS.TL inst:
                        renderer.gs.TextLeading = inst.leading;
                        break;
                    case CS.TD inst:
                        renderer.gs.TextLeading = -inst.ty;
                        renderer.textLineMatrix = renderer.textLineMatrix.PreConcat(SKMatrix.CreateTranslation(inst.tx, inst.ty));
                        renderer.textMatrix = renderer.textLineMatrix;
                        break;
                    case CS.Td inst:
                        renderer.textLineMatrix = renderer.textLineMatrix.PreConcat(SKMatrix.CreateTranslation(inst.tx, inst.ty));
                        renderer.textMatrix = renderer.textLineMatrix;
                        break;
                    case CS.T_ inst:
                        renderer.textLineMatrix = renderer.textLineMatrix.PreConcat(SKMatrix.CreateTranslation(0, -renderer.gs.TextLeading));
                        renderer.textMatrix = renderer.textLineMatrix;
                        break;
                    case CS.Apostrophe inst:
                        renderer.textLineMatrix = renderer.textLineMatrix.PreConcat(SKMatrix.CreateTranslation(0, -renderer.gs.TextLeading));
                        renderer.textMatrix = renderer.textLineMatrix;
                        ShowText(inst.@string);
                        break;
                    case CS.Quote inst:
                        renderer.gs.TextWordSpacing = inst.aw;
                        renderer.gs.TextCharSpacing = inst.ac;
                        renderer.textLineMatrix = renderer.textLineMatrix.PreConcat(SKMatrix.CreateTranslation(0, -renderer.gs.TextLeading));
                        renderer.textMatrix = renderer.textLineMatrix;
                        ShowText(inst.@string);
                        break;
                    case CS.Tw inst:
                        renderer.gs.TextWordSpacing = inst.wordSpace;
                        break;
                    //xobjects
                    case CS.Do inst:
                        {
                            var imgStream = resources.XObject[inst.name].As<PdfStream>();
                            var subtype = imgStream.Dict["Subtype"].ToString();
                            if (subtype == "Image")
                            {
                                int width = (int)imgStream.Dict["Width"], height = (int)imgStream.Dict["Height"],
                                    numPixels = width * height, bpc = (int)imgStream.Dict["BitsPerComponent"];
                                byte[] imgBytes;
                                SKColorSpace skColorSpace = null;
                                SKAlphaType alphaType;
                                var (bytes, filterName) = imgStream.GetDecodedBytesForImage();
                                if (filterName == "DCTDecode")
                                {
                                    imgBytes = Native.DecodeDCT(bytes, numPixels);
                                    alphaType = SKAlphaType.Opaque;
                                }
                                else if (filterName == "JPXDecode")
                                {
                                    imgBytes = Native.DecodeJPX(bytes, numPixels);
                                    alphaType = SKAlphaType.Opaque;
                                }
                                else
                                {
                                    imgBytes = new byte[numPixels * 4];

                                    var colorSpace = imgStream.Dict["ColorSpace"];

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
                                                        byte[] indexData = indexObj is PdfStream indexStream ? indexStream.GetDecodedBytes() : ((PdfString)indexObj).Value;
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
                                                            imgBytes = ColorHelper.CMYK2RGB(imgBytes);
                                                            //throw new NotImplementedException("Indexed DeviceCMYK");
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
                                                        var iccBytes = stream.GetDecodedBytes();
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
                                                case 8: sMaskBytes = sMask.GetDecodedBytes(); break;
                                                case 1:
                                                    {
                                                        var _sMaskBytes = sMask.GetDecodedBytes();
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
                                                bool invert = decode != null && decode.Count == 2 && (int)decode[0] == 1;
                                                var maskByteReader = new ByteReader(maskStream.GetDecodedBytes());
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
                                                while (arrIX < maskArr.Count)
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
                                var stream = (PdfStream)resources.XObject[inst.name];
                                if (stream != null)
                                {
                                    var matrixArr = (PdfArray)stream.Dict["Matrix"];
                                    renderer.canvas.Save();
                                    if (matrixArr != null)
                                    {
                                        var matrix = Utils.MatrixFromArray(matrixArr);
                                        renderer.canvas.SetMatrix(Utils.MatrixConcat(renderer.canvas.TotalMatrix, matrix));
                                    }
                                    new DrawContext(renderer, new W.ContentStream(stream, null));
                                    renderer.canvas.Restore();
                                }
                            }
                            break;
                        }
                    case CS.BI inst:
                        {
                            var decode = (PdfArray)(inst.Dict.GetValueOrDefault("Decode") ?? inst.Dict.GetValueOrDefault("D"));
                            bool invert = decode != null && decode.Count == 2 && (int)decode[0] == 1;
                            int width = (int)(inst.Dict["W"] ?? inst.Dict["Width"]),
                                height = (int)(inst.Dict["H"] ?? inst.Dict["Height"]);
                            var bmp = new SKBitmap(new SKImageInfo { Width = width, Height = height, ColorType = SKColorType.Rgba8888, AlphaType = SKAlphaType.Unpremul });
                            bmp.Erase(SKColors.Transparent);
                            var imgByteReader = new ByteReader(inst.ImgBytes);
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
                    case CS.Tr inst:
                        break;
                    default:
                        break;
                }

            }

            SKColor RGBFromOperands(IList<PdfObject> operands)
            {
                var colors = operands.Select(x => (byte)Math.Round((float)x * 255)).ToArray();
                return new SKColor(colors[0], colors[1], colors[2]);
            }
            SKColor GrayFromOperands(IList<PdfObject> operands)
            {
                var c = (byte)((float)operands[0] * 255);
                return new SKColor(c, c, c);
            }
            SKColor CMYKFromOperands(IList<PdfObject> operands) => ColorHelper.CMYK2RGB_Single(operands.Select(x => (float)x).ToArray());
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
                    var fontMatrix = Utils.MatrixFromArray(renderer.gs.TextFont.Type3Font["FontMatrix"].As<PdfArray>());
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
                        new DrawContext(renderer, new W.ContentStream(glyphDesc, resources));
                        float glyphWidth = 0;
                        if (renderer.gs.TextFont.Widths != null && renderer.gs.TextFont.FirstChar != null)
                        {
                            glyphWidth = renderer.gs.TextFont.Widths[b - renderer.gs.TextFont.FirstChar.Value];
                        }
                        var effWidth = textStateFontMatrix.MapPoint(glyphWidth, 0).X + renderer.gs.TextCharSpacing;
                        renderer.textMatrix = renderer.textMatrix.PreConcat(SKMatrix.CreateTranslation(effWidth, 0));
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
                                    if (CharEncoding.UnicodeByName.TryGetValue(bName, out var _unicode))
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
                        renderer.textMatrix = renderer.textMatrix.PreConcat(SKMatrix.CreateTranslation(effWidth, 0));
                    }
                }
            }

        }
    }
}
