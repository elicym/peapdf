/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SeaPeaYou.PeaPdf
{
    class OTFFont
    {

        OTFFont() { }

        public void Write(Stream stream)
        {
            _write(stream, false);
        }

        public void AddIdentityCMap()
        {
            tableRecords.Entries.RemoveAll(x => x.Tag == "cmap");
            var cmap = new Table_cmap(Enumerable.Range(1, numGlyphs - 1).Select(x => (x, x)).ToList());
            tableRecords.Entries.Add(new TableRecord("cmap") { LinkedSection = cmap });
        }

        public static OTFFont FromTT(byte[] bytes)
        {
            var stream = new MemoryStream(bytes);

            var font = new OTFFont();
            var offsetSubtable = new OffsetSubtable(stream);
            font.tableRecords = new TableRecords(stream, offsetSubtable.NumTables);

            foreach (var tableDirEntry in font.tableRecords.Entries)
            {
                stream.Seek(tableDirEntry.LinkedSectionOffset, SeekOrigin.Begin);
                Section linkedSection;
                switch (tableDirEntry.Tag)
                {
                    case "cmap":
                        linkedSection = font.cmapTable = new Table_cmap(stream, tableDirEntry.LinkedSectionLength);
                        font.NoUnicodeCMap = !font.cmapTable.SubtableHeaders.Any(x => x.platform == 0 || (x.platform == 3 && (x.encoding == 1 || x.encoding == 10)));
                        break;
                    case "head":
                        font.headTableRecord = tableDirEntry;
                        linkedSection = font.headTable = new Table_head(stream, tableDirEntry.LinkedSectionLength);
                        break;
                    case "maxp":
                        {
                            var maxp = new Table_maxp(stream);
                            font.numGlyphs = maxp.NumGlyphs;
                            linkedSection = maxp;
                            break;
                        }
                    default:
                        linkedSection = new RawSection(stream, tableDirEntry.LinkedSectionLength);
                        break;
                }
                tableDirEntry.LinkedSection = linkedSection;
            }
            if (!font.tableRecords.Entries.Any(x => x.Tag == "name"))
            {
                var tableName = new Table_name();
                font.tableRecords.Entries.Add(new TableRecord { Tag = "name", LinkedSection = tableName });
            }
            if (!font.tableRecords.Entries.Any(x => x.Tag == "post"))
            {
                var tableName = new Table_post();
                font.tableRecords.Entries.Add(new TableRecord { Tag = "post", LinkedSection = tableName });
            }
            if (!font.tableRecords.Entries.Any(x => x.Tag == "OS/2"))
            {
                var tableName = new Table_OS2();
                font.tableRecords.Entries.Add(new TableRecord { Tag = "OS/2", LinkedSection = tableName });
            }

            return font;
        }

        public static OTFFont FromCFF(byte[] bytes, CharEncoding encoding, Dictionary<byte, string> code2Names)
        {
            var font = new OTFFont();
            font.LoadFromCFF(bytes, encoding, code2Names);
            return font;
        }

        void _write(Stream stream, bool forChecksum)
        {
            tableRecords.Entries = tableRecords.Entries.OrderBy(x => x.Tag, StringComparer.Ordinal).ToList();
            var offsetSubtable = new OffsetSubtable((short)tableRecords.Entries.Count, isCFF);

            var allSections = new List<Section> { offsetSubtable, tableRecords };
            allSections.AddRange(tableRecords.Entries.Where(x => x.LinkedSection != null).Select(x => x.LinkedSection));

            if (!forChecksum)
            {
                headTable.CheckSumAdjustment = 0;
                var tablesChecksum = new ChecksumStream();
                foreach (var t in tableRecords.Entries)
                {
                    t.LinkedSection.Write(tablesChecksum);
                    t.CalcChecksum();
                }
                headTableRecord.CheckSum = (int)unchecked(0xB1B0AFBA - (uint)tablesChecksum.Sum);

                var fontChecksum = new ChecksumStream();
                _write(fontChecksum, true);
                headTable.CheckSumAdjustment = fontChecksum.Sum;
            }

            int offset = 0;
            foreach (var section in allSections)
            {
                section.Offset = offset;
                offset += section.Length;
            }

            foreach (var section in allSections)
            {
                section.Write(stream);
            }
        }

        void LoadFromCFF(byte[] bytes, CharEncoding encoding, Dictionary<byte, string> code2Names)
        {
            isCFF = true;
            tableRecords = new TableRecords();
            var cff = new CFF(bytes);

            headTable = new Table_head
            {
                UnitsPerEm = 1000,
                XMin = -170,
                YMin = -228,
                XMax = 1003,
                YMax = 962,
                MacStyle = 1,
                LowestRevPPEM = 4,
            };
            headTableRecord = new TableRecord("head") { LinkedSection = headTable };
            tableRecords.Entries.Add(headTableRecord);

            var cmapTable = new Table_cmap(cff.Charset.SIDs, encoding, code2Names, cff.ExtraNames);
            tableRecords.Entries.Add(new TableRecord("cmap") { LinkedSection = cmapTable });

            var hheaTable = new Table_hhea();
            tableRecords.Entries.Add(new TableRecord("hhea") { LinkedSection = hheaTable });

            var section1 = new RawSection(bytes);
            tableRecords.Entries.Add(new TableRecord("CFF ") { LinkedSection = section1 });

            var nameTable = new Table_name();
            tableRecords.Entries.Add(new TableRecord("name") { LinkedSection = nameTable });

            var maxpTable = new Table_maxp((ushort)cff.NumGlyphs);
            tableRecords.Entries.Add(new TableRecord("maxp") { LinkedSection = maxpTable });

            var hmtxTable = new Table_hmtx();
            hmtxTable.NumGlyphs = cff.NumGlyphs;
            tableRecords.Entries.Add(new TableRecord("hmtx") { LinkedSection = hmtxTable });

            var os2Table = new Table_OS2();
            tableRecords.Entries.Add(new TableRecord("OS/2") { LinkedSection = os2Table });

        }

        public bool NoUnicodeCMap;
        TableRecords tableRecords;
        Table_head headTable;
        TableRecord headTableRecord;
        Table_cmap cmapTable;
        bool isCFF;
        int numGlyphs;

        static byte ReadByte(Stream stream) => (byte)stream.ReadByte();

        static short ReadShort(Stream stream) => (short)ReadUShort(stream);
        static ushort ReadUShort(Stream stream)
        {
            byte b1 = (byte)stream.ReadByte(), b2 = (byte)stream.ReadByte();
            return (ushort)((b1 << 8) + b2);
        }

        static int ReadInt(Stream stream) => (int)ReadUInt(stream);
        static uint ReadUInt(Stream stream)
        {
            byte b1 = ReadByte(stream), b2 = ReadByte(stream),
                b3 = ReadByte(stream), b4 = ReadByte(stream);
            return (uint)((b1 << 24) | (b2 << 16) | (b3 << 8) | b4);
        }

        static long ReadLong(Stream stream)
        {
            long v = 0;
            for (int i = 0; i < 8; i++)
            {
                v |= (long)ReadByte(stream) << ((7 - i) * 8);
            }
            return v;
        }

        static string ReadString(Stream stream, int len)
        {
            var buf = new byte[len];
            for (var i = 0; i < len; i++)
                buf[i] = (byte)stream.ReadByte();
            return Encoding.ASCII.GetString(buf);
        }

        static void WriteShort(Stream stream, short s) => WriteUShort(stream, (ushort)s);
        static void WriteUShort(Stream stream, ushort s)
        {
            stream.WriteByte((byte)(s >> 8));
            stream.WriteByte((byte)(s));
        }

        static void WriteInt(Stream stream, int i) => WriteUInt(stream, (uint)i);
        static void WriteUInt(Stream stream, uint i)
        {
            stream.WriteByte((byte)(i >> 24));
            stream.WriteByte((byte)(i >> 16));
            stream.WriteByte((byte)(i >> 8));
            stream.WriteByte((byte)(i));
        }

        static void WriteLong(Stream stream, long l)
        {
            for (int i = 0; i < 8; i++)
            {
                stream.WriteByte((byte)(l >> ((7 - i) * 8)));
            }
        }

        static void WriteString(Stream stream, string s)
        {
            var bytes = Encoding.ASCII.GetBytes(s);
            foreach (var b in bytes)
                stream.WriteByte(b);
        }

        static byte[] ReadByteArray(Stream stream, int count)
        {
            byte[] buffer = new byte[count];
            int left = count, offset = 0;
            while (left > 0)
            {
                int read = stream.Read(buffer, offset, left);
                if (read == 0)
                    throw new EndOfStreamException();
                offset += read;
                left -= read;
            }
            return buffer;
        }

        abstract class Section
        {
            public int Offset { get; set; }
            public abstract int Length { get; }
            public abstract void Write(Stream stream);
        }

        class OffsetSubtable : Section
        {
            public short NumTables;
            public bool IsCFF;

            public OffsetSubtable(Stream stream)
            {
                IsCFF = ReadInt(stream) == 0x4F54544F;
                NumTables = ReadShort(stream);
                ReadUShort(stream);
                ReadUShort(stream);
                ReadUShort(stream);
            }

            public OffsetSubtable(short numTables, bool isCFF)
            {
                NumTables = numTables;
                IsCFF = isCFF;
            }

            public override int Length => 12;

            public override void Write(Stream stream)
            {
                WriteInt(stream, IsCFF ? 0x4F54544F : 0x00010000);
                WriteShort(stream, NumTables);
                int maxP2 = 0;
                for (; ; maxP2++)
                {
                    if ((1 << maxP2) > NumTables)
                    {
                        maxP2--;
                        break;
                    }
                }
                int searchRange = maxP2 * 16;
                WriteUShort(stream, (ushort)searchRange);
                WriteUShort(stream, (ushort)Math.Log(maxP2, 2));
                WriteUShort(stream, (ushort)(NumTables * 16 - searchRange));
            }
        }

        class TableRecord : Section
        {
            public string Tag;
            public int CheckSum;
            public int LinkedSectionOffset;
            public int LinkedSectionLength;
            public Section LinkedSection;

            public TableRecord()
            {
            }

            public TableRecord(string tag)
            {
                Tag = tag;
            }

            public void Read(Stream stream)
            {
                Tag = ReadString(stream, 4);
                CheckSum = ReadInt(stream);
                LinkedSectionOffset = ReadInt(stream);
                LinkedSectionLength = ReadInt(stream);
            }

            public override int Length => 16;

            public override void Write(Stream stream)
            {
                WriteString(stream, Tag);
                WriteInt(stream, CheckSum);
                WriteInt(stream, LinkedSection.Offset);
                WriteInt(stream, LinkedSection.Length);
            }

            public override string ToString() => Tag;

            public void CalcChecksum()
            {
                var checksumStream = new ChecksumStream();
                LinkedSection.Write(checksumStream);
                CheckSum = checksumStream.Sum;
            }
        }

        class TableRecords : Section
        {
            public List<TableRecord> Entries = new List<TableRecord>();

            public TableRecords(Stream stream, int count)
            {
                for (int i = 0; i < count; i++)
                {
                    var entry = new TableRecord();
                    entry.Read(stream);
                    Entries.Add(entry);
                }
            }

            public TableRecords() { }

            public override int Length => Entries.Sum(x => x.Length);

            public override void Write(Stream stream)
            {
                foreach (var entry in Entries)
                    entry.Write(stream);
            }

        }

        class RawSection : Section
        {
            public byte[] Bytes;

            public RawSection(Stream stream, int len)
            {
                Bytes = ReadByteArray(stream, len);
            }

            public RawSection(byte[] bytes) => Bytes = bytes;

            public override int Length => Bytes.Length;

            public override void Write(Stream stream)
            {
                stream.Write(Bytes);
            }
        }

        class Table_hmtx : Section
        {
            public int NumGlyphs;

            public override int Length => NumGlyphs * 2 + 2;

            public override void Write(Stream stream)
            {
                WriteUShort(stream, 500);
                WriteShort(stream, 0);
                var left = NumGlyphs - 1;
                for (int i = 0; i < left; i++)
                {
                    WriteShort(stream, 0);
                }
            }
        }

        //when possible, encoding of font should match pdf encoding
        //otherwise, we need to convert in showText
        class Table_cmap : Section
        {
            public (short platform, short encoding, int offset)[] SubtableHeaders;

            IList<cmapGroup> groups;
            byte[] data;

            public override int Length
            {
                get
                {
                    if (SubtableHeaders != null)
                        return 4 + 8 * SubtableHeaders.Length + data.Length;
                    return 12 + (12 * groups.Count + 16);
                }
            }

            public Table_cmap(IList<int> sids, CharEncoding encoding, Dictionary<byte, string> code2Names, List<string> extraNames)
            {
                Dictionary<int, byte> nameDict = null;
                if (code2Names != null)
                {
                    var name2SIDs = new Dictionary<string, int>(CharEncoding.SIDByName);
                    for (var i = 0; i < extraNames.Count; i++)
                        name2SIDs.Add(extraNames[i], CFF.MaxStandardSID + 1 + i);
                    nameDict = code2Names?.Select(x =>
                    {
                        var sid = name2SIDs.TryGetValue(x.Value, out var s) ? s : (int?)null;
                        return new { sid, code = x.Key };
                    }).Where(x => x.sid != null).ToDictionary(x => x.sid.Value, x => x.code);
                }

                var codeGlyphs = new List<(int code, int glyph)>();
                for (int i = 0; i < sids.Count; i++)
                {
                    int? code;
                    if (nameDict != null && nameDict.TryGetValue(sids[i], out var _code))
                        code = _code;
                    else
                        code = encoding == null ? sids[i] : encoding.SID2Code(sids[i]);
                    if (code != null)
                        codeGlyphs.Add((code.Value, i + 1));
                }
                groupsFromCodeGlyphs(codeGlyphs);
            }

            public Table_cmap(IList<(int code, int glyph)> codeGlyphs)
            {
                groupsFromCodeGlyphs(codeGlyphs);
            }

            public Table_cmap(Stream s, int len)
            {
                ReadShort(s); //version
                int numTables = ReadShort(s); //numTables
                SubtableHeaders = new (short platform, short encoding, int offset)[numTables];
                for (int i = 0; i < numTables; i++)
                {
                    short platform = ReadShort(s), encoding = ReadShort(s);
                    int offset = ReadInt(s);
                    SubtableHeaders[i] = (platform, encoding, offset);
                }
                data = ReadByteArray(s, len - (4 + (numTables * 8)));
            }

            void groupsFromCodeGlyphs(IList<(int code, int glyph)> codeGlyphs)
            {
                groups = new List<cmapGroup>();
                (int code, int glyph)? runStartCharCodeGlyph = null, prevCharCodeGlyph = null;
                foreach (var codeGlyph in codeGlyphs)
                {
                    if (prevCharCodeGlyph == null)
                    {
                        runStartCharCodeGlyph = prevCharCodeGlyph = codeGlyph;
                    }
                    else
                    {
                        if (codeGlyph.code == prevCharCodeGlyph.Value.code + 1 && codeGlyph.glyph == prevCharCodeGlyph.Value.glyph + 1)
                        {
                            prevCharCodeGlyph = codeGlyph;
                        }
                        else
                        {
                            groups.Add(new cmapGroup(runStartCharCodeGlyph.Value.code, prevCharCodeGlyph.Value.code, runStartCharCodeGlyph.Value.glyph));
                            runStartCharCodeGlyph = prevCharCodeGlyph = codeGlyph;
                        }
                    }
                }
                if (runStartCharCodeGlyph != null)
                    groups.Add(new cmapGroup(runStartCharCodeGlyph.Value.code, prevCharCodeGlyph.Value.code, runStartCharCodeGlyph.Value.glyph));

            }

            public override void Write(Stream stream)
            {
                if (SubtableHeaders != null) //read
                {
                    WriteShort(stream, 0); //version
                    WriteShort(stream, (short)SubtableHeaders.Length); //numTables
                    for (int i = 0; i < SubtableHeaders.Length; i++)
                    {
                        var sh = SubtableHeaders[i];
                        WriteShort(stream, sh.platform);
                        WriteShort(stream, sh.encoding);
                        WriteInt(stream, sh.offset);
                    }
                    stream.Write(data);
                }
                else
                {
                    WriteShort(stream, 0); //version
                    WriteShort(stream, 1); //numTables
                                           //encoding record
                    WriteShort(stream, 3); //platformID (Windows)
                    WriteShort(stream, 10); //encodingID (Unicode)
                    WriteInt(stream, 12); //offset
                                          //subtable
                    WriteShort(stream, 12); //format
                    WriteShort(stream, 0); //reserved
                    WriteInt(stream, 12 * groups.Count + 16); //length of subtable
                    WriteInt(stream, 0); //language
                    WriteInt(stream, groups.Count); //numGroups
                    foreach (var grp in groups)
                    {
                        WriteInt(stream, grp.StartCharCode);
                        WriteInt(stream, grp.EndCharCode);
                        WriteInt(stream, grp.StartGlyphID);
                    }
                }
            }
        }

        class cmapGroup
        {
            public int StartCharCode, EndCharCode, StartGlyphID;

            public cmapGroup(int startCharCode, int endCharCode, int startGlyphID)
            {
                StartCharCode = startCharCode; EndCharCode = endCharCode; StartGlyphID = startGlyphID;
            }
        }

        class Table_head : Section
        {
            public ushort Flags, UnitsPerEm, MacStyle, LowestRevPPEM;
            public int FontRevision, CheckSumAdjustment;
            public long Created, Modified;
            public short XMin, YMin, XMax, YMax, IndexToLocFormat;

            public Table_head(Stream stream, int len)
            {
                ReadUShort(stream); //majorVersion - 1
                ReadUShort(stream); //minorVersion - 0
                FontRevision = ReadInt(stream);
                CheckSumAdjustment = ReadInt(stream);
                ReadUInt(stream); //magic number
                Flags = ReadUShort(stream);
                UnitsPerEm = ReadUShort(stream);
                Created = ReadLong(stream);
                Modified = ReadLong(stream);
                XMin = ReadShort(stream);
                YMin = ReadShort(stream);
                XMax = ReadShort(stream);
                YMax = ReadShort(stream);
                MacStyle = ReadUShort(stream);
                LowestRevPPEM = ReadUShort(stream);
                ReadShort(stream);
                IndexToLocFormat = ReadShort(stream);
                ReadShort(stream);
            }

            public Table_head() { }

            public override int Length => 54;

            public override void Write(Stream stream)
            {
                WriteUShort(stream, 1);
                WriteUShort(stream, 0);
                WriteInt(stream, FontRevision);
                WriteInt(stream, CheckSumAdjustment);
                WriteUInt(stream, 0x5F0F3CF5);
                WriteUShort(stream, Flags);
                WriteUShort(stream, UnitsPerEm);
                WriteLong(stream, Created);
                WriteLong(stream, Modified);
                WriteShort(stream, XMin);
                WriteShort(stream, YMin);
                WriteShort(stream, XMax);
                WriteShort(stream, YMax);
                WriteUShort(stream, MacStyle);
                WriteUShort(stream, LowestRevPPEM);
                WriteShort(stream, 2);
                WriteShort(stream, IndexToLocFormat);
                WriteShort(stream, 0);
            }
        }

        class Table_post : Section
        {
            public override int Length => 32;

            public override void Write(Stream stream)
            {
                WriteInt(stream, (int)Math.Round(2.5 * 65536));

                WriteInt(stream, 0);

                WriteShort(stream, 0);
                WriteShort(stream, 0);

                WriteShort(stream, 0);

                WriteInt(stream, 0);
                WriteInt(stream, 0);
                WriteInt(stream, 0);
                WriteInt(stream, 0);

                WriteShort(stream, 0);
            }
        }

        class Table_hhea : Section
        {
            public override int Length => 36;

            public override void Write(Stream stream)
            {
                WriteUShort(stream, 1);
                WriteUShort(stream, 0);
                WriteShort(stream, 500);
                WriteShort(stream, -100);
                WriteShort(stream, 0);
                WriteUShort(stream, 500);
                WriteShort(stream, 0);
                WriteShort(stream, 0);
                WriteShort(stream, 500);
                WriteShort(stream, 1);
                WriteShort(stream, 0);
                WriteShort(stream, 0);
                WriteShort(stream, 0);
                WriteShort(stream, 0);
                WriteShort(stream, 0);
                WriteShort(stream, 0);
                WriteShort(stream, 0);
                WriteUShort(stream, 1);
            }
        }

        class Table_maxp : Section
        {
            public uint Version;
            public ushort NumGlyphs;
            byte[] rest;

            public override int Length => 6 + (rest?.Length ?? 0);

            public Table_maxp(Stream s)
            {
                Version = ReadUInt(s);
                NumGlyphs = ReadUShort(s);
                if (Version == 0x10000)
                    rest = ReadByteArray(s, 2 * 13);
            }

            public Table_maxp(ushort numGlyphs)
            {
                Version = 0x00005000;
                NumGlyphs = numGlyphs;
            }

            public override void Write(Stream stream)
            {
                WriteUInt(stream, Version);
                WriteUShort(stream, NumGlyphs);
                if (rest != null)
                    stream.Write(rest);
            }
        }

        class Table_name : Section
        {
            public Table_name() { }

            public override int Length => 6;

            public override void Write(Stream s)
            {
                WriteUShort(s, 0);
                WriteUShort(s, 0);
                WriteUShort(s, 6);
            }
        }

        class Table_OS2 : Section
        {
            public override int Length => 96;

            public override void Write(Stream s)
            {
                WriteUShort(s, 4);
                WriteShort(s, 500);
                WriteShort(s, 400);
                WriteShort(s, 5);
                WriteShort(s, 1); //fsType
                WriteShort(s, 0);
                WriteShort(s, 0);
                WriteShort(s, 0);
                WriteShort(s, 0);
                WriteShort(s, 0);
                WriteShort(s, 0);
                WriteShort(s, 0);
                WriteShort(s, 0);
                WriteShort(s, 0);
                WriteShort(s, 0);
                WriteShort(s, 0); //sFamilyClass
                s.Write(new byte[10]);
                WriteUInt(s, uint.MaxValue);
                WriteUInt(s, uint.MaxValue);
                WriteUInt(s, uint.MaxValue);
                WriteUInt(s, uint.MaxValue);
                WriteInt(s, 0);
                s.Write(new byte[17 * 2]);
            }
        }

        class ChecksumStream : Stream
        {
            Queue<byte> byteQueue = new Queue<byte>();

            public int Sum { get; private set; }

            public override bool CanRead => false;

            public override bool CanSeek => false;

            public override bool CanWrite => true;

            public override long Length => throw new NotImplementedException();

            public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public override void Flush() { }

            public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();

            public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

            public override void SetLength(long value) => throw new NotImplementedException();

            public override void Write(byte[] buffer, int offset, int count)
            {
                for (int i = 0; i < count; i++)
                {
                    byteQueue.Enqueue(buffer[offset + i]);
                    if (byteQueue.Count == 4)
                    {
                        byte b1 = byteQueue.Dequeue(), b2 = byteQueue.Dequeue(),
                            b3 = byteQueue.Dequeue(), b4 = byteQueue.Dequeue();
                        unchecked
                        {
                            Sum += ((b1 << 24) + (b2 << 16) + (b3 << 8) + b4);
                        }
                    }
                }
            }
        }

        //enum CMapPlatform : short { Unicode = 0, Macintosh = 1, Windows = 3 }
        class CFF
        {
            public const int MaxStandardSID = 390;
            public int NumGlyphs;
            public CFFCharset Charset;
            public List<string> ExtraNames;

            public CFF(byte[] bytes)
            {
                var stream = new MemoryStream(bytes);
                var majorVersion = ReadByte(stream);
                var minorVersion = ReadByte(stream);
                var hdrSize = ReadByte(stream);
                var offSize = ReadByte(stream);
                stream.Position += hdrSize - 4;

                var nameIndex = new INDEX<Text>(stream);
                var topDictIndex = new INDEX<DICT>(stream);
                var stringIndex = new INDEX<Text>(stream);

                int charStringsOffset;
                if (topDictIndex.Objects[0].dict.TryGetValue(DICT.Operator.CharStrings, out var _charStringsOffset))
                {
                    charStringsOffset = (int)_charStringsOffset[0];
                    stream.Position = charStringsOffset;
                    var charStringIndex = new INDEX<Raw>(stream);
                    NumGlyphs = charStringIndex.Objects.Count;
                }

                int charsetOffset;
                if (topDictIndex.Objects[0].dict.TryGetValue(DICT.Operator.Charset, out var _charsetOffset))
                {
                    charsetOffset = (int)_charsetOffset[0];
                    stream.Position = charsetOffset;
                    Charset = new CFFCharset(stream, NumGlyphs);
                }
                else
                {
                    Charset = CFFCharset.ISOAdobe();
                }

                ExtraNames = stringIndex.Objects.Select(x => x.Str).ToList();
            }

            static int ReadNum(Stream stream, int size)
            {
                var v = 0;
                for (int i = 0; i < size; i++)
                {
                    v |= ReadByte(stream) << ((size - 1 - i) * 8);
                }
                return v;
            }

            class INDEX<T> where T : IReadStream, new()
            {
                int[] offsets;

                public List<T> Objects = new List<T>();

                public INDEX(Stream stream)
                {
                    var numObj = ReadUShort(stream);
                    if (numObj > 0)
                    {
                        var offSize = ReadByte(stream);
                        offsets = new int[numObj + 1];
                        for (int i = 0; i <= numObj; i++)
                        {
                            offsets[i] = ReadNum(stream, offSize);
                        }
                    }
                    for (int i = 0; i < numObj; i++)
                    {
                        var obj = new T();
                        obj.ReadStream(stream, offsets[i + 1] - offsets[i]);
                        Objects.Add(obj);
                    }
                }

            }

            interface IReadStream
            {
                void ReadStream(Stream stream, int len);
            }

            class Text : IReadStream
            {
                public string Str;
                public void ReadStream(Stream stream, int len)
                {
                    var bytes = ReadByteArray(stream, len);
                    Str = Encoding.ASCII.GetString(bytes);
                }
            }

            class Raw : IReadStream
            {
                byte[] bytes;
                public void ReadStream(Stream stream, int len)
                {
                    bytes = ReadByteArray(stream, len);
                }
            }

            class DICT : IReadStream
            {
                public enum Operator
                {
                    Version, Notice, FullName, FamilyName, Weight, FontBBox, UniqueID = 13, XUID, Charset,
                    Encoding, CharStrings, Private, Copyright = 192, IsFixedPitch, ItalicAngle, UnderlinePosition,
                    UnderlineThickness, PaintType, CharstringType, FontMatrix, StrokeWidth, SyntheticBase = 3092,
                    PostScript, BaseFontName, BaseFontBlend
                }

                public Dictionary<Operator, object[]> dict = new Dictionary<Operator, object[]>();

                public void ReadStream(Stream stream, int len)
                {
                    var operands = new List<object>();
                    int streamStart = (int)stream.Position, streamEnd = streamStart + len;
                    while (stream.Position < streamEnd)
                    {
                        var b = ReadByte(stream);
                        if (b <= 21)
                        {
                            int op = b;
                            if (op == 12)
                            {
                                op <<= 8;
                                op |= ReadByte(stream);
                            }
                            dict.Add((Operator)op, operands.ToArray());
                            operands.Clear();
                        }
                        else
                        {
                            if (b >= 32 && b <= 246)
                                operands.Add(b - 139);
                            else if (b >= 247 && b <= 250)
                            {
                                var b1 = ReadByte(stream);
                                operands.Add((b - 247) * 256 + b1 + 108);
                            }
                            else if (b >= 251 && b <= 254)
                            {
                                var b1 = ReadByte(stream);
                                operands.Add(-(b - 251) * 256 - b1 - 108);
                            }
                            else if (b == 28)
                            {
                                byte b1 = ReadByte(stream), b2 = ReadByte(stream);
                                operands.Add((b1 << 8) | b2);
                            }
                            else if (b == 29)
                            {
                                byte b1 = ReadByte(stream), b2 = ReadByte(stream), b3 = ReadByte(stream), b4 = ReadByte(stream);
                                operands.Add((b1 << 24) | (b2 << 16) | (b3 << 8) | (b4));
                            }
                            else if (b == 30)
                            {
                                byte bCur = default;
                                int pos = -1;
                                int getNibble()
                                {
                                    if (pos == -1)
                                    {
                                        bCur = ReadByte(stream);
                                        pos = 1;
                                    }
                                    var nibble = (bCur >> (4 * pos--)) & 0xf;
                                    return nibble;
                                }
                                var sb = new StringBuilder();
                                while (true)
                                {
                                    var nibble = getNibble();
                                    switch (nibble)
                                    {
                                        case 0xf: goto afterNibbles;
                                        case 0xa: sb.Append('.'); break;
                                        case 0xb: sb.Append('E'); break;
                                        case 0xc: sb.Append("E-"); break;
                                        case 0xe: sb.Append('-'); break;
                                        default: sb.Append(nibble.ToString()); break;
                                    }
                                }
                                afterNibbles:
                                operands.Add(decimal.Parse(sb.ToString(), System.Globalization.NumberStyles.Number | System.Globalization.NumberStyles.AllowExponent));
                            }
                        }
                    }

                }
            }

        }

        class CFFCharset
        {
            public List<int> SIDs = new List<int>();

            public CFFCharset(Stream s, int numGlyphs)
            {
                var format = ReadByte(s);
                switch (format)
                {
                    case 0:
                        for (int i = 1; i < numGlyphs; i++)
                        {
                            SIDs.Add(ReadUShort(s));
                        }
                        break;
                    case 1:
                        while (SIDs.Count < numGlyphs - 1)
                        {
                            var range = new Range1(s);
                            for (int i = 0; i <= range.NLeft; i++)
                            {
                                SIDs.Add(range.First + i);
                            }
                        }
                        break;
                    default: throw new NotImplementedException();

                }
            }

            public CFFCharset(List<int> sids) { SIDs = sids; }

            public static CFFCharset ISOAdobe() => new CFFCharset(Enumerable.Range(1, 228).ToList());

            class Range1
            {
                public ushort First;
                public byte NLeft;

                public Range1(Stream s)
                {
                    First = ReadUShort(s);
                    NLeft = ReadByte(s);
                }
            }
        }

    }
}
