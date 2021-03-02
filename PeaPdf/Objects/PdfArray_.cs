using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace SeaPeaYou.PeaPdf
{
    struct PdfArray<T>: IEnumerable<T> where T : PdfObject
    {

        PdfArray arr;

        public PdfArray(PdfArray arr) { this.arr = arr; }

        public T this[int ix] => (T)arr[ix];
        public int Count => arr.Count;

        public void Add(T t) => arr.Add(t);
        public void Remove(T t) => arr.Remove(t);

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var item in arr)
            {
                yield return (T)item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    }
}
