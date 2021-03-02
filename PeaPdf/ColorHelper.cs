/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using SkiaSharp;
using System;

namespace SeaPeaYou.PeaPdf
{
    static class ColorHelper
    {

        public static byte[] CMYK2RGB(byte[] cmyk)
        {
            var rgb = Native.CMYK2RGB(cmyk);
            return rgb;
        }

        public static SKColor CMYK2RGB_Single(float[] cmyk)
        {
            var res = Native.CMYK2RGB_Single(cmyk);
            return res;
        }

    }
}
