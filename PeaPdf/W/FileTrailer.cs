/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SeaPeaYou.PeaPdf.W
{
    class FileTrailer
    {

        public readonly PdfDict Dict;

        public FileTrailer(PdfDict dict) { Dict = dict; }
        public FileTrailer()
        {
            Dict = new PdfDict();
            Root = new Catalog("1.7");
        }

        public int? Size { get => (int?)Dict["Size"]; set => Dict["Size"] = (PdfNumeric)value; }

        public int? Prev { get => (int?)Dict["Prev"]; set => Dict["Prev"] = (PdfNumeric)value; }

        public Catalog Root { get => new Catalog((PdfDict)Dict["Root"]); set => Dict["Root"] = value.Dict; }

        public PdfDict Encrypt { get => (PdfDict)Dict["Encrypt"]; set => Dict["Encrypt"] = value; }

        public PdfDict Info { get => (PdfDict)Dict["Info"]; set => Dict["Info"] = value; }

        public (byte[], byte[])? ID
        {
            get => Dict["ID"]?.As<PdfArray>().To(x => (x[0].As<PdfString>().Value, x[1].As<PdfString>().Value));
            set => Dict["ID"] = value == null ? null : new PdfArray((PdfString)value.Value.Item1, (PdfString)value.Value.Item2);
        }

    }

    class FileTrailerStream
    {

        public readonly FileTrailer FileTrailer;

        public FileTrailerStream(FileTrailer fileTrailer) { FileTrailer = fileTrailer; }

        public IList<(int objNum, int count)> Index
        {
            get => FileTrailer.Dict["Index"]?.AsPairs<PdfNumeric>().Select(x => ((int)x.Item1, (int)x.Item2)).ToList();
            set => FileTrailer.Dict["Index"] = value == null ? null : new PdfArray(value.SelectMany(x => new[] { (PdfNumeric)x.objNum, (PdfNumeric)x.count }).ToArray());
        }

        public (int, int, int) W
        {
            get
            {
                var w = (PdfArray)FileTrailer.Dict["W"];
                return ((int)w[0], (int)w[1], (int)w[2]);
            }
            set
            {
                FileTrailer.Dict["W"] = new PdfArray((PdfNumeric)value.Item1, (PdfNumeric)value.Item2, (PdfNumeric)value.Item3);
            }
        }

    }


}
