/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SeaPeaYou.PeaPdf
{

    //immutable
    class PdfNumeric : PdfObject
    {
        public readonly decimal Value;

        public PdfNumeric(PdfReader r)
        {
            var str = r.ReadStringUntilDelimiter();
            Value = decimal.Parse(str);
        }

        public PdfNumeric(decimal value) => Value = value;

        public override string ToString() => Value.ToString();

        internal override void Write(PdfWriter w, ObjID? encryptionObjID)
        {
            //if (++i > 2000) Debugger.Break();
            if (w.NeedsDeliminator)
                w.WriteByte(' ');
            w.WriteString(Value.ToString());
            w.NeedsDeliminator = true;
        }

        public override PdfObject Clone() => this; //being immutable

        public static explicit operator PdfNumeric(int? v) => v == null ? null : new PdfNumeric(v.Value);
        public static explicit operator PdfNumeric(decimal? v) => v == null ? null : new PdfNumeric(v.Value);
        public static explicit operator PdfNumeric(float? v) => v == null ? null : new PdfNumeric((decimal)v.Value);

    }
}
