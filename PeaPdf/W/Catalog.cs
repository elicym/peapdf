/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace SeaPeaYou.PeaPdf.W
{
    class Catalog
    {
        public readonly PdfDict Dict;

        public Catalog(PdfDict dict) => this.Dict = dict;

        public Catalog(string version)
        {
            Dict = new PdfDict { Type = "Catalog" };
            Version = version;
            Pages = new PageTreeNode(new List<Page>());
        }

        public string Version { get => Dict["Version"].As<PdfName>()?.String; set => Dict["Version"] = (PdfName)value; }

        public PdfDict Extensions { get => (PdfDict)Dict["Extensions"]; set => Dict["Extensions"] = value; }

        public PageTreeNode Pages
        {
            get => new PageTreeNode((PdfDict)Dict["Pages"]);
            set => Dict["Pages"] = value.PdfDict;
        }

        public PdfDict PageLabels { get => (PdfDict)Dict["PageLabels"]; set => Dict["PageLabels"] = value; }

        public PdfDict Names { get => (PdfDict)Dict["Names"]; set => Dict["Names"] = value; }

        public PdfDict Dests { get => (PdfDict)Dict["Dests"]; set => Dict["Dests"] = value; }

        public PdfDict ViewerPreferences { get => (PdfDict)Dict["ViewerPreferences"]; set => Dict["ViewerPreferences"] = value; }

        public string PageLayout { get => Dict["PageLayout"].As<PdfName>()?.String; set => Dict["PageLayout"] = (PdfName)value; }

        public string PageMode { get => Dict["PageMode"].As<PdfName>()?.String; set => Dict["PageMode"] = (PdfName)value; }

        public PdfDict Outlines { get => (PdfDict)Dict["Outlines"]; set => Dict["Outlines"] = value; }

        public PdfArray Threads { get => (PdfArray)Dict["Threads"]; set => Dict["Threads"] = value; }

        public PdfObject OpenAction { get => Dict["OpenAction"]; set => Dict["OpenAction"] = value; }

        public PdfDict AA { get => (PdfDict)Dict["AA"]; set => Dict["AA"] = value; }

        public PdfDict URI { get => (PdfDict)Dict["URI"]; set => Dict["URI"] = value; }

        public AcroForm AcroForm { get => ((PdfDict)Dict["AcroForm"])?.To(x => new AcroForm(x)); }

        public PdfStream Metadata { get => (PdfStream)Dict["Metadata"]; set => Dict["Metadata"] = value; }

        public PdfDict StructTreeRoot { get => (PdfDict)Dict["StructTreeRoot"]; set => Dict["StructTreeRoot"] = value; }

        public PdfDict MarkInfo { get => (PdfDict)Dict["MarkInfo"]; set => Dict["MarkInfo"] = value; }

        public string Lang { get => Dict["Lang"].As<PdfString>()?.ToString(); set => Dict["Lang"] = (PdfString)value; }

        public PdfDict SpiderInfo { get => (PdfDict)Dict["SpiderInfo"]; set => Dict["SpiderInfo"] = value; }

        public PdfArray OutputIntents { get => (PdfArray)Dict["OutputIntents"]; set => Dict["OutputIntents"] = value; }

        public PdfDict PieceInfo { get => (PdfDict)Dict["PieceInfo"]; set => Dict["PieceInfo"] = value; }

        public PdfDict OCProperties { get => (PdfDict)Dict["OCProperties"]; set => Dict["OCProperties"] = value; }

        public PdfDict Perms { get => (PdfDict)Dict["Perms"]; set => Dict["Perms"] = value; }

        public PdfDict Legal { get => (PdfDict)Dict["Legal"]; set => Dict["Legal"] = value; }

        public PdfArray Requirements { get => (PdfArray)Dict["Requirements"]; set => Dict["Requirements"] = value; }

        public PdfDict Collection { get => (PdfDict)Dict["Collection"]; set => Dict["Collection"] = value; }

        public bool? NeedsRendering { get => Dict["NeedsRendering"].As<PdfBool>()?.Value; set => Dict["NeedsRendering"] = (PdfBool)value; }

        public PdfDict DSS { get => (PdfDict)Dict["DSS"]; set => Dict["DSS"] = value; }

        public PdfArray AF { get => (PdfArray)Dict["AF"]; set => Dict["AF"] = value; }

        public PdfDict DPartRoot { get => (PdfDict)Dict["DPartRoot"]; set => Dict["DPartRoot"] = value; }

    }
}
