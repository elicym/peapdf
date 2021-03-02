/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace SeaPeaYou.PeaPdf.W
{

    enum FieldType { None, Btn, Tx, Ch, Sig }
    enum Alignment { Left, Centered, Right }

    class Field
    {
        public readonly PdfDict PdfDict;

        public Field(PdfDict dict) => this.PdfDict = dict;

        /// <summary>The field type. Required for terminal fields.</summary>
        public FieldType FT
        {
            get
            {
                var name = PdfDict["FT"].As<PdfName>()?.String;
                if (name == null) return FieldType.None;
                switch (name)
                {
                    case "Btn": return FieldType.Btn;
                    case "Tx": return FieldType.Tx;
                    case "Ch": return FieldType.Ch;
                    case "Sig": return FieldType.Sig;
                    default: throw new NotSupportedException(name);
                }
            }
        }

        /// <summary>Required if not the root field.</summary>
        public PdfDict Parent { get => (PdfDict)PdfDict["Parent"]; }

        /// <summary></summary>
        /// <summary>Required if not terminal.</summary>
        public PdfArray<PdfDict>? Kids { get => ((PdfArray)PdfDict["Kids"])?.To(x => new PdfArray<PdfDict>(x)); }

        /// <summary>The partial field name. Required.</summary>
        public PdfString T { get => (PdfString)PdfDict["T"]; }

        /// <summary>UI field name.</summary>
        public PdfString TU { get => (PdfString)PdfDict["TU"]; }

        /// <summary>Mapping name.</summary>
        public PdfString TM { get => (PdfString)PdfDict["TM"]; }

        /// <summary>Default appearance instructions. Required for variable text fields.</summary>
        public PdfString DA { get => (PdfString)PdfDict["DA"]; }

        /// <summary>Resources for appearance stream.</summary>
        public ResourceDictionary DR => ((PdfDict)PdfDict["DR"])?.To(x => new ResourceDictionary(x));

        /// <summary>Alignment. Required for variable text fields.</summary>
        public Alignment Q { get => (Alignment)((int?)PdfDict["Q"] ?? 0); }

        /// <summary>Field's value.</summary>
        public PdfObject V { get => PdfDict["V"]; set => PdfDict["V"] = value; }

        /// <summary>For text fields;</summary>
        public int? MaxLen { get => (int?)PdfDict["MaxLen"]; }

        int flag => (int?)PdfDict["Ff"] ?? 0;

        public bool ReadOnly { get => (flag & 1) > 0; }
        public bool Required { get => (flag & 2) > 0; }
        public bool NoExport { get => (flag & 4) > 0; }
        public bool Multiline { get => (flag & 0x1000) > 0; }
        public bool Password { get => (flag & 0x2000) > 0; }
        public bool FileSelect { get => (flag & 0x10_0000) > 0; }
        public bool DoNotSpellCheck { get => (flag & 0x40_0000) > 0; }
        public bool DoNotScroll { get => (flag & 0x80_0000) > 0; }
        public bool Comb { get => (flag & 0x100_0000) > 0; }

    }

}
