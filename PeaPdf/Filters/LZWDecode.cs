/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace SeaPeaYou.PeaPdf.Filters
{
    class LZWDecode : LZWFlateDecode
    {

        public static byte[] Decode(PdfDict decodeParms, byte[] bytes) => new LZWDecode(decodeParms, bytes).result;

        public LZWDecode(PdfDict decodeParms, byte[] bytes) : base(decodeParms)
        {
            earlyChange = (int?)decodeParms?["EarlyChange"] ?? 1;

            var decodedBytes = new List<byte>();
            var bitReader = new BitReader(new ByteReader(bytes));
            bool started = false;

            Dictionary<int, Entry> entries = new Dictionary<int, Entry>();
            Entry newEntry = default;

            int codeLimit;
            int nextCode;
            int codeLen;
            bool tableLimit;

            ResetEntries();

            while (true)
            {
                int code = bitReader.ReadBits(codeLen);
                var entry = entries[code];
                switch (entry.Type)
                {
                    case EntryType.ClearTable:
                        if (started)
                            ResetEntries();
                        newEntry = null;
                        break;
                    case EntryType.EOD:
                        result = DecodePredictor(decodedBytes.ToArray());
                        return;
                    case EntryType.Data:
                        {
                            if (newEntry != null)
                            {
                                newEntry.Data[newEntry.Data.Length - 1] = entry.Data[0];
                            }
                            if (tableLimit)
                            {
                                if (newEntry != null)
                                    newEntry = null;
                            }
                            else
                            {
                                var newEntryData = new byte[entry.Data.Length + 1];
                                entry.Data.CopyTo(newEntryData, 0);
                                newEntry = new Entry(EntryType.Data, newEntryData);
                                entries.Add(nextCode++, newEntry);
                                if (nextCode > codeLimit)
                                {
                                    if (codeLen == 12)
                                    {
                                        tableLimit = true;
                                    }
                                    else
                                    {
                                        codeLen++;
                                        codeLimit = (1 << codeLen) - earlyChange;
                                    }
                                }
                            }
                            decodedBytes.AddRange(entry.Data);
                            break;
                        }
                }
                if (!started)
                    started = true;
            }

            void ResetEntries()
            {
                entries.Clear();
                for (int i = 0; i <= 255; i++)
                {
                    entries.Add(i, new Entry(EntryType.Data, new[] { (byte)i }));
                }
                entries.Add(256, new Entry(EntryType.ClearTable, null));
                entries.Add(257, new Entry(EntryType.EOD, null));
                codeLen = 9;
                nextCode = 258;
                codeLimit = (1 << codeLen) - earlyChange;
                tableLimit = false;
            }

        }

        int earlyChange;
        byte[] result;

        enum EntryType { Data, ClearTable, EOD }
        class Entry
        {
            public readonly EntryType Type;
            public readonly byte[] Data;

            public Entry(EntryType type, byte[] data)
            {
                Type = type;
                Data = data;
            }
        }

    }
}
