/*
 * Copyright 2021 Elliott Cymerman
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

        public static PdfBool TryRead(PdfReader r)
        {
            if (r.ReadString("true"))
                return new PdfBool(true);
            if (r.ReadString("false"))
                return new PdfBool(false);
            return null;
        }

        internal override void Write(PdfWriter w, ObjID? encryptionObjID)
        {
            w.EnsureDeliminated();
            w.WriteString(Value ? "true" : "false");
            w.NeedsDeliminator = true;
        }

        public override PdfObject Clone() => new PdfBool(Value);

        public static explicit operator PdfBool(bool? b) => b == null ? null : new PdfBool(b.Value);

    }
}
