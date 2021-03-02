/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace SeaPeaYou.PeaPdf.W
{
    class Rectangle
    {
        public readonly float LowerLeftX, LowerLeftY, UpperRightX, UpperRightY;
        internal readonly PdfArray PdfArray;

        internal Rectangle(PdfArray arr)
        {
            this.PdfArray = arr;
            var values = arr.Select(x => (float)x).ToList();
            LowerLeftX = values[0];
            LowerLeftY = values[1];
            UpperRightX = values[2];
            UpperRightY = values[3];
            if (LowerLeftY > UpperRightY)
            {
                LowerLeftY = values[3];
                UpperRightY = values[1];
            }
            if (LowerLeftX > UpperRightX)
            {
                LowerLeftX = values[2];
                UpperRightX = values[0];
            }

        }

        public Rectangle(float lowerLeftX, float lowerLeftY, float upperRightX, float upperRightY)
        {
            LowerLeftX = lowerLeftX; LowerLeftY = lowerLeftY; UpperRightX = upperRightX; UpperRightY = upperRightY;
            PdfArray = new PdfArray((PdfNumeric)lowerLeftX, (PdfNumeric)lowerLeftY, (PdfNumeric)upperRightX, (PdfNumeric)upperRightY);
        }

    }
}
