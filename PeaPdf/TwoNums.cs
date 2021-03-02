/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SeaPeaYou.PeaPdf
{
    class TwoNums
    {

        public TwoNums(int num1, int num2, string keyword)
        {
            Num1 = num1;
            Num2 = num2;
            Keyword = keyword;
        }

        public TwoNums(ObjID objID, string keyword) : this(objID.ObjNum, objID.GenNum, keyword) { }

        public void Write(PdfWriter w)
        {
            if (w.NeedsDeliminator)
                w.WriteByte(' ');
            w.WriteString(Num1.ToString());
            w.WriteByte(' ');
            w.WriteString(Num2.ToString());
            if (Keyword != null)
            {
                w.WriteByte(' ');
                w.WriteString(Keyword);
            }
            w.NeedsDeliminator = true;
        }

        public readonly int Num1, Num2;
        public readonly string Keyword;

        public static TwoNums TryRead(PdfReader r, string keyword)
        {
            var clone = r.Clone();

            int? num1 = ReadNum(clone);
            if (num1 == null)
                return null;

            if (clone.ReadByte() != ' ')
                return null;

            int? num2 = ReadNum(clone);
            if (num2 == null)
                return null;

            if (keyword != null)
            {
                if (clone.ReadByte() != ' ')
                    return null;
                var str = clone.ReadStringUntilDelimiter();
                if (str != keyword)
                    return null;
            }

            r.Pos = clone.Pos;

            return new TwoNums(num1.Value, num2.Value, keyword);
        }

        static int? ReadNum(PdfReader r)
        {
            var str = r.ReadStringUntilDelimiter();
            if (str.Length == 0 || str.Any(x => !char.IsDigit(x)))
                return null;
            return int.Parse(str);
        }

    }
}
