/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SeaPeaYou.PeaPdf
{
    class Encryption
    {

        static byte[] paddingBytes = new byte[] { 0x28, 0xBF, 0x4E, 0x5E, 0x4E, 0x75, 0x8A, 0x41, 0x64, 0x00, 0x4E, 0x56, 0xFF, 0xFA, 0x01, 0x08, 0x2E, 0x2E, 0x00, 0xB6, 0xD0, 0x68, 0x3E, 0x80, 0x2F, 0x0C, 0xA9, 0xFE, 0x64, 0x53, 0x69, 0x7A };
        static RNGCryptoServiceProvider rng;

        public static byte[] PadBytes(string pwd)
        {
            var bytes = Encoding.ASCII.GetBytes(pwd);
            if (bytes.Length >= 32)
            {
                bytes = bytes.Take(32).ToArray();
            }
            else
            {
                bytes = bytes.Concat(paddingBytes.Take(32 - bytes.Length)).ToArray();
            }
            return bytes;
        }

        public static byte[] ComputeO(string userPwd, string ownerPwd, int? length = null)
        {
            var pwdBytes = PadBytes(ownerPwd ?? userPwd);
            var md5Input = pwdBytes;
            var md5 = MD5.Create();
            for (int i = 0; i < 51; i++)
            {
                md5Input = md5.ComputeHash(md5Input);
            }
            var rc4EncKey = md5Input.Take((length ?? 40) / 8).ToArray();
            var userPwdBytes = PadBytes(userPwd);
            var o = RC4.Encrypt(rc4EncKey, userPwdBytes);
            for (int i = 0; i < 19; i++)
            {
                var iterationCounter = (byte)(i + 1);
                var _rc4EncKey = (byte[])rc4EncKey.Clone();
                for (int j = 0; j < _rc4EncKey.Length; j++)
                {
                    _rc4EncKey[j] ^= iterationCounter;
                }
                o = RC4.Encrypt(_rc4EncKey, o);
            }
            return o;
        }

        public static byte[] ComputeU_Unpadded(byte[] encryptionKey, byte[] fileID)
        {
            var _md5Input = new List<byte>();
            _md5Input.AddRange(paddingBytes);
            _md5Input.AddRange(fileID);
            var md5 = MD5.Create();
            var hash = md5.ComputeHash(_md5Input.ToArray());
            var u = RC4.Encrypt(encryptionKey, hash);
            for (int i = 0; i < 19; i++)
            {
                var iterationCounter = (byte)(i + 1);
                var _rc4EncKey = (byte[])encryptionKey.Clone();
                for (int j = 0; j < _rc4EncKey.Length; j++)
                {
                    _rc4EncKey[j] ^= iterationCounter;
                }
                u = RC4.Encrypt(_rc4EncKey, u);
            }
            return u;
        }

        public static byte[] ComputeU(byte[] encryptionKey, byte[] fileID)
        {
            var u = ComputeU_Unpadded(encryptionKey, fileID);
            var U = new byte[32];
            Array.Copy(u, U, 16);
            return U;

        }

        public static byte[] ComputeEncryptionKey(byte[] userPwd, byte[] O, int P, byte[] fileID, int? length = null)
        {
            var _md5Input = new List<byte>();
            _md5Input.AddRange(userPwd);
            _md5Input.AddRange(O);
            _md5Input.AddRange(BitConverter.GetBytes(P));
            _md5Input.AddRange(fileID);
            var md5 = MD5.Create();
            var hash = md5.ComputeHash(_md5Input.ToArray());
            var n = (length ?? 40) / 8;
            for (int i = 0; i < 50; i++)
            {
                var firstNBytes = hash.Take(n).ToArray();
                hash = md5.ComputeHash(firstNBytes);
            }
            var key = hash.Take(n).ToArray();
            return key;
        }

        public static byte[] GetUserPwd(byte[] ownerPwd, byte[] O, int? length = null)
        {
            var md5Input = ownerPwd;
            var md5 = MD5.Create();
            for (int i = 0; i < 51; i++)
            {
                md5Input = md5.ComputeHash(md5Input);
            }
            var rc4EncKey = md5Input.Take((length ?? 40) / 8).ToArray();
            byte[] o = null;
            for (int i = 19; i >= 0; i--)
            {
                var iterationCounter = (byte)i;
                var _rc4EncKey = (byte[])rc4EncKey.Clone();
                for (int j = 0; j < _rc4EncKey.Length; j++)
                {
                    _rc4EncKey[j] ^= iterationCounter;
                }
                o = RC4.Encrypt(_rc4EncKey, i == 19 ? O : o);
            }
            return o;
        }

        public static byte[] GetRandomBytes()
        {
            if (rng == null) rng = new RNGCryptoServiceProvider();
            var bytes = new byte[16];
            rng.GetBytes(bytes);
            return bytes;
        }
    }
}
