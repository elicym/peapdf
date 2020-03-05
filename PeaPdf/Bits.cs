/*
 * Copyright 2020 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SeaPeaYou.PeaPdf
{

    class BitReader
    {
        ByteReader byteReader;
        byte b;
        int bitIX = -1; //index of next read

        public BitReader(ByteReader byteReader) => this.byteReader = byteReader;

        protected virtual byte GetNextByte() => byteReader.ReadByte();

        public bool ReadBit()
        {
            if (bitIX == -1)
            {
                b = GetNextByte();
                bitIX = 7;
            }
            return (b & (1 << bitIX--)) > 0;
        }

        public int ReadBits(int count)
        {
            int v = 0;
            for (int i = 0; i < count; i++)
            {
                if (ReadBit())
                    v |= 1 << (count - 1 - i);
            }
            return v;
        }

    }

    //must be disposed to flush
    class BitWriter : IDisposable
    {
        ByteWriter byteWriter;
        public BitWriter(ByteWriter byteWriter) => this.byteWriter = byteWriter;

        byte b;
        int bitIX = 7; //index of next write
        bool disposed;

        public void AddBit(bool bit)
        {
            if (disposed) throw new ObjectDisposedException(nameof(BitWriter));
            if (bit)
                b |= (byte)(1 << bitIX);
            bitIX--;
            if (bitIX == -1)
            {
                byteWriter.WriteByte(b);
                b = 0;
                bitIX = 7;
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            if (bitIX < 7)
                byteWriter.WriteByte(b);
            disposed = true;
        }
    }

}
