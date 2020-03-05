/*
 * Copyright 2020 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections;

namespace SeaPeaYou.PeaPdf
{
    class PdfDict : PdfObject, IEnumerable<(string key, PdfObject value)>
    {

        public PdfDict(FParse fParse, PdfIndirectReference iRef)
        {
            if (!fParse.ReadString("<<"))
                throw new FormatException();

            this.fParse = fParse;

            dict = new Dictionary<PdfName, PdfObject>();
            fParse.SkipWhiteSpace();
            while (!fParse.ReadString(">>"))
            {
                var pdfName = new PdfName(fParse);
                fParse.SkipWhiteSpace();
                var val = fParse.ReadPdfObject(iRef);
                dict.Add(pdfName, val);
                fParse.SkipWhiteSpace();
            }
        }

        public PdfDict(Dictionary<PdfName, PdfObject> dict)
        {
            this.dict = dict;
        }

        public PdfObject this[PdfName pdfName] => dict.TryGetValue(pdfName, out var pdfObject) ? fParse.Deref(pdfObject) : null;

        public override void Write(Stream stream, PDF pdf, PdfIndirectReference iRef)
        {
            stream.WriteByte((byte)'<');
            stream.WriteByte((byte)'<');

            foreach (var item in dict)
            {
                item.Key.Write(stream, pdf, iRef);
                item.Value.Write(stream, pdf, iRef);
            }

            stream.WriteByte((byte)'>');
            stream.WriteByte((byte)'>');
            stream.WriteByte((byte)' ');

        }

        public IEnumerator<(string key, PdfObject value)> GetEnumerator()
        {
            foreach (var keyVal in dict)
            {
                var val = fParse.Deref(keyVal.Value);
                yield return (keyVal.Key.ToString(), val);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        readonly Dictionary<PdfName, PdfObject> dict;
        readonly FParse fParse;
    }
}
