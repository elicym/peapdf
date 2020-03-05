/*
 * Copyright 2020 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SeaPeaYou.PeaPdf
{
    class PdfName : PdfObject, IEquatable<PdfName>
    {

        public readonly byte[] Value;

        public PdfName(FParse fParse)
        {
            if (fParse.ReadByte() != '/')
                throw new FormatException();
            var byteList = new List<byte>();
            while (!fParse.AtEnd && !Utils.IsDelimiter(fParse.PeekByte))
            {
                var b = fParse.ReadByte();
                if (b == '#')
                {
                    byte byte1 = fParse.ReadByte(), byte2 = fParse.ReadByte();
                    byteList.Add((byte)(Utils.ReadHexDigit(byte1) * 16 + Utils.ReadHexDigit(byte2)));
                    continue;
                }
                byteList.Add(b);
            }
            Value = byteList.ToArray();
        }

        public PdfName(string str)
        {
            Value = str.Select(x => (byte)x).ToArray();
        }

        public override bool Equals(object obj)
        {
            var other = obj as PdfName;
            return Equals(other);
        }

        public bool Equals(PdfName other)
        {
            if (other == null)
                return false;
            return other.Value.SequenceEqual(Value);
        }

        public override int GetHashCode()
        {
            int hash = 17;
            foreach (var element in Value)
                hash = unchecked(hash * 31 + element);
            return hash;
        }

        public override string ToString()
        {
            return new string(Value.Select(x => (char)x).ToArray());
        }

        public override void Write(Stream stream, PDF pdf, PdfIndirectReference iRef)
        {
            stream.WriteByte((byte)'/');
            foreach (var b in Value)
            {
                var escape = Utils.IsDelimiter(b) || b == '#' || b < 33 || b > 126;
                if (escape)
                {
                    stream.WriteByte((byte)'#');
                    stream.WriteHex(b);
                }
                else
                {
                    stream.WriteByte(b);
                }
            }
            stream.WriteByte((byte)' ');
        }

        public static implicit operator PdfName(string name) => new PdfName(name);
        //don't use == or != - too complex
    }
}
