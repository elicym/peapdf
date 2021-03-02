/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace SeaPeaYou.PeaPdf
{

    class PdfReader : ByteReader
    {

        public PdfReader(byte[] bytes, int pos = 0, PdfFile pdfVersion = null) : base(bytes, pos) => this.pdfVersion = pdfVersion;

        public PdfReader(PdfFile pdfVersion, int pos = 0) : base(pdfVersion.Bytes, pos) => this.pdfVersion = pdfVersion;

        PdfReader(PdfReader from, int? pos) : base(from, pos) => this.pdfVersion = from.pdfVersion;

        public void SkipWhiteSpace()
        {
            while (!AtEnd)
            {
                var b = PeekByte;
                if (!Utils.IsWhiteSpace(b) && b != '%')
                    break;
                Pos++;
                if (b == '%')
                {
                    while (!AtEnd)
                    {
                        if (Utils.IsEOL(ReadByte()))
                            break;
                    }
                    continue;
                }
            }
        }

        public bool ReadString(string str)
        {
            var res = PeekString(str);
            if (res)
                Pos += str.Length;
            return res;
        }

        public bool PeekString(string str)
        {
            if (Pos + str.Length > Bytes.Length)
                return false;
            for (int i = 0; i < str.Length; i++)
                if (Bytes[Pos + i] != str[i])
                    return false;
            return true;
        }

        public string ReadString(int count)
        {
            var res = PeekString(count);
            Pos += count;
            return res;
        }

        public string PeekString(int count)
        {
            if (Pos + count > Bytes.Length)
                throw new FormatException();
            var sb = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                sb.Append((char)ReadByte());
            }
            return sb.ToString();
        }

        public string ReadStringUntilDelimiter()
        {
            var sb = new StringBuilder();
            while (!AtEnd && !Utils.IsDelimiter(PeekByte))
            {
                sb.Append((char)ReadByte());
            }
            return sb.ToString();
        }

        public void ReadEOL()
        {
            if (PeekByte == '\n') Pos++;
            else if (PeekByte == '\r')
            {
                Pos++;
                if (PeekByte == '\n') Pos++;
            }
        }

        public int? Find(string str)
        {
            for (var p = Pos; p < Bytes.Length - str.Length; p++)
            {
                int i = 0;
                for (; i < str.Length; i++)
                {
                    if (Bytes[p + i] != str[i])
                        break;
                }
                if (i == str.Length)
                {
                    return p;
                }
            }
            return null;
        }

        public PdfReader Clone(int? pos = null) => new PdfReader(this, pos);

        public PdfObject ReadPdfObject(ObjID? baseObjID)
        {
            var pdfBool = PdfBool.TryRead(this);
            if (pdfBool != null)
                return pdfBool;

            var c = PeekByte;

            if (c == '[')
                return new PdfArray(this, baseObjID);

            if (c == '/')
                return new PdfName(this);

            if (PeekString("<<"))
            {
                var dict = new PdfDict(this, baseObjID);
                SkipWhiteSpace();
                if (PeekString("stream"))
                    return new PdfStream(dict, this, baseObjID);
                else
                    return dict;
            }

            if (c == '(' || c == '<')
                return new PdfString(this, baseObjID);

            var pdfIndirectReference = PdfIndirectReference.TryRead(this);
            if (pdfIndirectReference != null)
                return pdfIndirectReference;

            if (c == '+' || c == '-' || c == '.' || c >= '0' && c <= '9')
                return new PdfNumeric(this);

            if (ReadString("null"))
                return null;

            throw new FormatException();
        }

        public ObjID ReadObjHeader(ObjID? objID)
        {
            var twoNums = TwoNums.TryRead(this, "obj");
            if (twoNums == null)
                throw new FormatException();
            if (objID != null && (twoNums.Num1 != objID.Value.ObjNum || twoNums.Num2 != objID.Value.GenNum))
                throw new FormatException();
            if (objID == null)
                objID = new ObjID(twoNums.Num1, twoNums.Num2);
            SkipWhiteSpace();
            return objID.Value;
        }

        public PdfObject Deref(PdfObject obj) => pdfVersion != null ? pdfVersion.Deref(obj) : obj;

        public byte[] Decrypt(byte[] bytes, ObjID? objID) => pdfVersion != null ? pdfVersion.Decrypt(bytes, objID) : bytes;

        PdfFile pdfVersion;

    }
}
