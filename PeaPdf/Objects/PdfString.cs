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
    class PdfString : PdfObject
    {

        public readonly byte[] Value;

        public PdfString(FParse fParse, PdfIndirectReference iRef)
        {
            var byteList = new List<byte>();
            var firstChar = fParse.ReadByte();
            if (firstChar == '(')
            {
                int unClosed = 0;
                while (!fParse.AtEnd)
                {
                    var c = fParse.ReadByte();
                    switch (c)
                    {
                        case (byte)'(': unClosed++; goto default;
                        case (byte)')':
                            if (unClosed == 0)
                            {
                                Value = fParse.Decrypt(byteList.ToArray(), iRef);
                                return;
                            }
                            unClosed--;
                            goto default;
                        case (byte)'\\':
                            {
                                var n = fParse.ReadByte();
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
                                        if (!fParse.AtEnd && fParse.PeekByte == '\n')
                                        {
                                            fParse.Pos++;
                                        }
                                        break;
                                    case (byte)'\\': byteList.Add((byte)'\\'); break;
                                    default:
                                        {
                                            if (!Utils.IsDigit(n))
                                            {
                                                fParse.Pos--;
                                                continue; //just ignore slash
                                            }
                                            byte?[] digits = { n, null, null };
                                            if (Utils.IsDigit(fParse.PeekByte))
                                            {
                                                digits[1] = fParse.ReadByte();
                                                if (Utils.IsDigit(fParse.PeekByte))
                                                {
                                                    digits[2] = fParse.ReadByte();
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
                                if (fParse.PeekByte == '\n')
                                {
                                    fParse.Pos++;
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
                while (!fParse.AtEnd)
                {
                    var c = fParse.ReadByte();
                    if (c == '>')
                    {
                        if (prevChar != null)
                        {
                            byteList.Add((byte)(Utils.ReadHexDigit(prevChar.Value) * 16));
                            prevChar = null;
                        }
                        Value = fParse.Decrypt(byteList.ToArray(), iRef);
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

        public override string ToString()
        {
            return Encoding.UTF8.GetString(Value);
        }

        public override void Write(Stream stream, PDF pdf, PdfIndirectReference iRef)
        {
            if (Value.All(x => (x >= ' ' && x <= '~') || x == '\r' || x == '\n'))
            {
                stream.WriteByte((byte)'(');
                foreach (var b in Value)
                {
                    switch (b)
                    {
                        case (byte)'\r':
                            stream.WriteByte((byte)'\\');
                            stream.WriteByte((byte)'r');
                            break;
                        case (byte)'\n':
                            stream.WriteByte((byte)'\\');
                            stream.WriteByte((byte)'n');
                            break;
                        case (byte)'(':
                            stream.WriteByte((byte)'\\');
                            stream.WriteByte((byte)'(');
                            break;
                        case (byte)')':
                            stream.WriteByte((byte)'\\');
                            stream.WriteByte((byte)')');
                            break;
                        case (byte)'\\':
                            stream.WriteByte((byte)'\\');
                            stream.WriteByte((byte)'\\');
                            break;
                        default:
                            stream.WriteByte(b);
                            break;
                    }
                }
                stream.WriteByte((byte)')');
            }
            else
            {
                stream.WriteByte((byte)'<');
                foreach (var b in Value)
                {
                    stream.WriteHex(b);
                }
                stream.WriteByte((byte)'>');

            }
        }

    }
}
