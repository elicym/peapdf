/*
 * Copyright 2020 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SeaPeaYou.PeaPdf
{
    class PdfNumeric : PdfObject
    {
        public readonly decimal Value;

        public PdfNumeric(FParse fParse)
        {
            var str = fParse.ReadStringUntilDelimiter();
            Value = decimal.Parse(str);
        }

        public PdfNumeric(decimal value) => Value = value;

        public override string ToString() => Value.ToString();

        public override void Write(Stream stream, PDF pdf, PdfIndirectReference iRef)
        {
            stream.WriteString(Value.ToString());
            stream.WriteByte((byte)' ');
        }

    }
}
