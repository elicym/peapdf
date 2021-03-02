/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace SeaPeaYou.PeaPdf
{
    interface IByteReader
    {
        int Pos { get; set; }
        byte ReadByte();
        int ReadShort();
        int ReadInt();
        void SkipBytes(int count);
    }

    class ByteReader : IByteReader
    {
        public int Pos { get; set; }
        protected byte[] Bytes;

        public ByteReader(byte[] bytes, int pos = 0) => (this.Bytes, this.Pos) = (bytes, pos);

        protected ByteReader(ByteReader from, int? pos)
        {
            Bytes = from.Bytes;
            Pos = pos ?? from.Pos;
        }

        public byte ReadByte() => Bytes[Pos++];
        public byte PeekByte => Bytes[Pos];
        public byte PeekByteAtOffset(int offset) => Bytes[Pos + offset];

        public int ReadShort() => (ReadByte() << 8) | ReadByte();

        public int ReadInt() => (ReadByte() << 24) | (ReadByte() << 16) | (ReadByte() << 8) | ReadByte();

        public (int, int) Read2Halfs()
        {
            var b = ReadByte();
            return (b >> 4, b & 0b1111);
        }

        public int ReadBytes(int count)
        {
            int v = 0;
            for (int i = 0; i < count; i++)
            {
                var b = ReadByte();
                v |= b << ((count - i - 1) * 8);
            }
            return v;
        }

        public byte[] ReadByteArray(int count)
        {
            var res = new byte[count];
            for (int i = 0; i < count; i++)
            {
                res[i] = ReadByte();
            }
            return res;
        }

        public void SkipBytes(int count) => Pos += count;

        public bool AtEnd => Pos == Bytes.Length;
    }

    class ByteReaderLE : IByteReader
    {
        public int Pos { get; set; }
        protected byte[] Bytes;

        public ByteReaderLE(byte[] bytes, int pos = 0) => (this.Bytes, this.Pos) = (bytes, pos);

        public byte ReadByte() => Bytes[Pos++];

        public int ReadShort() => ReadByte() | (ReadByte() << 8);

        public int ReadInt() => ReadByte() | (ReadByte() << 8) | (ReadByte() << 16) | (ReadByte() << 24);

        public void SkipBytes(int count) => Pos += count;

    }

    class ByteWriter
    {
        List<byte> bytes = new List<byte>();

        public int Count => bytes.Count;

        public void WriteByte(byte b) => bytes.Add(b);
        public void WriteByte(char c) => bytes.Add((byte)c);
        public void WriteBytes(IEnumerable<byte> b) => bytes.AddRange(b);
        public void WriteShort(int n)
        {
            bytes.Add((byte)(n >> 8));
            bytes.Add((byte)(n >> 0));
        }
        public void WriteInt(int n)
        {
            bytes.Add((byte)(n >> 24));
            bytes.Add((byte)(n >> 16));
            bytes.Add((byte)(n >> 8));
            bytes.Add((byte)(n >> 0));
        }

        public byte[] ToArray() => bytes.ToArray();
    }
}
