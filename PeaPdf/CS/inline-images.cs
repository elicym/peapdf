/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace SeaPeaYou.PeaPdf.CS
{
    class BI : Instruction
    {
        public Dictionary<string, PdfObject> Dict = new Dictionary<string, PdfObject>();
        public byte[] ImgBytes;

        public BI(List<PdfObject> _) { }

        public void Read(PdfReader r)
        {
            r.SkipWhiteSpace();
            while (!r.ReadString("ID"))
            {
                var name = new PdfName(r);
                r.SkipWhiteSpace();
                var obj = r.ReadPdfObject(null);
                r.SkipWhiteSpace();
                Dict.Add(name.String, obj);
            }
            r.Pos++;
            var imgBytes = new List<byte>();
            while (r.PeekByte != 'E' || r.PeekByteAtOffset(1) != 'I')
            {
                imgBytes.Add(r.ReadByte());
            }
            r.ReadString("EI");
            ImgBytes = imgBytes.ToArray();
        }

        public override string Keyword => "BI";
        public override IList<PdfObject> GetOperands() => throw new NotImplementedException();
    }

}
