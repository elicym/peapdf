/*
 * Copyright 2020 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace SeaPeaYou.PeaPdf
{
    using Filters;

    class PdfStream : PdfObject
    {
        public PdfDict Dict;

        byte[] decodedBytes;
        byte[] encodedBytes;
        List<string> filterNames;
        FParse fParse;

        public PdfStream(PdfDict dict, FParse fParse, PdfIndirectReference iRef)
        {
            if (!fParse.ReadString("stream"))
                throw new FormatException("stream keyword not found");
            this.Dict = dict;
            this.fParse = fParse;
            fParse.ReadEOL();

            filterNames = Dict["Filter"]?.AsArray<PdfName>().Select(x => x.ToString()).ToList() ?? new List<string>();

            int length = (int)dict["Length"];
            encodedBytes = fParse.Decrypt(fParse.ReadByteArray(length), iRef);

            fParse.ReadEOL();
            if (!fParse.ReadString("endstream"))
                throw new FormatException("endstream keyword not found");
        }

        void Decode()
        {
            if (decodedBytes != null)
                return;

            var bytes = encodedBytes;

            var decodeParmsArr = Dict["DecodeParms"]?.AsArray<PdfDict>();
            for (int i = 0; i < filterNames.Count; i++)
            {
                var filterName = filterNames[i];
                var decodeParms = decodeParmsArr?[i];
                switch (filterName)
                {
                    case "ASCIIHexDecode": bytes = ASCIIHEXDecode.Decode(bytes); break;
                    case "ASCII85Decode": bytes = ASCII85Decode.Decode(bytes); break;
                    case "LZWDecode": bytes = LZWDecode.Decode(decodeParms, bytes); break;
                    case "FlateDecode": bytes = FlateDecode.Decode(decodeParms, bytes); break;
                    case "DCTDecode": bytes = DCTDecode.Decode(decodeParms, bytes); break;
                    case "CCITTFaxDecode": bytes = CCITTFaxDecode.Decode(decodeParms, bytes); break;
                    case "JPXDecode":
                    case "Crypt":
                    case "RunLengthDecode":
                    case "JBIG2Decode":
                        throw new NotImplementedException(filterName);
                    default: throw new NotSupportedException(filterName);
                }
            }
            decodedBytes = bytes;

        }

        public byte[] GetRawBytes() => encodedBytes.ToArray();

        public byte[] GetBytes()
        {
            Decode();
            return decodedBytes;
        }

        public override void Write(Stream stream, PDF pdf, PdfIndirectReference iRef)
        {
            Dict.Write(stream, pdf, iRef);
            stream.WriteString("stream");
            stream.WriteByte((byte)'\n');
            var bytes = pdf.Encrypt(encodedBytes.ToArray(), iRef);
            stream.Write(bytes);
            stream.WriteByte((byte)'\n');
            stream.WriteString("endstream");
            stream.WriteByte((byte)'\n');
        }

    }

}
