/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using SeaPeaYou.PeaPdf.W;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace SeaPeaYou.PeaPdf.VisualElements
{

    public class Image : VisualElement
    {
        static int counter;

        byte[] bytes;
        SKEncodedImageFormat format;
        string key;
        int imgWidth, imgHeight;

        public Image(byte[] bytes)
        {
            key = $"pp-i-" + Interlocked.Increment(ref counter);
            using (var ms = new System.IO.MemoryStream(bytes))
            using (var codec = SKCodec.Create(ms))
            {
                format = codec.EncodedFormat;
                if (format != SKEncodedImageFormat.Jpeg && format != SKEncodedImageFormat.Png)
                    throw new NotSupportedException(format.ToString());
            }
            using var img = SKImage.FromEncodedData(bytes);
            imgWidth = img.Width;
            imgHeight = img.Height;
            if (format == SKEncodedImageFormat.Png)
            {
                var pixels = Marshal.AllocHGlobal(imgWidth * imgHeight * 4);
                img.ReadPixels(new SKImageInfo(imgWidth, imgHeight, SKColorType.Rgba8888), pixels, imgWidth * 4, 0, 0);
                unsafe
                {
                    byte* p = (byte*)pixels.ToPointer();
                    var png = new byte[imgWidth * imgHeight * 3];
                    for (int y = 0; y < imgHeight; y++)
                    {
                        for (int x = 0; x < imgWidth; x++)
                        {
                            for (int c = 0; c < 3; c++)
                            {
                                var b = p[y * imgWidth * 4 + x * 4 + c];
                                png[y * imgWidth * 3 + x * 3 + c] = b;
                            }
                        }
                    }
                    this.bytes = png;
                }                
                Marshal.FreeHGlobal(pixels);
            }
            else
            {
                this.bytes = bytes;
            }
        }

        internal override DrawInfo PrepareToDraw(float maxX, ResourceDictionary resources)
        {
            var bounds = Bounds ?? new Bounds();
            bounds.Top ??= 0;
            bounds.Left ??= 0;
            bounds.Right ??= maxX;
            bounds.Bottom ??= (float)imgHeight / imgWidth * (bounds.Right.Value - bounds.Left.Value) + bounds.Top.Value;
            resources.XObject ??= new PdfDict();
            if (resources.XObject[key] == null)
            {

                PdfStream stream;
                if (format == SKEncodedImageFormat.Jpeg)
                {
                    stream = new PdfStream("DCTDecode");
                    stream.SetEncodedBytes(bytes);
                }
                else
                {
                    var decodeParms = new PdfDict
                    {
                        {"Predictor",(PdfNumeric)13 },
                        {"Colors",(PdfNumeric)3 },
                        {"BitsPerComponent",(PdfNumeric)8 },
                        {"Columns",(PdfNumeric)imgWidth }
                    };
                    stream = new PdfStream("FlateDecode", decodeParms);
                    stream.SetDecodedBytes(bytes);
                }
                stream.Dict["Type"] = (PdfName)"XObject";
                stream.Dict["Subtype"] = (PdfName)"Image";
                stream.Dict["Width"] = (PdfNumeric)imgWidth;
                stream.Dict["Height"] = (PdfNumeric)imgHeight;
                stream.Dict["ColorSpace"] = (PdfName)"DeviceRGB";
                stream.Dict["BitsPerComponent"] = (PdfNumeric)8;
                resources.XObject[key] = stream;
            }
            var drawInfo = new DrawInfo();
            float height = Bounds.Bottom.Value - Bounds.Top.Value, width = Bounds.Right.Value - Bounds.Left.Value;
            drawInfo.Instructions.Add(new CS.q());
            drawInfo.Instructions.Add(new CS.cm(width, 0, 0, height, Bounds.Left.Value, Bounds.Top.Value));
            drawInfo.Instructions.Add(new CS.cm(1, 0, 0, -1, 0, 1));
            drawInfo.Instructions.Add(new CS.Do(key));
            drawInfo.Instructions.Add(new CS.Q());
            drawInfo.Right = Bounds.Right.Value;
            drawInfo.Bottom = Bounds.Bottom.Value;
            return drawInfo;
        }
    }
}
