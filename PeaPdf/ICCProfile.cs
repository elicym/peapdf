/*
 * Copyright 2020 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SeaPeaYou.PeaPdf
{
    partial class ICCProfile
    {

        public ICCProfile(byte[] bytes)
        {
            var r = new ByteReader(bytes);

            //header
            int size = r.ReadInt(), cmmType = r.ReadInt(), majorVer = r.ReadBytes(1), minorVer = r.ReadBytes(1); r.SkipBytes(2);
            ProfileClass deviceClass = (ProfileClass)r.ReadInt();
            ColorSpace colorSpace = (ColorSpace)r.ReadInt(), psc = (ColorSpace)r.ReadInt();
            r.SkipBytes(12); //datetime
            r.SkipBytes(4); //file signature
            Platform platform = (Platform)r.ReadInt();
            var bitReader = new BitReader(r);
            bool embedded = bitReader.ReadBit(), reliesOnColorData = bitReader.ReadBit();
            r.SkipBytes(3);
            r.SkipBytes(4); //manufacturer
            r.SkipBytes(4); //model
            bitReader = new BitReader(r);
            bool transparency = bitReader.ReadBit(), matte = bitReader.ReadBit(), polarityNegative = bitReader.ReadBit(), whiteMedia = bitReader.ReadBit(); r.SkipBytes(7);
            RenderingIntent renderingIntent = (RenderingIntent)r.ReadInt();
            XYZ pcsIlluminant = r.ReadXYZ();
            r.SkipBytes(4); //creator
            r.SkipBytes(16); //profile ID
            r.SkipBytes(28); //reserved

            //tag table
            var tagC = r.ReadInt();
            var tagEntries = new TagEntry[tagC];
            for (var i = 0; i < tagC; i++)
            {
                tagEntries[i] = new TagEntry { Signature = (TagSignature)r.ReadInt(), Offset = r.ReadInt(), Size = r.ReadInt() };
            };
            //tags
            var tags = new Tag[tagC];
            for (int i = 0; i < tagC; i++)
            {
                var tagEntry = tagEntries[i];
                var tagR = new ByteReader(bytes, tagEntry.Offset);
                var typeSignature = tagR.ReadInt();
                tagR.SkipBytes(4); //reserved
                var tagDataSize = tagEntry.Size - 8;
                var tagType = typeSignature switch
                {
                    0x6D667432 => new lutType(tagR, true, psc == ColorSpace.CIELAB ? fromPSCSignatures.Contains(tagEntry.Signature) : (bool?)null),
                    0x6D667431 => new lutType(tagR, false, psc == ColorSpace.CIELAB ? fromPSCSignatures.Contains(tagEntry.Signature) : (bool?)null),
                    0x6368726D => new chromaticityType(tagR),
                    0x636c726f => new colorantOrderType(tagR),
                    0x74657874 => new textType(tagR, tagDataSize),
                    0x6D6C7563 => new multiLocalizedUnicodeType(tagR),
                    0x64657363 => new profileDescriptionType(tagR),
                    0x58595A20 => new XYZType(tagR, tagDataSize),
                    0x63757276 => new curveType(tagR),
                    0x70617261 => new parametricCurveType(tagR),
                    0x6D414220 => new lutABToABType(tagR, false),
                    0x6D424120 => new lutABToABType(tagR, true),
                    _ => (TagType)null
                };

                tags[i] = new Tag { Signature = tagEntry.Signature, Type = tagType };
            }

            aToB0 = (ConversionType)tags.Single(x => x.Signature == TagSignature.AToB0).Type;
            bToA0 = (ConversionType)tags.SingleOrDefault(x => x.Signature == TagSignature.BToA0)?.Type;
        }

        public void ConvertToPCS(Span<float> input, Span<float> output)
        {
            Span<float> vals = stackalloc float[input.Length];
            input.CopyTo(vals);
            aToB0.Convert(vals, output);
        }

        public void ConvertFromPCS(Span<float> input, Span<float> output)
        {
            Span<float> vals = stackalloc float[input.Length];
            input.CopyTo(vals);
            bToA0.Convert(vals, output);
        }

        ConversionType aToB0, bToA0;

        const float ushortMax = 65535, ff00 = 65280;
        static readonly HashSet<TagSignature> fromPSCSignatures = new HashSet<TagSignature> { TagSignature.BToA0, TagSignature.BToA1, TagSignature.BToA2, TagSignature.BToD0, TagSignature.BToD1, TagSignature.BToD2, TagSignature.BToD3 };

        class TagEntry
        {
            public TagSignature Signature;
            public int Offset, Size;
        }

        class Tag
        {
            public TagSignature Signature;
            public TagType Type;
        }

        class ByteReader : PeaPdf.ByteReader
        {
            public ByteReader(byte[] bytes, int pos = 0) : base(bytes, pos) { }
            ByteReader(ByteReader from, int pos) : base(from, pos) { }

            public float Read_s15Fixed16()
            {
                var v = ReadInt();
                return ((v >> 16) + (v & 0xffff) / (float)0x10000);
            }
            public float Read_u16Fixed16()
            {
                var v = (uint)ReadInt();
                return ((v >> 16) + (v & 0xffff) / (float)0x10000);
            }

            public XYZ ReadXYZ() => new XYZ(Read_s15Fixed16(), Read_s15Fixed16(), Read_s15Fixed16());

            public ByteReader Clone(int pos) => new ByteReader(this, pos);

        }

        //class Reader
        //{
        //    byte[] bytes;
        //    byte b;
        //    int bitIX = -1;
        //    public int Pos = 0;

        //    public Reader(byte[] bytes, int pos = 0) => (this.bytes, this.Pos) = (bytes, pos);

        //    public bool ReadBit()
        //    {
        //        if (bitIX == -1)
        //        {
        //            b = bytes[Pos++];
        //            bitIX = 7;
        //        }
        //        return (b & (1 << bitIX--)) > 0;
        //    }

        //    public void EndByte() => bitIX = -1;

        //    public int ReadBits(int count)
        //    {
        //        int v = 0;
        //        for (int i = 0; i < count; i++)
        //        {
        //            if (ReadBit())
        //                v |= 1 << (count - 1 - i);
        //        }
        //        return v;
        //    }

        //    public byte ReadByte() => bytes[Pos++];

        //    public int ReadBytes(int count)
        //    {
        //        if (bitIX != -1)
        //            throw new InvalidOperationException("in byte");
        //        int v = 0;
        //        for (int i = 0; i < count; i++)
        //        {
        //            var b = bytes[Pos++];
        //            v |= b << ((count - i - 1) * 8);
        //        }
        //        return v;
        //    }

        //    public byte[] ReadByteArray(int count)
        //    {
        //        var b = new byte[count];
        //        Array.Copy(bytes, Pos, b, 0, count);
        //        Pos += count;
        //        return b;
        //    }

        //    public int ReadInt() => ReadBytes(4);
        //    public uint ReadUInt() => (uint)ReadBytes(4);

        //    public XYZ ReadXYZ() => new XYZ(Read_s15Fixed16(), Read_s15Fixed16(), Read_s15Fixed16());

        //    public void SkipBytes(int count) => Pos += count;

        //    public float Read_s15Fixed16()
        //    {
        //        var v = ReadInt();
        //        return ((v >> 16) + (v & 0xffff) / (float)0x10000);
        //    }
        //    public float Read_u16Fixed16()
        //    {
        //        var v = ReadUInt();
        //        return ((v >> 16) + (v & 0xffff) / (float)0x10000);
        //    }

        //    public Reader Clone(int pos) => new Reader(bytes, pos);

        //}


    }
}
