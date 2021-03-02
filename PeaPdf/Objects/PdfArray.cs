/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SeaPeaYou.PeaPdf
{
    class PdfArray : PdfObject, IEnumerable<PdfObject>
    {
        readonly List<PdfObject> items = new List<PdfObject>();

        public PdfObject this[int ix] => items[ix];
        public int Count => items.Count;

        public PdfArray(PdfReader r, ObjID? baseObjID)
        {
            if (r.ReadByte() != '[')
                throw new FormatException();
            r.SkipWhiteSpace();
            while (true)
            {
                if (r.PeekByte == ']')
                {
                    r.Pos++;
                    return;
                }
                var obj = r.ReadPdfObject(baseObjID);
                items.Add(r.Deref(obj)); //generally if you have an array, you read all objects, so we deref straight away
                r.SkipWhiteSpace();
            }
        }

        public PdfArray(params PdfObject[] objs)
        {
            items.AddRange(objs);
        }

        internal override void Write(PdfWriter w, ObjID? encryptionObjID)
        {
            w.WriteByte('[');
            w.NeedsDeliminator = false;
            foreach (var obj in items)
            {
                w.WriteObj(obj, encryptionObjID, false);
            }
            w.WriteByte(']');
            w.NeedsDeliminator = false;
        }

        public override PdfObject Clone()
        {
            var newObjs = new PdfObject[items.Count];
            for (int i = 0; i < items.Count; i++)
            {
                newObjs[i] = PdfObject.Clone(items[i]);
            }
            return new PdfArray(newObjs);
        }

        public void Add(PdfObject obj) => items.Add(obj);
        public bool Remove(PdfObject obj) => items.Remove(obj);
        public void Clear() => items.Clear();

        public IEnumerator<PdfObject> GetEnumerator() => items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    }
}
