/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace SeaPeaYou.PeaPdf.CS
{
    class Tc : Instruction
    {
        public float charSpace;

        public Tc(List<PdfObject> operands) { charSpace = (float)operands[0]; }
        public Tc(float charSpace) => this.charSpace = charSpace;

        public override string Keyword => "Tc";
        public override IList<PdfObject> GetOperands() => new[] { (PdfNumeric)charSpace };
    }

    class Tw : Instruction
    {
        public float wordSpace;

        public Tw(List<PdfObject> operands) { wordSpace = (float)operands[0]; }
        public Tw(float wordSpace) => this.wordSpace = wordSpace;

        public override string Keyword => "Tw";
        public override IList<PdfObject> GetOperands() => new[] { (PdfNumeric)wordSpace };
    }

    class Tz : Instruction
    {
        public float scale;

        public Tz(List<PdfObject> operands) { scale = (float)operands[0]; }
        public Tz(float scale) => this.scale = scale;

        public override string Keyword => "Tz";
        public override IList<PdfObject> GetOperands() => new[] { (PdfNumeric)scale };
    }

    class TL : Instruction
    {
        public float leading;

        public TL(List<PdfObject> operands) { leading = (float)operands[0]; }
        public TL(float leading) => this.leading = leading;

        public override string Keyword => "TL";
        public override IList<PdfObject> GetOperands() => new[] { (PdfNumeric)leading };
    }

    class Tf : Instruction
    {
        public string font;
        public float size;

        public Tf(List<PdfObject> operands)
        {
            font = operands[0].As<PdfName>().String;
            size = (float)operands[1];
        }
        public Tf(string font, float size) => (this.font, this.size) = (font, size);

        public override string Keyword => "Tf";
        public override IList<PdfObject> GetOperands() => new PdfObject[] { (PdfName)font, (PdfNumeric)size };
    }

    class Tr : Instruction
    {
        public int render;

        public Tr(List<PdfObject> operands) { render = (int)operands[0]; }
        public Tr(int render) => this.render = render;

        public override string Keyword => "Tr";
        public override IList<PdfObject> GetOperands() => new[] { (PdfNumeric)render };
    }

    class Ts : Instruction
    {
        public float rise;

        public Ts(List<PdfObject> operands) { rise = (float)operands[0]; }
        public Ts(float rise) => this.rise = rise;

        public override string Keyword => "Ts";
        public override IList<PdfObject> GetOperands() => new[] { (PdfNumeric)rise };
    }

}
