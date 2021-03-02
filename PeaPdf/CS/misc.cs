/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace SeaPeaYou.PeaPdf.CS
{
    class d0 : Instruction
    {
        public float wx;

        public d0(List<PdfObject> operands) => wx = (float)operands[0];

        public override string Keyword => "d0";
        public override IList<PdfObject> GetOperands() => new[] { (PdfNumeric)wx, (PdfNumeric)0 };
    }

    class d1 : Instruction
    {
        public float wx, llx, lly, urx, ury;

        public d1(List<PdfObject> operands)
        {
            wx = (float)operands[0];
            llx = (float)operands[2];
            lly = (float)operands[3];
            urx = (float)operands[4];
            ury = (float)operands[5];
        }

        public override string Keyword => "d1";
        public override IList<PdfObject> GetOperands() => new[] { (PdfNumeric)wx, (PdfNumeric)0, (PdfNumeric)llx, (PdfNumeric)lly, (PdfNumeric)urx, (PdfNumeric)ury };
    }

    class sh : Instruction
    {
        public string name;

        public sh(List<PdfObject> operands) => name = operands[0].As<PdfName>().String;

        public override string Keyword => "sh";
        public override IList<PdfObject> GetOperands() => new[] { (PdfName)name };
    }

    class Do : Instruction
    {
        public string name;

        public Do(List<PdfObject> operands) => name = operands[0].As<PdfName>().String;
        public Do(string name) => this.name = name;

        public override string Keyword => "Do";
        public override IList<PdfObject> GetOperands() => new[] { (PdfName)name };
    }

    class BX : Instruction
    {
        public BX(List<PdfObject> _) { }

        public override string Keyword => "";
        public override IList<PdfObject> GetOperands() => null;
    }

    class EX : Instruction
    {
        public EX(List<PdfObject> _) { }

        public override string Keyword => "";
        public override IList<PdfObject> GetOperands() => null;
    }
}
