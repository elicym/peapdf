/*
 * Copyright 2020 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using SkiaSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;
using System.Diagnostics;

namespace SeaPeaYou.PeaPdf
{
    static class ColorHelper
    {

        static ICCProfile deviceLink = new ICCProfile(Properties.Resources.devicelink);
        static Dictionary<float[], SKColor> cmykColors = new Dictionary<float[], SKColor>(new FloatArrayStructuralEqualityComparer());

        public static byte[] CMYK2RGB(byte[] cmyk)
        {
            var numPixels = cmyk.Length / 4;
            var cmykFloats = new float[cmyk.Length];
            for (int i = 0; i < cmyk.Length; i++)
            {
                cmykFloats[i] = cmyk[i] / 255f;
            }
            var rgbFloats = new float[cmyk.Length];
            var dict = new Dictionary<(float, float, float, float), (float, float, float)>();
            for (int i = 0; i < numPixels; i++)
            {
                Span<float> input = new Span<float>(cmykFloats, i * 4, 4), output = new Span<float>(rgbFloats, i * 4, 3);
                var inputKey = (input[0], input[1], input[2], input[3]);
                if (dict.TryGetValue(inputKey, out var _output))
                {
                    output[0] = _output.Item1;
                    output[1] = _output.Item2;
                    output[2] = _output.Item3;
                }
                else
                {
                    deviceLink.ConvertToPCS(input, output);
                    dict.Add(inputKey, (output[0], output[1], output[2]));
                }
            }
            var rgbBytes = new byte[cmyk.Length];
            for (int i = 0; i < cmyk.Length; i++)
            {
                rgbBytes[i] = ToByte(rgbFloats[i]);
            }
            return rgbBytes;
        }

        public static SKColor CMYK2RGB(float[] cmyk)
        {
            if (!cmykColors.TryGetValue(cmyk, out var res))
            {
                Span<float> rgb = stackalloc float[3];
                deviceLink.ConvertToPCS(cmyk, rgb);
                res = new SKColor(ToByte(rgb[0]), ToByte(rgb[1]), ToByte(rgb[2]));
                cmykColors.Add(cmyk, res);
            }
            return res;
        }

        static byte ToByte(float d) => (byte)Math.Min(255, Math.Max(0, (int)Math.Round(d * 255)));

        class FloatArrayStructuralEqualityComparer : EqualityComparer<float[]>
        {
            public override bool Equals(float[] x, float[] y) => StructuralComparisons.StructuralEqualityComparer.Equals(x, y);

            public override int GetHashCode(float[] obj) => StructuralComparisons.StructuralEqualityComparer.GetHashCode(obj);
        }

    }
}
