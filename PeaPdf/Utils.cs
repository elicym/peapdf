/*
 * Copyright 2021 Elliott Cymerman
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
    enum ColorSpace { DeviceRGB, DeviceGray, DeviceCMYK, Blank }

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

        public static void WriteHex(this ByteWriter w, byte b)
        {
            var quotient = Math.DivRem(b, 16, out int rem);
            w.WriteByte(GetHexDigit(quotient));
            w.WriteByte(GetHexDigit(rem));
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
                matrix= matrix.PreConcat(m);
            }
            return matrix;
        }

        public static SKMatrix MatrixFromArray(float[] nums)
        {
            var matrix = SKMatrix.CreateIdentity();
            matrix.ScaleX = nums[0];
            matrix.SkewY = nums[1];
            matrix.SkewX = nums[2];
            matrix.ScaleY = nums[3];
            matrix.TransX = nums[4];
            matrix.TransY = nums[5];
            return matrix;
        }

        public static SKMatrix MatrixFromArray(PdfArray arr) => MatrixFromArray(arr.Select(x => (float)x).ToArray());

        public static void Write(this Stream stream, byte[] bytes) => stream.Write(bytes, 0, bytes.Length);

        public static V GetValueOrDefault<K, V>(this Dictionary<K, V> dict, K key)
        {
            return dict.TryGetValue(key, out V val) ? val : default;
        }

        public static int DivideCeil(this int dividend, int divisor)
        {
            var quotient = Math.DivRem(dividend, divisor, out var remainder);
            if (remainder > 0 && quotient >= 0)
                quotient++;
            return quotient;
        }

        //Split a IEnumerable into chunks.
        public static IEnumerable<IList<T>> Chunk<T>(this IEnumerable<T> source, int chunkSize)
        {
            var list = new List<T>(chunkSize);
            foreach (var element in source)
            {
                list.Add(element);
                if (list.Count == chunkSize)
                {
                    yield return list;
                    list = new List<T>(chunkSize);
                }
            }
            if (list.Count > 0)
                yield return list;
        }

        public static T To<F, T>(this F obj, Func<F, T> func) where F : PdfObject => func(obj);
        public static T To<F, T>(this F obj, Func<F, T> func, Func<T> funcIfNull) where F : PdfObject => obj != null ? func(obj) : funcIfNull();
        public static void IfNotNull<T>(this T obj, Action<T> action) where T:PdfObject
        {
            if (obj != null)
                action(obj);
        }
    }

}
