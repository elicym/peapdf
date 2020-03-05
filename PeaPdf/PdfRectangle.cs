/*
 * Copyright 2020 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace SeaPeaYou.PeaPdf
{
    class PdfRectangle
    {
        public readonly float LowerLeftX, LowerLeftY, UpperRightX, UpperRightY;

        public PdfRectangle(IEnumerable<PdfObject> vals)
        {
            var values = vals.Select(x => (float)x).ToList();
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

    }
}
