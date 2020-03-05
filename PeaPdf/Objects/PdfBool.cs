/*
 * Copyright 2020 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SeaPeaYou.PeaPdf
{
    class PdfBool : PdfObject
    {

        public readonly bool Value;

        public PdfBool(bool value)
        {
            Value = value;
        }

        public override string ToString() => Value.ToString();

        public static PdfBool TryRead(FParse fParse)
        {
            if (fParse.ReadString("true"))
                return new PdfBool(true);
            if (fParse.ReadString("false"))
                return new PdfBool(false);
            return null;
        }

        public override void Write(Stream stream, PDF pdf, PdfIndirectReference iRef)
        {
            stream.WriteString(Value ? "true" : "false");
            stream.WriteByte((byte)' ');
        }

    }
}
