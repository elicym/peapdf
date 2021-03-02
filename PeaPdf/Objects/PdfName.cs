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
    //immutable
    class PdfName : PdfObject, IEquatable<PdfName>
    {

        public readonly IReadOnlyCollection<byte> Value;
        public readonly string String;

        internal PdfName(PdfReader r)
        {
            if (r.ReadByte() != '/')
                throw new FormatException();
            var byteList = new List<byte>();
            while (!r.AtEnd && !Utils.IsDelimiter(r.PeekByte))
            {
                var b = r.ReadByte();
                if (b == '#')
                {
                    byte byte1 = r.ReadByte(), byte2 = r.ReadByte();
                    byteList.Add((byte)(Utils.ReadHexDigit(byte1) * 16 + Utils.ReadHexDigit(byte2)));
                    continue;
                }
                byteList.Add(b);
            }
            var bytes = byteList.ToArray();
            Value = bytes;
            String = Encoding.UTF8.GetString(bytes);
        }

        public PdfName(string str)
        {
            Value = Encoding.UTF8.GetBytes(str);
            String = str;
        }

        public override bool Equals(object obj) => Equals(obj as PdfName);

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

        public override string ToString() => String;

        internal void WriteThis(PdfWriter w)  
        {
            w.WriteByte('/');
            foreach (var b in Value)
            {
                var escape = Utils.IsDelimiter(b) || b == '#' || b < 33 || b > 126;
                if (escape)
                {
                    w.WriteByte('#');
                    w.WriteHex(b);
                }
                else
                {
                    w.WriteByte(b);
                }
            }
            w.NeedsDeliminator = true;
        }
        internal override void Write(PdfWriter w, ObjID? encryptionObjID) => WriteThis(w);

        public override PdfObject Clone() => this; //being immutable

        public static explicit operator PdfName(string name) => name == null ? null : new PdfName(name);
        //don't use == or != - too complex
    }
}
