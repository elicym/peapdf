/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace SeaPeaYou.PeaPdf
{
    public static class PageSizes
    {
        public static readonly Dimensions A4 = new Dimensions(595, 842);
        public static readonly Dimensions A5 = new Dimensions(420, 595);
        public static readonly Dimensions Letter = new Dimensions(612, 792);
    }

    public class Dimensions
    {
        public int Width, Height;
        public Dimensions(int width, int height) => (Width, Height) = (width, height);
    }
}
