/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using SeaPeaYou.PeaPdf.CS;
using System;
using System.Collections.Generic;
using System.Text;

namespace SeaPeaYou.PeaPdf.W
{
    class ResourceDictionary
    {
        public readonly PdfDict PdfDict;
        public ResourceDictionary(PdfDict dict) => this.PdfDict = dict;
        public ResourceDictionary()
        {
            PdfDict = new PdfDict();
        }

        public PdfDict Font { get => (PdfDict)PdfDict["Font"]; set => PdfDict["Font"] = value; }
        public PdfDict ExtGState { get => (PdfDict)PdfDict["ExtGState"]; set => PdfDict["ExtGState"] = value; }
        public PdfDict ColorSpace { get => (PdfDict)PdfDict["ColorSpace"]; set => PdfDict["ColorSpace"] = value; }
        public PdfDict XObject { get => (PdfDict)PdfDict["XObject"]; set => PdfDict["XObject"] = value; }

        public void ConcatWith(PdfDict other)
        {
            foreach (var (key, value) in other)
            {
                var isArr = key == "ProcSet";
                if (isArr)
                {
                    var arr = (PdfArray)PdfDict[key];
                    if (arr == null)
                    {
                        PdfDict[key] = value;
                    }
                    else
                    {
                        foreach (var item in (PdfArray)value)
                        {
                            arr.Add(item);
                        }
                    }
                }
                else
                {
                    var subDict = (PdfDict)PdfDict[key];
                    if (subDict == null)
                    {
                        PdfDict[key] = value;
                    }
                    else
                    {
                        foreach (var (subkey, subvalue) in (PdfDict)value)
                        {
                            subDict[subkey] = subvalue;
                        }
                    }
                }
            }
        }

    }
}
