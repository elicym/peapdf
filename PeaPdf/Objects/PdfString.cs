/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Collections.ObjectModel;

namespace SeaPeaYou.PeaPdf
{

    //immutable
    class PdfString : PdfObject
    {

        public readonly byte[] Value; //should be IReadOnlyList, but causes issues, eg can't use Array.Copy on it

        public PdfString(PdfReader r, ObjID? baseObjID)
        {
            var byteList = new List<byte>();
            var firstChar = r.ReadByte();
            if (firstChar == '(')
            {
                int unClosed = 0;
                while (!r.AtEnd)
                {
                    var c = r.ReadByte();
                    switch (c)
                    {
                        case (byte)'(': unClosed++; goto default;
                        case (byte)')':
                            if (unClosed == 0)
                            {
                                Value = r.Decrypt(byteList.ToArray(), baseObjID);
                                return;
                            }
                            unClosed--;
                            goto default;
                        case (byte)'\\':
                            {
                                var n = r.ReadByte();
                                switch (n)
                                {
                                    case (byte)'n': byteList.Add((byte)'\n'); break;
                                    case (byte)'r': byteList.Add((byte)'\r'); break;
                                    case (byte)'t': byteList.Add((byte)'\t'); break;
                                    case (byte)'b': byteList.Add((byte)'\b'); break;
                                    case (byte)'f': byteList.Add((byte)'\f'); break;
                                    case (byte)'(': byteList.Add((byte)'('); break;
                                    case (byte)')': byteList.Add((byte)')'); break;
                                    case (byte)'\n': break;
                                    case (byte)'\r':
                                        if (!r.AtEnd && r.PeekByte == '\n')
                                        {
                                            r.Pos++;
                                        }
                                        break;
                                    case (byte)'\\': byteList.Add((byte)'\\'); break;
                                    default:
                                        {
                                            if (!Utils.IsDigit(n))
                                            {
                                                r.Pos--;
                                                continue; //just ignore slash
                                            }
                                            byte?[] digits = { n, null, null };
                                            if (Utils.IsDigit(r.PeekByte))
                                            {
                                                digits[1] = r.ReadByte();
                                                if (Utils.IsDigit(r.PeekByte))
                                                {
                                                    digits[2] = r.ReadByte();
                                                }
                                            }

                                            int numDigits = digits[2] != null ? 3 : (digits[1] != null ? 2 : 1);
                                            int v = 0;
                                            for (var digitIX = 1; digitIX <= numDigits; digitIX++)
                                                v += (int)Math.Pow(8, numDigits - digitIX)/*PERF*/ * (digits[digitIX - 1].Value - '0');
                                            byteList.Add((byte)v);
                                            break;
                                        }
                                }
                                break;
                            }
                        case (byte)'\r':
                            {
                                if (r.PeekByte == '\n')
                                {
                                    r.Pos++;
                                }
                                byteList.Add((byte)'\n');
                                break;
                            }
                        default: byteList.Add(c); break;
                    }
                }
                throw new FormatException();
            }
            if (firstChar == '<')
            {
                byte? prevChar = null;
                while (!r.AtEnd)
                {
                    var c = r.ReadByte();
                    if (c == '>')
                    {
                        if (prevChar != null)
                        {
                            byteList.Add((byte)(Utils.ReadHexDigit(prevChar.Value) * 16));
                            prevChar = null;
                        }
                        Value = r.Decrypt(byteList.ToArray(), baseObjID);
                        return;
                    }
                    if (Utils.IsWhiteSpace(c))
                        continue;
                    if ((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))
                    {
                        if (prevChar == null)
                            prevChar = c;
                        else
                        {
                            byteList.Add((byte)(Utils.ReadHexDigit(prevChar.Value) * 16 + Utils.ReadHexDigit(c)));
                            prevChar = null;
                        }
                        continue;
                    }
                    throw new FormatException();
                }
                throw new FormatException();
            }
            throw new FormatException();
        }

        public PdfString(string str)
        {
            var ascii = str.All(x => x < 128);
            if (ascii)
            {
                Value = Encoding.ASCII.GetBytes(str);
            }
            else
            {
                var bytes = Encoding.UTF8.GetBytes(str);
                var value = new byte[bytes.Length + 3];
                value[0] = 239; value[1] = 187; value[2] = 191;
                bytes.CopyTo(value.AsSpan(3));
                Value = value;
            }
        }

        public PdfString(byte[] bytes) => Value = bytes;

        public override string ToString()
        {
            var bytes = Value.ToArray();
            if (bytes.Length >= 2 && bytes[0] == 254 && bytes[1] == 255)
            {
                for (int i = 0; i < bytes.Length; i += 2)
                {
                    var tmp = bytes[i];
                    bytes[i] = bytes[i + 1];
                    bytes[i + 1] = tmp;
                }
                return Encoding.Unicode.GetString(bytes);
            }
            if (bytes.Length >= 3 && bytes[0] == 239 && bytes[1] == 187 && bytes[2] == 191)
            {
                return Encoding.UTF8.GetString(bytes);
            }
            return Encoding.ASCII.GetString(bytes);
        }

        internal override void Write(PdfWriter w, ObjID? encryptionObjID)
        {
            if (Value.All(x => (x >= ' ' && x <= '~') || x == '\r' || x == '\n'))
            {
                w.WriteByte('(');
                foreach (var b in Value)
                {
                    switch (b)
                    {
                        case (byte)'\r':
                            w.WriteByte('\\');
                            w.WriteByte('r');
                            break;
                        case (byte)'\n':
                            w.WriteByte('\\');
                            w.WriteByte('n');
                            break;
                        case (byte)'(':
                            w.WriteByte('\\');
                            w.WriteByte('(');
                            break;
                        case (byte)')':
                            w.WriteByte('\\');
                            w.WriteByte(')');
                            break;
                        case (byte)'\\':
                            w.WriteByte('\\');
                            w.WriteByte('\\');
                            break;
                        default:
                            w.WriteByte(b);
                            break;
                    }
                }
                w.WriteByte(')');
            }
            else
            {
                w.WriteByte('<');
                foreach (var b in Value)
                {
                    w.WriteHex(b);
                }
                w.WriteByte('>');

            }
        }

        public override PdfObject Clone() => this; //being immutable

        public static explicit operator PdfString(string str) => str == null ? null : new PdfString(str);
        public static explicit operator PdfString(byte[] bytes) => new PdfString(bytes);


    }
}
