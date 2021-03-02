/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SeaPeaYou.PeaPdf
{
    abstract class PdfObject
    {
        internal ObjID? ObjID;

        public T As<T>() where T : PdfObject => (T)this;

        /// <summary>If it's an array, it returns the children converted to T, otherwise it creates an array containing just 'this' converted to T.</summary>
        public T[] AsArray<T>() where T : PdfObject
        {
            if (this is PdfArray arr)
                return arr.Cast<T>().ToArray();
            else
                return new[] { (T)this };
        }

        public (T,T)[] AsPairs<T>() where T : PdfObject
        {
            var arr = (PdfArray)this;
            var pairs = new (T, T)[arr.Count / 2];
            for (int i = 0; i < pairs.Length; i++)
            {
                pairs[i] = ((T)arr[i * 2], (T)arr[i * 2 + 1]);
            }
            return pairs;
        }

        public static PdfObject ArraySingleNull(PdfObject[] arr)
        {
            if (arr == null || arr.Length == 0)
                return null;
            if (arr.Length == 1)
                return arr[0];
            return new PdfArray(arr);
        }

        internal abstract void Write(PdfWriter w, ObjID? encryptionObjID);

        public abstract PdfObject Clone();

        public static explicit operator int(PdfObject pdfObj) => (int)((PdfNumeric)pdfObj).Value;

        public static explicit operator int?(PdfObject pdfObj) => pdfObj == null ? (int?)null : (int)pdfObj;

        public static explicit operator decimal(PdfObject pdfObj) => ((PdfNumeric)pdfObj).Value;

        public static explicit operator decimal?(PdfObject pdfObj) => pdfObj == null ? (decimal?)null : (decimal)pdfObj;

        public static explicit operator float(PdfObject pdfObj) => (float)((PdfNumeric)pdfObj).Value;

        public static explicit operator bool?(PdfObject pdfObj) => pdfObj == null ? (bool?)null : ((PdfBool)pdfObj).Value;

        //Unlike the non-static Write, this handles nulls, the object header/footer for a base object, and if we can just have a reference to the object.
        //public static void Write(PdfObject obj, Stream stream, PDF pdf, ObjID? baseObjID, bool isBase)
        //{
        //    if (obj == null)
        //    {
        //        stream.WriteString("null ");
        //        return;
        //    }
        //    if (isBase)
        //    {
        //        new TwoNums(obj.ObjID.Value, "obj").Write(stream, false);
        //        obj.Write(stream, pdf, obj.ObjID.Value);
        //        stream.WriteString("\nendobj\n");
        //        return;
        //    }
        //    if (obj.ObjID != null)
        //    {
        //        new PdfIndirectReference(obj.ObjID.Value).Write(stream, pdf, obj.ObjID);
        //        return;
        //    }
        //    obj.Write(stream, pdf, obj.ObjID);
        //}

        //Unlike the non-static Clone, this handles nulls, and if we can just clone the reference.
        //So far, this is implemented just for the trailer, which has no baseObjID.
        public static PdfObject Clone(PdfObject obj)
        {
            if (obj == null) return null;
            if (obj.ObjID != null) return obj; //this is a base object, so our cloned object can simply link to this object
            return obj.Clone();
        }

    }
}
