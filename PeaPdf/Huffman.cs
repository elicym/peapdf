/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace SeaPeaYou.PeaPdf
{

    class HuffmanBitCode : IEquatable<HuffmanBitCode>
    {

        public HuffmanBitCode() { bits = new List<bool>(10); }
        public HuffmanBitCode(int code, int size)
        {
            bits = new List<bool>(size);
            for (int i = 0; i < size; i++)
            {
                bits.Add((code & (1 << (size - 1 - i))) > 0);
            }
        }
        public HuffmanBitCode(string str)
        {
            bits = new List<bool>(str.Length);
            for (int i = 0; i < str.Length; i++)
            {
                switch (str[i])
                {
                    case '0': bits.Add(false); break;
                    case '1': bits.Add(true); break;
                    default: throw new Exception("bad bit " + str[i]);
                }
            }
        }

        public bool Equals(HuffmanBitCode other)
        {
            if (other == null || other.bits.Count != bits.Count)
                return false;
            for (int i = 0; i < bits.Count; i++)
            {
                if (other.bits[i] != bits[i])
                    return false;
            }
            return true;
        }

        public override bool Equals(object obj)
        {
            if (obj is HuffmanBitCode other)
                return Equals(other);
            return false;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            for (int i = 0; i < bits.Count; i++)
            {
                unchecked
                {
                    hash *= 31;
                    if (bits[i])
                        hash += 1;
                }
            }
            return hash;
        }

        public void AddBit(bool bit) => bits.Add(bit);
        public void Clear() => bits.Clear();

        public override string ToString()
        {
            var sb = new StringBuilder();
            for (var i = 0; i < bits.Count; i++)
                sb.Append(bits[i] ? '1' : '0');
            return sb.ToString();
        }

        public static implicit operator HuffmanBitCode(string str) => new HuffmanBitCode(str);

        readonly List<bool> bits;

    }

    class HuffmanTable<T>
    {

        public HuffmanTable(Dictionary<HuffmanBitCode, T> dict) => this.dict = dict;

        public HuffmanTable(IList<HuffmanBitCode> bitCodes, IList<T> vals)
        {
            dict = new Dictionary<HuffmanBitCode, T>();
            if (bitCodes.Count != vals.Count) throw new Exception("bitCodes & vals don't match");
            for (int i = 0; i < bitCodes.Count; i++)
            {
                dict.Add(bitCodes[i], vals[i]);
            }
        }

        public T GetVal(BitReader bitReader)
        {
            bitCode.Clear();
            for (int i = 0; i < 30; i++)
            {
                bitCode.AddBit(bitReader.ReadBit());
                if (dict.TryGetValue(bitCode, out var b))
                    return b;
            }
            throw new Exception("code not found");
        }

        readonly Dictionary<HuffmanBitCode, T> dict = new Dictionary<HuffmanBitCode, T>();
        readonly HuffmanBitCode bitCode = new HuffmanBitCode();

    }
}
