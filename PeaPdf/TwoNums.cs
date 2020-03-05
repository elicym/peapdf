/*
 * Copyright 2020 Elliott Cymerman
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

        public void Write(Stream stream, bool terminateWithSpace)
        {
            stream.WriteString(Num1.ToString());
            stream.WriteByte((byte)' ');
            stream.WriteString(Num2.ToString());
            if (Keyword != null)
            {
                stream.WriteByte((byte)' ');
                stream.WriteString(Keyword);
            }
            stream.WriteByte((byte)(terminateWithSpace ? ' ' : '\n'));
        }

        public readonly int Num1, Num2;
        public readonly string Keyword;

        public static TwoNums TryRead(FParse fParse, string keyword)
        {
            var clone = fParse.Clone();

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
                if (!clone.ReadString(keyword))
                    return null;
            }

            fParse.Pos = clone.Pos;

            return new TwoNums(num1.Value, num2.Value, keyword);
        }

        static int? ReadNum(FParse fParse)
        {
            var str = fParse.ReadStringUntilDelimiter();
            if (str.Length == 0 || str.Any(x => !char.IsDigit(x)))
                return null;
            return int.Parse(str);
        }

    }
}
