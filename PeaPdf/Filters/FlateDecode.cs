/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace SeaPeaYou.PeaPdf.Filters
{
    class FlateDecode : LZWFlateDecode
    {

        public static byte[] Decode(PdfDict decodeParms, byte[] bytes) => new FlateDecode(decodeParms, bytes, CompressionMode.Decompress).result;

        public static byte[] Encode(PdfDict decodeParms, byte[] bytes) => new FlateDecode(decodeParms, bytes, CompressionMode.Compress).result;

        public FlateDecode(PdfDict decodeParms, byte[] bytes, CompressionMode compressionMode) : base(decodeParms)
        {
            if (compressionMode == CompressionMode.Compress)
            {
                bytes = EncodePredictor(bytes);
                MemoryStream sourceMS = new MemoryStream(bytes), destMS = new MemoryStream();
                destMS.WriteByte(104); destMS.WriteByte(222); //zlib wrapper
                using (var destStream = new DeflateStream(destMS, CompressionMode.Compress))
                {
                    sourceMS.Position = 0;
                    sourceMS.CopyTo(destStream);
                }
                result = destMS.ToArray();
            }
            else
            {
                MemoryStream sourceMS = new MemoryStream(bytes), destMS = new MemoryStream();
                sourceMS.Seek(2, SeekOrigin.Begin); //skip zlib wrapper
                using (var sourceStream = (Stream)new DeflateStream(sourceMS, CompressionMode.Decompress))
                {
                    sourceStream.CopyTo(destMS);
                }
                result = DecodePredictor(destMS.ToArray());
            }
        }

        byte[] result;
    }
}
