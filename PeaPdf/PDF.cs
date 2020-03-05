/*
 * Copyright 2020 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using SkiaSharp;
using System.Diagnostics;
using System.Security.Cryptography;

namespace SeaPeaYou.PeaPdf
{
    public class PDF
    {

        public PDF(byte[] bytes, string password = null)
        {

            orgBytes = bytes;
            //read header
            var fParse = new FParse(this);
            if (!fParse.ReadString("%PDF-"))
                throw new FormatException();
            var version = new PdfNumeric(fParse);
            //read footer
            fParse.Pos = bytes.Length - 5;
            int loopUntil = bytes.Length - 1024;
            for (; fParse.Pos > loopUntil; fParse.Pos--)
            {
                if (fParse.PeekString("%%EOF"))
                    break;
            }
            if (fParse.Pos == loopUntil) throw new FormatException("no EOF");
            loopUntil = fParse.Pos - 30;
            for (; fParse.Pos > loopUntil; fParse.Pos--)
            {
                if (fParse.PeekString("startxref"))
                    break;
            }
            if (fParse.Pos == loopUntil) throw new FormatException("startxref");
            fParse.ReadStringUntilDelimiter();
            fParse.SkipWhiteSpace();
            int startxref = int.Parse(fParse.ReadStringUntilDelimiter());
            //read xRef
            fParse.Pos = startxref;
            xRef = new XRef(fParse);
            catalog = (PdfDict)xRef.TrailerDict["Root"];
            var encryptDict = (PdfDict)xRef.TrailerDict["Encrypt"];
            if (encryptDict != null)
            {
                if (!encryptDict["Filter"].As<PdfName>().Equals("Standard"))
                    throw new NotImplementedException("filter");
                PdfString O = (PdfString)encryptDict["O"], U = (PdfString)encryptDict["U"];
                int P = (int)encryptDict["P"];

                var paddedPwd = new byte[32];
                var paddingNeeded = paddedPwd.Length;
                if (password != null)
                {
                    var pwdBytes = Encoding.ASCII.GetBytes(password);
                    Array.Copy(pwdBytes, paddedPwd, Math.Min(pwdBytes.Length, paddedPwd.Length));
                    paddingNeeded -= pwdBytes.Length;
                }
                if (paddingNeeded > 0)
                {
                    Array.Copy(paddingBytes, 0, paddedPwd, paddedPwd.Length - paddingNeeded, paddingNeeded);
                }

                var md5Input = new List<byte>();
                md5Input.AddRange(paddedPwd);
                md5Input.AddRange(O.Value);
                md5Input.AddRange(BitConverter.GetBytes(P));
                var fileIDBytes = xRef.TrailerDict["ID"].As<PdfArray>()[0].As<PdfString>().Value;
                md5Input.AddRange(fileIDBytes);
                var md5 = MD5.Create();
                var hash = md5.ComputeHash(md5Input.ToArray());
                var lengthObj = encryptDict["Length"];
                var n = (lengthObj != null ? (int)lengthObj : 40) / 8;
                for (int i = 0; i < 50; i++)
                {
                    var firstNBytes = hash.Take(n).ToArray();
                    hash = md5.ComputeHash(firstNBytes);
                }
                var key = hash.Take(n).ToArray();

                md5Input.Clear();
                md5Input.AddRange(paddingBytes);
                md5Input.AddRange(fileIDBytes);
                var h1 = md5.ComputeHash(md5Input.ToArray());
                var b1 = RC4.Encrypt(key, h1);
                for (byte i = 1; i <= 19; i++)
                {
                    var key1 = (byte[])key.Clone();
                    for (int j = 0; j < key1.Length; j++)
                    {
                        key1[j] ^= i;
                    }
                    b1 = RC4.Encrypt(key1, b1);
                }
                if (!b1.SequenceEqual(U.Value.Take(b1.Length)))
                    throw new Exception("invalid password");

                encryptionKey = key;
            }

            pageTreeRoot = new PageTreeNode(this, (PdfDict)catalog["Pages"]);

        }

        public SKImage Render(int pageNum, float scale)
        {
            var page = pageTreeRoot.GetPage(pageNum);
            return new Renderer(this, page, scale).SKImage;
        }

        internal byte[] GetBytes() => orgBytes;

        internal PdfObject Deref(PdfObject obj)
        {
            if (obj == null)
                return null;
            if (obj is PdfIndirectReference iRef)
                return GetObj(iRef);
            return obj;
        }

        public int PageCount => pageTreeRoot.Count;

        readonly byte[] orgBytes;
        readonly XRef xRef;
        readonly PdfDict catalog;
        readonly byte[] encryptionKey;
        readonly PageTreeNode pageTreeRoot;
        readonly Dictionary<PdfIndirectReference, PdfObject> objDict = new Dictionary<PdfIndirectReference, PdfObject>(); //TODO only save root xRef
        readonly Dictionary<int, ObjectStream> objectStreamDict = new Dictionary<int, ObjectStream>();

        internal PdfObject GetPageObj(PdfDict page, PdfName key)
        {
            var _page = page;
            do
            {
                var obj = _page[key];
                if (obj != null)
                    return obj;
                _page = (PdfDict)_page["Parent"];
            } while (_page != null);
            return null;
        }

        internal byte[] Decrypt(byte[] bytes, PdfIndirectReference iRef)
        {
            if (encryptionKey == null || iRef == null)
                return bytes;
            var key = encryptionKey.ToList();
            key.AddRange(BitConverter.GetBytes(iRef.ObjectNum).Take(3));
            key.AddRange(BitConverter.GetBytes(iRef.GenerationNum).Take(2));
            var md5 = MD5.Create();
            var finalKey = md5.ComputeHash(key.ToArray()).Take(encryptionKey.Length + 5).ToArray();
            return RC4.Decrypt(finalKey.ToArray(), bytes);
        }

        internal byte[] Encrypt(byte[] bytes, PdfIndirectReference iRef)
        {
            if (encryptionKey == null || iRef == null)
                return bytes;
            var key = encryptionKey.ToList();
            key.AddRange(BitConverter.GetBytes(iRef.ObjectNum).Take(3));
            key.AddRange(BitConverter.GetBytes(iRef.GenerationNum).Take(2));
            var md5 = MD5.Create();
            var finalKey = md5.ComputeHash(key.ToArray()).Take(encryptionKey.Length + 5).ToArray();
            return RC4.Encrypt(finalKey.ToArray(), bytes);
        }

        internal PdfObject GetObj(PdfIndirectReference iRef)
        {
            if (!objDict.TryGetValue(iRef, out var res))
            {
                var entry = xRef.GetObjOffset(iRef);
                if (entry != null)
                {
                    switch (entry.Type)
                    {
                        case XRefEntryType.Free: break;
                        case XRefEntryType.InUse:
                            {
                                if (entry.GenNum != iRef.GenerationNum)
                                    break;
                                var fParse = new FParse(this, entry.Offset);
                                fParse.SkipWhiteSpace();
                                fParse.ReadObjHeader(iRef);
                                res = fParse.ReadPdfObject(iRef);
                                break;
                            }
                        case XRefEntryType.Compressed:
                            {
                                var objStm = GetObjectStream(entry.Offset);
                                res = objStm.GetObj(entry.GenNum);
                                break;
                            }
                    }
                }

                objDict.Add(iRef, res);
            }
            return res;
        }

        ObjectStream GetObjectStream(int objNum)
        {
            if (!objectStreamDict.TryGetValue(objNum, out var objStm))
            {
                var s = (PdfStream)GetObj(new PdfIndirectReference(objNum, 0));
                objStm = new ObjectStream(this, s);
                objectStreamDict.Add(objNum, objStm);
            }
            return objStm;
        }

        class ObjectStream
        {
            PDF pdf;
            (int objNum, int offset)[] objOffsets;
            byte[] bytes;
            int first;

            public ObjectStream(PDF pdf, PdfStream objStm)
            {
                this.pdf = pdf;
                var n = (int)objStm.Dict["N"];
                first = (int)objStm.Dict["First"];
                objOffsets = new (int, int)[n];
                bytes = objStm.GetBytes();
                var fParse = new FParse(bytes);
                for (int i = 0; i < n; i++)
                {
                    int objNum = (int)new PdfNumeric(fParse);
                    fParse.SkipWhiteSpace();
                    int offset = (int)new PdfNumeric(fParse);
                    fParse.SkipWhiteSpace();
                    objOffsets[i] = (objNum, offset);
                }
            }

            public PdfObject GetObj(int index)
            {
                var fParse = new FParse(bytes, pdf, first + objOffsets[index].offset);
                return fParse.ReadPdfObject(null);
            }
        }

        static byte[] paddingBytes = new byte[] { 0x28, 0xBF, 0x4E, 0x5E, 0x4E, 0x75, 0x8A, 0x41, 0x64, 0x00, 0x4E, 0x56, 0xFF, 0xFA, 0x01, 0x08, 0x2E, 0x2E, 0x00, 0xB6, 0xD0, 0x68, 0x3E, 0x80, 0x2F, 0x0C, 0xA9, 0xFE, 0x64, 0x53, 0x69, 0x7A };

        class XRef
        {
            public readonly PdfDict TrailerDict;

            readonly List<Section> sections;
            readonly XRef prevXref;
            List<(int objNum, int count)> streamSections;
            List<XRefEntry> streamEntries;

            public XRef(FParse fParse)
            {
                if (!fParse.ReadString("xref"))
                {
                    var iRef = fParse.ReadObjHeader(null);
                    TrailerDict = new PdfDict(fParse, iRef);
                    streamSections = new List<(int objNum, int count)>();
                    var indexObj = (PdfArray)TrailerDict["Index"];
                    if (indexObj != null)
                    {
                        for (int i = 0; i < indexObj.Length; i += 2)
                        {
                            streamSections.Add(((int)indexObj[i], (int)indexObj[i + 1]));
                        }
                    }
                    else
                    {
                        streamSections.Add((0, (int)TrailerDict["Size"]));
                    }
                    var streamWidths = TrailerDict["W"].As<PdfArray>().Select(x => (int)x).ToArray();
                    fParse.SkipWhiteSpace();
                    var bytes = new PdfStream(TrailerDict, fParse, null/*xref is not encrypted*/).GetBytes();
                    var byteReader = new ByteReader(bytes);
                    var totalEntries = streamSections.Sum(x => x.count);
                    streamEntries = new List<XRefEntry>();
                    for (int i = 0; i < totalEntries; i++)
                    {
                        var nums = streamWidths.Select(x => byteReader.ReadBytes(x)).ToList();
                        var entry = new XRefEntry((XRefEntryType)nums[0], nums[1], nums[2]);
                        streamEntries.Add(entry);
                    }
                }
                else
                {
                    sections = new List<Section>();
                    while (true)
                    {
                        fParse.SkipWhiteSpace();
                        var twoNums = TwoNums.TryRead(fParse, null);
                        if (twoNums == null)
                            break;
                        fParse.SkipWhiteSpace();
                        var section = new Section(twoNums.Num1, twoNums.Num2, fParse);
                        sections.Add(section);
                    }

                    if (TrailerDict == null)
                    {
                        if (!fParse.ReadString("trailer"))
                            throw new FormatException();
                        fParse.SkipWhiteSpace();
                        TrailerDict = new PdfDict(fParse, null);
                    }
                }

                PdfObject prevIX = TrailerDict["Prev"];
                if (prevIX != null)
                {
                    prevXref = new XRef(fParse.Clone((int)prevIX));
                }
                else
                {
                    if (sections != null && sections[0].StartNum > 0) //sometimes StartNum is wrongly > 0
                    {
                        sections[0].StartNum = 0;
                        sections[0].EndNum = sections[0].StartNum + sections[0].Count;
                    }
                }
            }

            //public XRef(List<XRefSection> sections)
            //{
            //    Sections = sections;
            //}

            public void Write(Stream stream)
            {
                stream.WriteString("xref\n");
                foreach (var sect in sections.OrderBy(x => x.StartNum))
                {
                    sect.Write(stream);
                }
            }

            public XRefEntry GetObjOffset(PdfIndirectReference pdfRef)
            {
                if (sections != null)
                {
                    foreach (var section in sections)
                    {
                        if (section.ContainsObjNum(pdfRef.ObjectNum))
                        {
                            var entry = section.GetEntry(pdfRef.ObjectNum);
                            return entry;
                        }
                    }
                }
                else
                {
                    var cSoFar = 0;
                    foreach (var section in streamSections)
                    {
                        if (pdfRef.ObjectNum >= section.objNum && pdfRef.ObjectNum < (section.objNum + section.count))
                        {
                            var entry = streamEntries[cSoFar + (pdfRef.ObjectNum - section.objNum)];
                            return entry;
                        }
                        cSoFar += section.count;
                    }
                }
                if (prevXref != null)
                    return prevXref.GetObjOffset(pdfRef);
                return null;
            }

            class Section
            {
                public int StartNum, EndNum, Count;

                byte[] bytes;

                public Section(int startNum, int count, FParse fParse)
                {
                    StartNum = startNum;
                    Count = count;
                    EndNum = StartNum + Count;
                    bytes = fParse.ReadByteArray(20 * count);
                }

                public Section(int startNum, int count, IEnumerable<(int offset, int genNum)> lines)
                {
                    StartNum = startNum;
                    Count = count;
                    EndNum = StartNum + count;
                    var ms = new MemoryStream();
                    foreach (var line in lines)
                    {
                        ms.WriteString(line.offset.ToString("d10"));
                        ms.WriteByte((byte)' ');
                        ms.WriteString(line.genNum.ToString("d5"));
                        ms.WriteByte((byte)' ');
                        ms.WriteByte((byte)'n');
                        ms.WriteByte((byte)' ');
                        ms.WriteByte((byte)'\n');
                    }
                    bytes = ms.ToArray();
                }

                public XRefEntry GetEntry(int objNum)
                {
                    var byteIX = (objNum - StartNum) * 20;
                    var entry = new XRefEntry(
                        GetString(byteIX + 17, 1) == "n" ? XRefEntryType.InUse : XRefEntryType.Free,
                        int.Parse(GetString(byteIX, 10)),
                        int.Parse(GetString(byteIX + 11, 5))
                    );
                    return entry;
                }

                public bool ContainsObjNum(int objNum) => objNum >= StartNum && objNum < EndNum;

                public string GetString(int byteIX, int count)
                {
                    var sb = new StringBuilder();
                    for (var i = 0; i < count; i++)
                        sb.Append((char)bytes[byteIX + i]);
                    return sb.ToString();
                }

                public void Write(Stream stream)
                {
                    var twoNums = new TwoNums(StartNum, Count, null);
                    twoNums.Write(stream, false);
                    stream.Write(bytes);
                }
            }

        }

        enum XRefEntryType { Free, InUse, Compressed }

        class XRefEntry
        {
            public readonly int Offset, GenNum;
            public readonly XRefEntryType Type;

            public XRefEntry(XRefEntryType type, int offset, int genNum)
            {
                Type = type;
                Offset = offset;
                GenNum = genNum;
            }
        }

    }

}
