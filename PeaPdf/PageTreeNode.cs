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

    class PageTreeNode
    {

        public PageTreeNode(PDF pdf, PdfDict pdfDict)
        {
            this.pdf = pdf;
            Count = (int)pdfDict["Count"];
            kids = pdfDict["Kids"].AsArray<PdfDict>();
        }

        public PdfDict GetPage(int page)
        {
            if (page <= 0)
                throw new Exception("Minimum value for page is 1.");
            if (page > Count)
                throw new Exception("page above page count.");
            int runningKidPages = 0;
            foreach (var kid in kids)
            {
                var kidType = (PdfName)kid["Type"];
                switch (kidType.ToString())
                {
                    case "Page":
                        runningKidPages++;
                        if (page == runningKidPages)
                            return kid;
                        break;
                    case "Pages":
                        {
                            var kidPageTreeNode = new PageTreeNode(pdf, kid);
                            int _runningKidPages = runningKidPages;
                            runningKidPages += kidPageTreeNode.Count;
                            if (page <= runningKidPages)
                                return kidPageTreeNode.GetPage(page - _runningKidPages);
                            break;
                        }
                    default: throw new Exception("Unknown page kid type.");
                }
            }
            throw new Exception("Could not find page.");
        }

        public readonly int Count;
        readonly PdfDict[] kids;
        readonly PDF pdf;

    }
}
