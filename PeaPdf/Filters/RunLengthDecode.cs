using System;
using System.Collections.Generic;
using System.Text;

namespace SeaPeaYou.PeaPdf.Filters
{
    class RunLengthDecode
    {

        public static byte[] Decode(byte[] bytes) => new RunLengthDecode(bytes).result;

        RunLengthDecode(byte[] bytes)
        {
            var output = new List<byte>(bytes.Length * 2);
            var br = new ByteReader(bytes);
            var length = br.ReadByte();
            if (length < 128)
            {
                for (int i = 0; i <= length; i++)
                {
                    output.Add(br.ReadByte());
                }
            }
            else
            {
                var b = br.ReadByte();
                var copied = 257 - length;
                for (int i = 0; i < copied; i++)
                {
                    output.Add(b);
                }
            }
            result = output.ToArray();
        }

        byte[] result;

    }
}
