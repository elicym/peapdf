/*
 * Copyright 2020 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SeaPeaYou.PeaPdf
{
    class PdfArray : PdfObject, IEnumerable<PdfObject>
    {

        public PdfObject this[int ix] => values[ix];
        public int Length => values.Length;

        public PdfArray(FParse fParse, PdfIndirectReference iRef)
        {
            if (fParse.ReadByte() != '[')
                throw new FormatException();
            var values = new List<PdfObject>();
            fParse.SkipWhiteSpace();
            while (true)
            {
                if (fParse.PeekByte == ']')
                {
                    fParse.Pos++;
                    this.values = values.ToArray();
                    return;
                }
                var obj = fParse.Deref(fParse.ReadPdfObject(iRef)); //generally if you have an array, you read all objects, so we deref straight away
                if (obj is PdfIndirectReference ir && ir.ObjectNum == 660)
                    System.Diagnostics.Debugger.Break();
                values.Add(obj);
                fParse.SkipWhiteSpace();
            }
        }

        public PdfArray(PdfObject[] objs)
        {
            values = objs;
        }

        public override void Write(Stream stream, PDF pdf, PdfIndirectReference iRef)
        {
            stream.WriteByte((byte)'[');
            foreach (var obj in values)
            {
                obj.Write(stream, pdf, iRef);
            }
            stream.WriteByte((byte)']');
        }

        public IEnumerator<PdfObject> GetEnumerator() => ((IEnumerable<PdfObject>)values).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        PdfObject[] values;
    }
}
