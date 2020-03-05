/*
 * Copyright 2020 Elliott Cymerman
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

        public T As<T>() where T : PdfObject => (T)this;

        public T[] AsArray<T>() where T : PdfObject
        {
            if (this is PdfArray arr)
                return arr.Cast<T>().ToArray();
            else
                return new[] { (T)this };
        }

        public static explicit operator int(PdfObject pdfObj) => (int)((PdfNumeric)pdfObj).Value;

        public static explicit operator int?(PdfObject pdfObj) => pdfObj == null ? (int?)null : (int)pdfObj;

        public static explicit operator decimal(PdfObject pdfObj) => ((PdfNumeric)pdfObj).Value;

        public static explicit operator float(PdfObject pdfObj) => (float)((PdfNumeric)pdfObj).Value;

        public static explicit operator bool?(PdfObject pdfObj) => pdfObj == null ? (bool?)null : ((PdfBool)pdfObj).Value;

        public abstract void Write(Stream stream, PDF pdf, PdfIndirectReference iRef);


    }
}
