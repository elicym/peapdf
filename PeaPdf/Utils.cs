/*
 * Copyright 2020 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using SkiaSharp;
using System.Linq;

namespace SeaPeaYou.PeaPdf
{
    static class Utils
    {

        public static bool IsWhiteSpace(byte b) => b == 0 || b == 9 || b == 10 || b == 12 || b == 13 || b == 32;
        public static bool IsDelimiter(byte b) => IsWhiteSpace(b) || b == '(' || b == ')' || b == '<' || b == '>' || b == '[' || b == ']' || b == '{' || b == '}' || b == '/' || b == '%';
        public static bool IsDigit(byte b) => b >= '0' && b <= '9';
        public static bool IsEOL(byte b) => b == '\r' || b == '\n';

        public static void WriteString(this Stream stream, string str)
        {
            var bytes = Encoding.ASCII.GetBytes(str);
            stream.Write(bytes);
        }

        public static void WriteHex(this Stream stream, byte b)
        {
            var quotient = Math.DivRem(b, 16, out int rem);
            stream.WriteByte(GetHexDigit(quotient));
            stream.WriteByte(GetHexDigit(rem));
        }

        static byte GetHexDigit(int num) => (byte)(num + (num < 10 ? '0' : 55));
        public static int ReadHexDigit(byte b)
        {
            if (b <= '9')
                return b - '0';
            if (b <= 'F')
                return b - 'A' + 10;
            return b - 'a' + 10;
        }

        public static SKMatrix MatrixConcat(params SKMatrix[] matrices)
        {
            SKMatrix matrix = matrices[0];
            foreach (var m in matrices.Skip(1))
            {
                SKMatrix.PreConcat(ref matrix, m);
            }
            return matrix;
        }

        public static void Write(this Stream stream, byte[] bytes) => stream.Write(bytes, 0, bytes.Length);

        public static V GetValueOrDefault<K,V>(this Dictionary<K,V> dict, K key)
        {
            return dict.TryGetValue(key, out V val) ? val : default;
        }
    }

}
