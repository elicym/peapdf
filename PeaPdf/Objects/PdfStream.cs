/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Linq;

namespace SeaPeaYou.PeaPdf
{
    using Filters;

    class PdfStream : PdfObject
    {
        public readonly PdfDict Dict;
        public readonly string[] FilterNames; //empty array if no filters

        readonly PdfReader r;
        readonly PdfDict[] decodeParmsArr; //null if no decode parms

        byte[] decodedBytes; byte[] encodedBytes; //one of these may be null, in which case it can be derived from the other


        internal PdfStream(PdfDict dict, PdfReader r, ObjID? baseObjID)
        {
            if (!r.ReadString("stream"))
                throw new FormatException("stream keyword not found");
            this.Dict = dict;
            this.r = r;
            r.ReadEOL();

            int length = (int)dict["Length"];
            encodedBytes = r.Decrypt(r.ReadByteArray(length), baseObjID);

            FilterNames = Dict["Filter"]?.AsArray<PdfName>().Select(x => x.ToString()).ToArray() ?? new string[0];
            decodeParmsArr = Dict["DecodeParms"]?.AsArray<PdfDict>();

            r.ReadEOL();
            if (!r.ReadString("endstream"))
                throw new FormatException("endstream keyword not found");
        }

        public PdfStream(byte[] bytes, string filterName, PdfDict extraEntries = null, PdfDict decodeParms = null, bool bytesAreEncoded = false) 
            : this(filterName)
        {
            if (extraEntries != null)
            {
                foreach (var (key, value) in extraEntries)
                {
                    Dict.Add(key, value);
                }
            }
            decodeParmsArr = decodeParms == null ? null : new PdfDict[] { decodeParms };
            if (bytesAreEncoded)
                encodedBytes = bytes;
            else
                decodedBytes = bytes;
        }

        public PdfStream(string filterName = nameof(FlateDecode), PdfDict decodeParms=null)
        {
            Dict = new PdfDict();
            FilterNames = filterName == null ? new string[0] : new[] { filterName };
            if (decodeParms != null)
                decodeParmsArr = new[] { decodeParms };
        }

        PdfStream(PdfStream cloneFrom)
        {
            Dict = cloneFrom.Dict.CloneThis();
            decodedBytes = cloneFrom.decodedBytes;
            encodedBytes = cloneFrom.encodedBytes;
            r = cloneFrom.r;
            FilterNames = cloneFrom.FilterNames;
        }

        public byte[] GetEncodedBytes()
        {
            if (encodedBytes == null)
            {
                if (FilterNames.Length == 0)
                {
                    encodedBytes = decodedBytes;
                }
                else if (FilterNames.Length == 1)
                {
                    switch (FilterNames[0])
                    {
                        case "FlateDecode":
                            encodedBytes = FlateDecode.Encode(decodeParmsArr?[0], decodedBytes);
                            break;
                        case "ASCIIHexDecode":
                            encodedBytes = ASCIIHEXDecode.Encode(decodedBytes);
                            break;
                        default: throw new NotSupportedException();
                    }
                }
                else throw new Exception("Cannot encode using filters: " + string.Join(",", FilterNames));
            }
            return encodedBytes;
        }

        public byte[] GetDecodedBytes()
        {
            if (decodedBytes == null)
            {
                var bytes = encodedBytes;

                for (int i = 0; i < FilterNames.Length; i++)
                {
                    var filterName = FilterNames[i];
                    var decodeParms = decodeParmsArr?[i];
                    switch (filterName)
                    {
                        case "ASCIIHexDecode": bytes = ASCIIHEXDecode.Decode(bytes); break;
                        case "ASCII85Decode": bytes = ASCII85Decode.Decode(bytes); break;
                        case "LZWDecode": bytes = LZWDecode.Decode(decodeParms, bytes); break;
                        case "FlateDecode": bytes = FlateDecode.Decode(decodeParms, bytes); break;
                        //case "DCTDecode": bytes = DCTDecode.Decode(decodeParms, bytes); break;
                        case "CCITTFaxDecode": bytes = CCITTFaxDecode.Decode(decodeParms, bytes); break;
                        case "RunLengthDecode": bytes = RunLengthDecode.Decode(bytes); break;
                        case "JPXDecode":
                        case "Crypt":
                        case "JBIG2Decode":
                        case "DCTDecode":
                            throw new NotImplementedException(filterName);
                        default: throw new NotSupportedException(filterName);
                    }
                }
                decodedBytes = bytes;
            }
            return decodedBytes;
        }

        public (byte[] bytes, string filterName) GetDecodedBytesForImage()
        {
            string filterName = null;
            if (decodedBytes == null)
            {
                var bytes = encodedBytes;

                for (int i = 0; i < FilterNames.Length; i++)
                {
                    var _filterName = FilterNames[i];
                    if (filterName != null) throw new NotSupportedException($"{_filterName} after {filterName}");
                    var decodeParms = decodeParmsArr?[i];
                    switch (_filterName)
                    {
                        case "ASCIIHexDecode": bytes = ASCIIHEXDecode.Decode(bytes); break;
                        case "ASCII85Decode": bytes = ASCII85Decode.Decode(bytes); break;
                        case "LZWDecode": bytes = LZWDecode.Decode(decodeParms, bytes); break;
                        case "FlateDecode": bytes = FlateDecode.Decode(decodeParms, bytes); break;
                        //case "DCTDecode": bytes = DCTDecode.Decode(decodeParms, bytes); break;
                        case "CCITTFaxDecode": bytes = CCITTFaxDecode.Decode(decodeParms, bytes); break;
                        case "RunLengthDecode": bytes = RunLengthDecode.Decode(bytes); break;
                        case "JPXDecode":
                        case "Crypt":
                        case "JBIG2Decode":
                        case "DCTDecode":
                            filterName = _filterName;
                            break;
                        default: throw new NotSupportedException(filterName);
                    }
                }
                decodedBytes = bytes;
            }
            return (decodedBytes, filterName);
        }

        public void SetEncodedBytes(byte[] bytes)
        {
            encodedBytes = bytes;
            decodedBytes = null;
        }

        public void SetDecodedBytes(byte[] bytes)
        {
            decodedBytes = bytes;
            encodedBytes = null;
        }

        internal override void Write(PdfWriter w, ObjID? encryptionObjID)
        {

            var bytes = w.Encrypt(GetEncodedBytes(), encryptionObjID);
            Dict["Length"] = (PdfNumeric)bytes.Length;
            Dict["Filter"] = ArraySingleNull(FilterNames.Select(x => (PdfName)x).ToArray());
            Dict["DecodeParms"] = ArraySingleNull(decodeParmsArr);

            Dict.Write(w, encryptionObjID);
            w.WriteString("stream");
            w.WriteNewLine();
            w.WriteBytes(bytes);
            w.WriteNewLine();
            w.WriteString("endstream");
            //this will always be followed by '\nendobj'
        }

        public override PdfObject Clone() => new PdfStream(this);

    }

}
