/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SeaPeaYou.PeaPdf.W
{
    class PageTreeNode
    {

        public readonly PdfDict PdfDict;

        public PageTreeNode(PdfDict dict) { PdfDict = dict; }
        public PageTreeNode(List<Page> kids)
        {
            PdfDict = new PdfDict { Type = "Pages" };
            PdfDict["Kids"] = new PdfArray(kids.Select(x=>x.Dict).ToArray());
            Count = kids.Count;
        }

        public PdfDict Parent { get => (PdfDict)PdfDict["Parent"]; set => PdfDict["Parent"] = value; }
        public PdfArray<PdfDict> Kids { get => new PdfArray<PdfDict>((PdfArray)PdfDict["Kids"]); }
        public int Count { get => (int)PdfDict["Count"]; set => PdfDict["Count"] = (PdfNumeric)value; }

    }
}
