/*
 * Copyright 2020 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SeaPeaYou.PeaPdf.Filters
{
    class LZWFlateDecode
    {

        public LZWFlateDecode(PdfDict decodeParms)
        {
            predictor = (int?)decodeParms?["Predictor"] ?? 1;
            if (predictor == 2)
                throw new NotImplementedException("tiff predictor");
            colors = (int?)decodeParms?["Colors"] ?? 1;
            bitsPerComponent = (int?)decodeParms?["BitsPerComponent"] ?? 8;
            columns = (int?)decodeParms?["Columns"] ?? 1;
        }

        public byte[] DoPredictor(byte[] bytes)
        {
            if (predictor >= 10)
            {
                columns *= colors;

                var predicted = new List<byte>(bytes.Length);
                int bytesIX = 0;
                do
                {
                    var algorithm = bytes[bytesIX++];
                    for (int c = 0; c < columns && bytesIX < bytes.Length; c++)
                    {
                        var b = bytes[bytesIX];
                        switch (algorithm)
                        {
                            case 0: break;
                            case 1:
                                if (c < colors)
                                    break;
                                b += predicted[predicted.Count - colors];
                                break;
                            case 2:
                                if (bytesIX < columns + 1)
                                    break;
                                b += predicted[predicted.Count - columns];
                                break;
                            case 3:
                                {
                                    byte left = c < colors ? (byte)0 : predicted[predicted.Count - colors],
                                        up = bytesIX < columns + 1 ? (byte)0 : predicted[predicted.Count - columns];
                                    b += (byte)((left + up) >> 1);
                                    break;
                                }
                            case 4:
                                {
                                    byte left = c < colors ? (byte)0 : predicted[predicted.Count - colors],
                                        up = bytesIX < columns + 1 ? (byte)0 : predicted[predicted.Count - columns],
                                        upLeft = (bytesIX < columns + 1 || c < colors) ? (byte)0 : predicted[predicted.Count - columns - colors];
                                    b += PaethPredictor(left, up, upLeft);
                                    break;
                                }
                            default: throw new NotImplementedException("algorithm");
                        }
                        predicted.Add(b);
                        bytesIX++;
                    }
                } while (bytesIX < bytes.Length);
                bytes = predicted.ToArray();
            }
            return bytes;
        }

        int predictor, colors, bitsPerComponent, columns;

        byte PaethPredictor(byte a, byte b, byte c)
        {
            int p = a + b - c,
                pa = Math.Abs(p - a),
                pb = Math.Abs(p - b),
                pc = Math.Abs(p - c);
            if (pa <= pb && pa <= pc) return a;
            if (pb <= pc) return b;
            return c;

        }

    }
}
