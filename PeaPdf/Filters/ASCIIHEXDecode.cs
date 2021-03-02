/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace SeaPeaYou.PeaPdf.Filters
{
    class ASCIIHEXDecode
    {

        public static byte[] Decode(byte[] bytes)
        {
            var res = new List<byte>();
            byte? prev = null;
            for (int i = 0; i < bytes.Length; i++)
            {
                var b = bytes[i];
                if (Utils.IsWhiteSpace(b))
                    continue;
                if (b == '>')
                    break;
                if (prev == null)
                {
                    prev = b;
                    continue;
                }
                res.Add((byte)(Utils.ReadHexDigit(prev.Value) * 16 + Utils.ReadHexDigit(b)));
            }
            return res.ToArray();
        }

        public static byte[] Encode(byte[] bytes)
        {
            var w = new ByteWriter();
            foreach (var b in bytes)
            {
                Utils.WriteHex(w, b);
            }
            return w.ToArray();
        }
    }
}
