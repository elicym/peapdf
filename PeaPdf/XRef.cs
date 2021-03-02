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
    abstract class XRef
    {
        public readonly int NextObjNum;
        public readonly bool IsStream;
        public readonly int Offset;
        public abstract W.FileTrailer FileTrailer { get; }

        public static XRef Read(PdfReader r)
        {
            var isStream = !r.ReadString("xref");
            if (isStream)
                return new XRefStream(r);
            else
                return new XRefTable(r);
        }

        public XRef(PdfReader r)
        {
            Offset = r.Pos;
        }

        public XRef(int offset) => Offset = offset;

        protected List<Section> GetSections(List<XRefInUseEntry> entries)
        {
            entries.Sort((a, b) => a.ObjID.ObjNum.CompareTo(b.ObjID.ObjNum));
            var sections = new List<Section>();
            for (int i = 0; i < entries.Count; i++)
            {
                int startNum = entries[i].ObjID.ObjNum, count = 0, nextIX;
                do
                {
                    count++;
                    nextIX = i + count;
                } while (nextIX < entries.Count && entries[nextIX].ObjID.ObjNum == startNum + count);
                var section = new Section(startNum, count, entries.Skip(i).Take(count).Select(x => (x.Offset, x.ObjID.GenNum)).ToList());
                sections.Add(section);
                i = nextIX - 1;
            }
            return sections;
        }

        public abstract void Write(PdfWriter w);

        public abstract XRefEntry GetObjOffset(ObjID objID);

        public abstract XRef CreateAdditional(List<XRefInUseEntry> entries, int offset);

        public class XRefTable : XRef
        {
            readonly List<TableSection> tableSections = new List<TableSection>();
            readonly XRefTable prevXRef;
            readonly W.FileTrailer fileTrailer;
            public override W.FileTrailer FileTrailer => fileTrailer;

            public XRefTable(PdfReader r) : base(r)
            {
                r.ReadString("xref");
                while (true)
                {
                    r.SkipWhiteSpace();
                    var twoNums = TwoNums.TryRead(r, null);
                    if (twoNums == null)
                        break;
                    r.SkipWhiteSpace();
                    var section = new TableSection(twoNums.Num1, twoNums.Num2, r);
                    tableSections.Add(section);
                }

                if (!r.ReadString("trailer"))
                    throw new FormatException();
                r.SkipWhiteSpace();
                fileTrailer = new W.FileTrailer(new PdfDict(r, null));

                if (fileTrailer.Prev != null)
                {
                    prevXRef = new XRefTable(r.Clone(fileTrailer.Prev));
                }
                else
                {
                    //a single xref must start from 0, yet sometimes it pretends it doesn't
                    tableSections[0].EndObjNum -= tableSections[0].StartObjNum;
                    tableSections[0].StartObjNum = 0;
                }

            }

            public XRefTable(List<XRefInUseEntry> entries, int offset, W.FileTrailer fileTrailer) : base(offset)
            {
                this.fileTrailer = fileTrailer;
                var section = GetSections(entries).Single();
                var tableSection = new TableSection(section, true);
                tableSections.Add(tableSection);
            }

            public XRefTable(List<XRefInUseEntry> entries, int offset, XRefTable prevXRef) : base(offset)
            {
                var sections = GetSections(entries);
                foreach (var section in sections)
                {
                    var tableSection = new TableSection(section);
                    tableSections.Add(tableSection);
                }

                this.prevXRef = prevXRef;
                fileTrailer = new W.FileTrailer(new PdfDict(prevXRef.fileTrailer.Dict));
                fileTrailer.Prev = prevXRef.Offset;
                if (sections.Count > 0)
                {
                    var lastSection = sections[sections.Count - 1];
                    fileTrailer.Size = Math.Max(prevXRef.fileTrailer.Size.Value, lastSection.StartObjNum + lastSection.Size);
                }
            }

            public override void Write(PdfWriter w)
            {
                var startxref = w.Count;
                w.WriteString("xref");
                w.WriteNewLine();
                foreach (var ts in tableSections)
                {
                    ts.Write(w);
                }
                w.WriteString("trailer");
                w.WriteNewLine();
                FileTrailer.Dict.Write(w, null);
                w.WriteNewLine();
                w.WriteString("startxref");
                w.WriteNewLine();
                w.WriteString(startxref.ToString());
                w.WriteNewLine();
                w.WriteString("%%EOF");
            }

            public override XRefEntry GetObjOffset(ObjID objID)
            {
                foreach (var section in tableSections)
                {
                    if (section.ContainsObjNum(objID.ObjNum))
                    {
                        var entry = section.GetEntry(objID.ObjNum);
                        return entry;
                    }
                }
                if (prevXRef != null)
                    return prevXRef.GetObjOffset(objID);
                return null;
            }

            public override XRef CreateAdditional(List<XRefInUseEntry> entries, int offset)
            {
                return new XRefTable(entries, offset, this);
            }

            class TableSection
            {
                public int StartObjNum, EndObjNum, Size;

                byte[] bytes;
                IEnumerable<(int offset, int genNum)> lines;

                public TableSection(int startObjNum, int size, PdfReader r)
                {
                    StartObjNum = startObjNum;
                    Size = size;
                    EndObjNum = StartObjNum + Size;
                    bytes = r.ReadByteArray(20 * size);
                }

                public TableSection(Section section, bool isFirst = false)
                {
                    StartObjNum = section.StartObjNum;
                    Size = section.Size;
                    EndObjNum = StartObjNum + section.Size;
                    lines = section.Entries;
                    var ms = new MemoryStream();
                    if (isFirst)
                    {
                        ms.WriteString("0000000000 65535 f \n");
                        StartObjNum--;
                        Size++;
                    }
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
                    var byteIX = (objNum - StartObjNum) * 20;
                    var entry = new XRefEntry(
                        GetString(byteIX + 17, 1) == "n" ? XRefEntryType.InUse : XRefEntryType.Free,
                        int.Parse(GetString(byteIX, 10)),
                        int.Parse(GetString(byteIX + 11, 5))
                    );
                    return entry;
                }

                public bool ContainsObjNum(int objNum) => objNum >= StartObjNum && objNum < EndObjNum;

                public string GetString(int byteIX, int count)
                {
                    var sb = new StringBuilder();
                    for (var i = 0; i < count; i++)
                        sb.Append((char)bytes[byteIX + i]);
                    return sb.ToString();
                }

                public void Write(PdfWriter w)
                {
                    var twoNums = new TwoNums(StartObjNum, Size, null);
                    twoNums.Write(w);
                    w.WriteNewLine();
                    w.WriteBytes(bytes);
                }

            }

        }

        public class XRefStream : XRef
        {
            readonly IList<(int startObjNum, int size)> streamSections;
            readonly List<XRefEntry> streamEntries = new List<XRefEntry>();
            readonly XRefStream prevXRef;
            readonly W.FileTrailerStream fileTrailerStream;
            readonly byte[] bytes;
            public override W.FileTrailer FileTrailer => fileTrailerStream.FileTrailer;
            public readonly PdfStream PdfStream;

            public XRefStream(PdfReader r) : base(r)
            {
                var iRef = r.ReadObjHeader(null);
                fileTrailerStream = new W.FileTrailerStream(new W.FileTrailer(new PdfDict(r, null)));
                streamSections = fileTrailerStream.Index;
                if (streamSections == null)
                    streamSections = new List<(int, int)> { (0, fileTrailerStream.FileTrailer.Size.Value) };
                r.SkipWhiteSpace();
                bytes = new PdfStream(fileTrailerStream.FileTrailer.Dict, r, null/*xref is not encrypted*/).GetDecodedBytes();
                var byteReader = new ByteReader(bytes);
                var totalEntries = streamSections.Sum(x => x.size);
                var streamWidths = fileTrailerStream.W;
                for (int i = 0; i < totalEntries; i++)
                {
                    var entry = new XRefEntry((XRefEntryType)byteReader.ReadBytes(streamWidths.Item1), byteReader.ReadBytes(streamWidths.Item2), byteReader.ReadBytes(streamWidths.Item3));
                    streamEntries.Add(entry);
                }

                if (fileTrailerStream.FileTrailer.Prev != null)
                {
                    prevXRef = new XRefStream(r.Clone(fileTrailerStream.FileTrailer.Prev));
                }

            }

            XRefStream(List<XRefInUseEntry> entries, int offset, XRefStream prevXRef) : base(offset)
            {
                var nextObjNum = Math.Max(entries.Max(x => x.ObjID.ObjNum) + 1, prevXRef.fileTrailerStream.FileTrailer.Size.Value);
                entries.Add(new XRefInUseEntry(offset, new ObjID(nextObjNum, 0))); //for xref object
                var sections = GetSections(entries);
                if (sections.Count > 0)
                {
                    var lastSection = sections[sections.Count - 1];
                }

                var ms = new MemoryStream();
                streamSections = new List<(int, int)>();
                foreach (var section in sections)
                {
                    streamSections.Add((section.StartObjNum, section.Size));
                    foreach (var entry in section.Entries)
                    {
                        streamEntries.Add(new XRefEntry(XRefEntryType.InUse, entry.offset, entry.genNum));
                        ms.WriteByte(1);
                        ms.WriteByte((byte)(entry.offset >> 24));
                        ms.WriteByte((byte)(entry.offset >> 16));
                        ms.WriteByte((byte)(entry.offset >> 8));
                        ms.WriteByte((byte)(entry.offset >> 0));
                        ms.WriteByte((byte)entry.genNum);
                    }
                }
                bytes = ms.ToArray();

                this.prevXRef = prevXRef;
                fileTrailerStream = new W.FileTrailerStream(new W.FileTrailer(new PdfDict(prevXRef.fileTrailerStream.FileTrailer.Dict)));
                fileTrailerStream.FileTrailer.Prev = prevXRef.Offset;
                fileTrailerStream.FileTrailer.Size = nextObjNum + 1;
                if (sections.Count > 0)
                {
                    var lastSection = sections[sections.Count - 1];
                    fileTrailerStream.FileTrailer.Size = Math.Max(prevXRef.fileTrailerStream.FileTrailer.Size.Value, lastSection.StartObjNum + lastSection.Size);
                }
                var index = new PdfObject[sections.Count * 2];
                for (int i = 0; i < sections.Count; i++)
                {
                    var section = sections[i];
                    index[i * 2] = (PdfNumeric)section.StartObjNum;
                    index[i * 2 + 1] = (PdfNumeric)section.Size;
                }
                fileTrailerStream.Index = sections.Select(x => (x.StartObjNum, x.Size)).ToList();
                fileTrailerStream.FileTrailer.Dict["Filter"] = null;
                fileTrailerStream.W = (1, 4, 1);
            }

            public XRefStream(List<XRefEntry> entries, int offset, W.FileTrailerStream fileTrailerStream) : base(offset)
            {
                var w = new ByteWriter();
                foreach (var entry in entries)
                {
                    w.WriteByte((byte)entry.Type);
                    w.WriteInt(entry.Offset);
                    w.WriteByte((byte)entry.GenNum);
                }
                bytes = w.ToArray();
                var dict = fileTrailerStream; // new TYP.FileTrailerStream(new TYP.FileTrailer(new PdfDict(fileTrailerStream.FileTrailer.PdfDict)));
                dict.FileTrailer.Size = entries.Count + 1;
                dict.Index = new[] { (1, entries.Count) };
                dict.W = (1, 4, 1);
                dict.FileTrailer.Prev = null;
                dict.FileTrailer.Dict.Type = "XRef";
                PdfStream = new PdfStream(bytes, "FlateDecode", dict.FileTrailer.Dict);
            }

            public override XRef CreateAdditional(List<XRefInUseEntry> entries, int offset)
            {
                return new XRefStream(entries, offset, this);
            }

            public override void Write(PdfWriter w)
            {
                //var pdfStream = new PdfStream(bytes, trailerDict.PdfDict);
                //pdfStream.ObjID = new ObjID(trailerDict.Size - 1, 0);
                PdfStream.Write(w, null);
            }

            public override XRefEntry GetObjOffset(ObjID objID)
            {
                var cSoFar = 0;
                foreach (var section in streamSections)
                {
                    if (objID.ObjNum >= section.startObjNum && objID.ObjNum < (section.startObjNum + section.size))
                    {
                        var entry = streamEntries[cSoFar + (objID.ObjNum - section.startObjNum)];
                        return entry;
                    }
                    cSoFar += section.size;
                }
                if (prevXRef != null)
                    return prevXRef.GetObjOffset(objID);
                return null;
            }

        }

        protected class Section
        {
            public int StartObjNum, Size;
            public IList<(int offset, int genNum)> Entries;
            public Section(int startNum, int count, IList<(int offset, int genNum)> entries)
            {
                this.StartObjNum = startNum; this.Size = count; this.Entries = entries;
            }
        }
    }

    enum XRefEntryType { Free, InUse, Compressed }

    class XRefEntry
    {
        public readonly XRefEntryType Type;
        public readonly int Offset, GenNum;
        //for Free: Offset=objNum of next free object
        //for Compressed: Offset=objNum of objStream, GenNum=index in objStream

        public XRefEntry(XRefEntryType type, int offset, int genNum)
        {
            Type = type;
            Offset = offset;
            GenNum = genNum;
        }
    }

    class XRefInUseEntry
    {
        public readonly int Offset;
        public readonly ObjID ObjID;

        public XRefInUseEntry(int offset, ObjID objID) => (Offset, ObjID) = (offset, objID);
    }
}
