/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace SeaPeaYou.PeaPdf.W
{
    class AppearanceStream
    {
        public readonly PdfDict PdfDict;

        /// <summary>(Required) The annotation’s normal appearance. </summary>
        public FormXObjects N;
        /// <summary>(Optional) The annotation’s rollover appearance. </summary>
        public FormXObjects R;
        /// <summary>(Optional) The annotation’s down appearance. </summary>
        public FormXObjects D;

        public AppearanceStream(PdfDict dict)
        {
            PdfDict = dict;
            dict["N"].IfNotNull(x => N = new FormXObjects(x)); //sometimes it's null, even though it shouldn't be
            dict["R"].IfNotNull(x => R = new FormXObjects(x));
            dict["D"].IfNotNull(x => D = new FormXObjects(x));
        }

        public AppearanceStream(Rectangle rectangle)
        {
            PdfDict = new PdfDict();
            N = new FormXObjects(new FormXObject(rectangle));
        }

        public void UpdateObjects()
        {
            N.UpdateObjects();
            PdfDict["N"] = N.GetPdfObject();
            if (R != null)
            {
                R.UpdateObjects();
                PdfDict["R"] = R.GetPdfObject();
            }
            if (D != null)
            {
                D.UpdateObjects();
                PdfDict["D"] = D.GetPdfObject();
            }
        }

    }
}
