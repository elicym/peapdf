/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace SeaPeaYou.PeaPdf.W
{
    class WrapperArray<T> : IEnumerable<T>
    {
        PdfArray arr;
        Func<PdfObject, T> getT;
        Func<T, PdfObject> getPdfObject;

        public WrapperArray(PdfArray arr, Func<PdfObject, T> getT, Func<T, PdfObject> getPdfObject)
        {
            this.arr = arr;
            this.getT = getT;
            this.getPdfObject = getPdfObject;
        }

        public T this[int ix] => getT(arr[ix]);

        public void Add(T t) => arr.Add(getPdfObject(t));
        public void Remove(T t) => arr.Remove(getPdfObject(t));

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var item in arr)
            {
                yield return getT(item);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
