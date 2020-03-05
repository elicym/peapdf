/*
 * Copyright 2020 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SeaPeaYou.PeaPdf.Filters
{
    //from https://github.com/coding-horror/ascii85/ MIT, no copyright notice there
    class ASCII85Decode
    {

        public static byte[] Decode(byte[] bytes) => new ASCII85Decode(bytes).result;

        ASCII85Decode(byte[] bytes)
        {
            MemoryStream ms = new MemoryStream();
            int count = 0;
            int start = (bytes.Length >= 2 && bytes[0] == '<' && bytes[1] == '~') ? 2 : 0,
                end = bytes.Length - ((bytes.Length >= 2 && bytes[bytes.Length - 1] == '>' && bytes[bytes.Length - 2] == '~') ? 2 : 0);
            for (int i = start; i < end; i++)
            {
                char c = (char)bytes[i];
                bool processChar;
                switch (c)
                {
                    case 'z':
                        if (count != 0)
                        {
                            throw new Exception("The character 'z' is invalid inside an ASCII85 block.");
                        }
                        _decodedBlock[0] = 0;
                        _decodedBlock[1] = 0;
                        _decodedBlock[2] = 0;
                        _decodedBlock[3] = 0;
                        ms.Write(_decodedBlock, 0, _decodedBlock.Length);
                        processChar = false;
                        break;
                    case '\n':
                    case '\r':
                    case '\t':
                    case '\0':
                    case '\f':
                    case '\b':
                        processChar = false;
                        break;
                    default:
                        if (c < '!' || c > 'u')
                        {
                            throw new Exception("Bad character '" + c + "' found. ASCII85 only allows characters '!' to 'u'.");
                        }
                        processChar = true;
                        break;
                }

                if (processChar)
                {
                    _tuple += ((uint)(c - _asciiOffset) * pow85[count]);
                    count++;
                    if (count == _encodedBlock.Length)
                    {
                        DecodeBlock();
                        ms.Write(_decodedBlock, 0, _decodedBlock.Length);
                        _tuple = 0;
                        count = 0;
                    }
                }
            }

            // if we have some bytes left over at the end..
            if (count != 0)
            {
                if (count == 1)
                {
                    throw new Exception("The last block of ASCII85 data cannot be a single byte.");
                }
                count--;
                _tuple += pow85[count];
                DecodeBlock(count);
                for (int i = 0; i < count; i++)
                {
                    ms.WriteByte(_decodedBlock[i]);
                }
            }

            result = ms.ToArray();

        }

        const int _asciiOffset = 33;
        byte[] _encodedBlock = new byte[5];
        byte[] _decodedBlock = new byte[4];
        uint _tuple = 0;

        uint[] pow85 = { 85 * 85 * 85 * 85, 85 * 85 * 85, 85 * 85, 85, 1 };

        byte[] result;

        void DecodeBlock()
        {
            DecodeBlock(_decodedBlock.Length);
        }

        void DecodeBlock(int bytes)
        {
            for (int i = 0; i < bytes; i++)
            {
                _decodedBlock[i] = (byte)(_tuple >> 24 - (i * 8));
            }
        }

    }
}