/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SeaPeaYou.PeaPdf.W
{
    class Annotation
    {
        public readonly PdfDict PdfDict;
        /// <summary>Appearance dictionary. Required for most annotation types, if has a non-zero size.</summary>
        public AppearanceStream AP;
        public List<CS.Instruction> DA;

        public Annotation(PdfDict dict)
        {
            this.PdfDict = dict;
            dict["AP"].IfNotNull(x => AP = new AppearanceStream((PdfDict)x));
            dict["DA"].IfNotNull(x => DA = ContentStream.ParseInstructions(((PdfString)x).Value));
        }

        public Annotation(string subtype, Rectangle rect)
        {
            PdfDict = new PdfDict();
            PdfDict["Subtype"] = (PdfName)subtype;
            PdfDict["Rect"] = rect.PdfArray;
        }

        /// <summary>Location. Required.</summary>
        public Rectangle Rect { get => new Rectangle((PdfArray)PdfDict["Rect"]); }

        int flag => (int?)PdfDict["F"] ?? 0;

        public bool Invisible => (flag & 1) > 0;
        public bool Hidden => (flag & 2) > 0;
        public bool Print => (flag & 4) > 0;
        public bool NoZoom => (flag & 8) > 0;
        public bool NoRotate => (flag & 0x10) > 0;
        public bool NoView => (flag & 0x20) > 0;
        public bool ReadOnly => (flag & 0x40) > 0;
        public bool Locked => (flag & 0x80) > 0;
        public bool ToggleNoView => (flag & 0x100) > 0;
        public bool LockedContents => (flag & 0x200) > 0;

        public void UpdateObjects()
        {
            if (PdfDict["T"].Equals((PdfString)"phone")) Debugger.Break();
            AP?.UpdateObjects();
            PdfDict["AP"] = AP?.PdfDict;
        }
    }
}
