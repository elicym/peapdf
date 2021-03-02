/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SeaPeaYou.PeaPdf
{
    class PdfIndirectReference : PdfObject
    {
        public readonly ObjID ToObjID;

        public PdfIndirectReference(ObjID toObjID) => ToObjID = toObjID;

        public void WriteThis(PdfWriter w)
        {
            var twoNums = new TwoNums(ToObjID, "R");
            twoNums.Write(w);
        }
        internal override void Write(PdfWriter w, ObjID? encryptionObjID) => WriteThis(w);

        public override PdfObject Clone() => this; //being immutable

        public static PdfIndirectReference TryRead(PdfReader r)
        {
            var twoNums = TwoNums.TryRead(r, "R");
            if (twoNums == null)
                return null;

            return new PdfIndirectReference(new ObjID(twoNums.Num1, twoNums.Num2));
        }

    }
}
