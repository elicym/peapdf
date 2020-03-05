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

    class FParse : ByteReader
    {

        public FParse(byte[] bytes, PDF pdf = null, int pos = 0) : base(bytes, pos) => this.pdf = pdf;

        public FParse(PDF pdf, int pos = 0) : base(pdf.GetBytes(), pos) => this.pdf = pdf;

        FParse(FParse from, int? pos) : base(from, pos) => this.pdf = from.pdf;

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

        public FParse Clone(int? pos = null) => new FParse(this, pos);

        public PdfObject ReadPdfObject(PdfIndirectReference iRef)
        {
            var pdfBool = PdfBool.TryRead(this);
            if (pdfBool != null)
                return pdfBool;

            var c = PeekByte;

            if (c == '[')
                return new PdfArray(this, iRef);

            if (c == '/')
                return new PdfName(this);

            if (PeekString("<<"))
            {
                var dict = new PdfDict(this, iRef);
                SkipWhiteSpace();
                if (PeekString("stream"))
                    return new PdfStream(dict, this, iRef);
                else
                    return dict;
            }

            if (c == '(' || c == '<')
                return new PdfString(this, iRef);

            var pdfIndirectReference = PdfIndirectReference.TryRead(this);
            if (pdfIndirectReference != null)
                return pdfIndirectReference;

            if (c == '+' || c == '-' || c == '.' || c >= '0' && c <= '9')
                return new PdfNumeric(this);

            if (ReadString("null"))
                return null;

            throw new FormatException();
        }

        public PdfIndirectReference ReadObjHeader(PdfIndirectReference iRef)
        {
            var twoNums = TwoNums.TryRead(this, "obj");
            if (twoNums == null)
                throw new FormatException();
            if (iRef != null && (twoNums.Num1 != iRef.ObjectNum || twoNums.Num2 != iRef.GenerationNum))
                throw new FormatException();
            if (iRef == null)
                iRef = new PdfIndirectReference(twoNums.Num1, twoNums.Num2);
            SkipWhiteSpace();
            return iRef;
        }

        public PdfObject Deref(PdfObject obj) => pdf != null ? pdf.Deref(obj) : obj;

        public byte[] Decrypt(byte[] bytes, PdfIndirectReference iRef) => pdf != null ? pdf.Decrypt(bytes, iRef) : bytes;

        PDF pdf;

    }
}
