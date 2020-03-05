/*
 * Copyright 2020 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace SeaPeaYou.PeaPdf
{
    class ByteReader
    {
        protected byte[] Bytes;
        public int Pos = 0;

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

    class ByteWriter
    {
        List<byte> bytes = new List<byte>();

        public void WriteByte(byte b) => bytes.Add(b);

        public byte[] ToArray() => bytes.ToArray();
    }
}
