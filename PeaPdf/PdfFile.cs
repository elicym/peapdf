/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SeaPeaYou.PeaPdf
{

    //This file is to keep the original bytes that PDF was opened from, for dereferencing indirect references. Also contains the original Version.
    class PdfFile
    {
        public readonly decimal Version;
        public readonly byte[] Bytes;
        public readonly W.FileTrailer FileTrailer;
        public readonly bool UsedUserPassword, UsedOwnerPassword;
        public readonly UserAccessPermissions Permissions;

        readonly Dictionary<ObjID, PdfObject> objDict = new Dictionary<ObjID, PdfObject>(); //TODO only save root xRef
        readonly Dictionary<int, ObjectStream> objectStreamDict = new Dictionary<int, ObjectStream>();
        readonly byte[] encryptionKey;
        readonly XRef xRef;
        readonly bool useRC4;

        public PdfFile(byte[] bytes, string password = null)
        {
            Bytes = bytes;
            //read header
            var r = new PdfReader(this);
            if (!r.ReadString("%PDF-"))
                throw new FormatException();
            Version = new PdfNumeric(r).Value;
            //read footer
            r.Pos = bytes.Length - 5;
            int loopUntil = bytes.Length - 1024;
            for (; r.Pos > loopUntil; r.Pos--)
            {
                if (r.PeekString("%%EOF"))
                    break;
            }
            if (r.Pos == loopUntil) throw new FormatException("no EOF");
            loopUntil = r.Pos - 30;
            for (; r.Pos > loopUntil; r.Pos--)
            {
                if (r.PeekString("startxref"))
                    break;
            }
            if (r.Pos == loopUntil) throw new FormatException("startxref");
            r.ReadStringUntilDelimiter();
            r.SkipWhiteSpace();
            var startXRef = int.Parse(r.ReadStringUntilDelimiter());
            //read xRef
            r.Pos = startXRef;
            xRef = XRef.Read(r);
            var encryptDict = xRef.FileTrailer.Encrypt;
            if (xRef.FileTrailer.Encrypt != null)
            {
                if (encryptDict["Filter"].As<PdfName>().String != "Standard")
                    throw new NotImplementedException("filter");
                PdfString O = (PdfString)encryptDict["O"], U = (PdfString)encryptDict["U"];
                int P = (int)encryptDict["P"];
                Permissions = (UserAccessPermissions)P;
                var fileID = xRef.FileTrailer.ID.Value.Item1;
                var length = (int?)encryptDict["Length"];
                var paddedPwd = Encryption.PadBytes(password ?? "");
                var encKey = Encryption.ComputeEncryptionKey(paddedPwd, O.Value, P, fileID, length);
                if (Encryption.ComputeU_Unpadded(encKey, fileID).SequenceEqual(U.Value.Take(16)))
                {
                    UsedUserPassword = true;
                    encryptionKey = encKey;
                }
                else
                {
                    var userPwd = Encryption.GetUserPwd(paddedPwd, O.Value, length);
                    encKey = Encryption.ComputeEncryptionKey(userPwd, O.Value, P, fileID, length);
                    if (Encryption.ComputeU_Unpadded(encKey, fileID).SequenceEqual(U.Value.Take(16)))
                    {
                        UsedOwnerPassword = true;
                        encryptionKey = encKey;
                    }
                    else
                    {
                        throw new Exception("Password is incorrect.");
                    }
                }
                if (xRef.FileTrailer.Encrypt["StmF"]?.As<PdfName>().String != "StdCF"
                        || xRef.FileTrailer.Encrypt["StrF"]?.As<PdfName>().String != "StdCF")
                    throw new NotImplementedException("Identity crypt filter.");
                var cfm = xRef.FileTrailer.Encrypt["CF"].As<PdfDict>()["StdCF"].As<PdfDict>()["CFM"].As<PdfName>().String;
                useRC4 = cfm == "V2" ? true : (cfm == "AESV2" ? false : throw new Exception("Bad CFM."));
            }
            FileTrailer = xRef.FileTrailer;
        }

        internal PdfObject Deref(PdfObject obj)
        {
            if (obj == null)
                return null;
            if (obj is PdfIndirectReference iRef)
                return GetObj(iRef.ToObjID);
            return obj;
        }

        internal byte[] Decrypt(byte[] bytes, ObjID? objID)
        {
            if (encryptionKey == null || objID == null)
                return bytes;
            var key = encryptionKey.ToList();
            key.AddRange(BitConverter.GetBytes(objID.Value.ObjNum).Take(3));
            key.AddRange(BitConverter.GetBytes(objID.Value.GenNum).Take(2));
            if(!useRC4)
            {
                key.AddRange(new byte[] { 0x73, 0x41, 0x6C, 0x54 });
            }
            var md5 = MD5.Create();
            var finalKey = md5.ComputeHash(key.ToArray()).Take(encryptionKey.Length + 5).ToArray();
            if (useRC4)
            {
                return RC4.Decrypt(finalKey.ToArray(), bytes);
            }
            else
            {
                using (var rijndael = new RijndaelManaged { Key = finalKey, IV = bytes.Take(16).ToArray(), Mode = CipherMode.CBC, BlockSize = 128 })
                using (var msS = new MemoryStream())
                {
                    msS.Write(bytes, 16, bytes.Length - 16);
                    msS.Position = 0;
                    using (var msD = new MemoryStream())
                    {
                        using (var cryptoStream = new CryptoStream(msS, rijndael.CreateDecryptor(), CryptoStreamMode.Read))
                        {
                            cryptoStream.CopyTo(msD);
                        }
                        return msD.ToArray();
                    }
                }
            }
        }

        internal PdfObject GetObj(ObjID objID)
        {
            if (!objDict.TryGetValue(objID, out var res))
            {
                var entry = xRef.GetObjOffset(objID);
                if (entry != null)
                {
                    switch (entry.Type)
                    {
                        case XRefEntryType.Free: break;
                        case XRefEntryType.InUse:
                            {
                                if (entry.GenNum != objID.GenNum)
                                    break;
                                var r = new PdfReader(this, entry.Offset);
                                r.SkipWhiteSpace();
                                r.ReadObjHeader(objID);
                                res = r.ReadPdfObject(objID);
                                res.ObjID = objID;
                                break;
                            }
                        case XRefEntryType.Compressed:
                            {
                                var objStm = GetObjectStream(entry.Offset);
                                res = objStm.GetObj(entry.GenNum);
                                res.ObjID = objID;
                                break;
                            }
                    }
                }

                objDict.Add(objID, res);
            }
            return res;
        }

        ObjectStream GetObjectStream(int objNum)
        {
            if (!objectStreamDict.TryGetValue(objNum, out var objStm))
            {
                var s = (PdfStream)GetObj(new ObjID(objNum, 0));
                objStm = new ObjectStream(this, s);
                objectStreamDict.Add(objNum, objStm);
            }
            return objStm;
        }

        class ObjectStream
        {
            PdfFile pdfFile;
            (int objNum, int offset)[] objOffsets;
            byte[] bytes;
            int first;

            public ObjectStream(PdfFile pdfVersion, PdfStream objStm)
            {
                this.pdfFile = pdfVersion;
                var n = (int)objStm.Dict["N"];
                first = (int)objStm.Dict["First"];
                objOffsets = new (int, int)[n];
                bytes = objStm.GetDecodedBytes();
                var r = new PdfReader(bytes);
                for (int i = 0; i < n; i++)
                {
                    int objNum = (int)new PdfNumeric(r);
                    r.SkipWhiteSpace();
                    int offset = (int)new PdfNumeric(r);
                    r.SkipWhiteSpace();
                    objOffsets[i] = (objNum, offset);
                }
            }

            public PdfObject GetObj(int index)
            {
                var r = new PdfReader(bytes, first + objOffsets[index].offset, pdfFile);
                return r.ReadPdfObject(null);
            }
        }

    }
}
