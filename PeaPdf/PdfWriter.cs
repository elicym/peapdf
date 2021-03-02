/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SeaPeaYou.PeaPdf
{
    class PdfWriter : ByteWriter
    {

        public Dictionary<PdfObject, int> IndirectObjs = new Dictionary<PdfObject, int>();

        public bool NeedsDeliminator;

        byte[] encryptionKey;
        bool encryptionUseRC4;

        public PdfWriter(byte[] encryptionKey = null, bool encryptionUseRC4 = false)
        {
            this.encryptionKey = encryptionKey;
            this.encryptionUseRC4 = encryptionUseRC4;
        }

        public void WriteString(string str)
        {
            WriteBytes(Encoding.ASCII.GetBytes(str));
            NeedsDeliminator = true;
        }

        public void WriteNewLine()
        {
            WriteByte('\n');
            NeedsDeliminator = false;
        }

        public void WriteFullNewLine()
        {
            WriteByte('\r');
            WriteByte('\n');
            NeedsDeliminator = false;
        }

        //Handles null, and if all we need is an Indirect Reference (if is an indirect object and allowIndirectRef).
        public void WriteObj(PdfObject obj, ObjID? encryptionObjID, bool noIndirectRef)
        {
            if (obj == null)
            {
                EnsureDeliminated();
                WriteString("null");
                NeedsDeliminator = true;
                return;
            }
            if (!noIndirectRef && IndirectObjs.TryGetValue(obj, out var objNum))
            {
                new PdfIndirectReference(new ObjID(objNum, 0)).WriteThis(this);
                return;
            }
            obj.Write(this, encryptionObjID);
        }

        public void WriteBaseObj(PdfObject obj, ObjID objID, bool encrypt)
        {
            new TwoNums(objID, "obj").Write(this);
            WriteNewLine();
            WriteObj(obj, encrypt ? objID : (ObjID?)null, true);
            WriteNewLine();
            WriteString("endobj");
            WriteNewLine();
        }

        public byte[] Encrypt(byte[] bytes, ObjID? objID)
        {
            if (encryptionKey == null || objID == null)
                return bytes;
            var key = encryptionKey.ToList();
            key.AddRange(BitConverter.GetBytes(objID.Value.ObjNum).Take(3));
            key.AddRange(BitConverter.GetBytes(objID.Value.GenNum).Take(2));
            if (!encryptionUseRC4)
            {
                key.AddRange(new byte[] { 0x73, 0x41, 0x6C, 0x54 });
            }
            var md5 = MD5.Create();
            var finalKey = md5.ComputeHash(key.ToArray()).Take(encryptionKey.Length + 5).ToArray();
            if (encryptionUseRC4)
            {
                return RC4.Encrypt(finalKey.ToArray(), bytes);
            }
            else
            {
                var iv = Encryption.GetRandomBytes();
                using (var rijndael = new RijndaelManaged { Key = finalKey, IV = iv, Mode = CipherMode.CBC, BlockSize = 128 })
                using (var msS = new MemoryStream(bytes))
                using (var msD = new MemoryStream())
                {
                    msD.Write(iv);
                    using (var cryptoStream = new CryptoStream(msD, rijndael.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(bytes);
                    }
                    return msD.ToArray();
                }
            }
        }

        public void EnsureDeliminated()
        {
            if (NeedsDeliminator)
            {
                WriteByte(' ');
                NeedsDeliminator = false;
            }
        }


    }
}
