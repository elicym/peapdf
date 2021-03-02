/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace SeaPeaYou.PeaPdf.W
{
    class AcroForm
    {
        public readonly PdfDict PdfDict;
        public AcroForm(PdfDict dict) => this.PdfDict = dict;

        public PdfArray<PdfDict> Fields => new PdfArray<PdfDict>((PdfArray)PdfDict["Fields"]);

        public ResourceDictionary DR => ((PdfDict)PdfDict["DR"])?.To(x => new ResourceDictionary(x));

        public PdfString DA => (PdfString)PdfDict["DA"];
    }
}
