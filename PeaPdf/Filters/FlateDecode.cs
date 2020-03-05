/*
 * Copyright 2020 Elliott Cymerman
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

        public static byte[] Decode(PdfDict decodeParms, byte[] bytes) => new FlateDecode(decodeParms, bytes).result;

        public FlateDecode(PdfDict decodeParms, byte[] bytes) : base(decodeParms) {
            MemoryStream sourceMS = new MemoryStream(bytes), destMS = new MemoryStream();
            sourceMS.Seek(2, SeekOrigin.Begin); //skip zlib wrapper
            var sourceStream = (Stream)new DeflateStream(sourceMS, CompressionMode.Decompress);
            sourceStream.CopyTo(destMS);
            result = DoPredictor(destMS.ToArray());
        }

        byte[] result;
    }
}
