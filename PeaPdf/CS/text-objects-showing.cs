/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace SeaPeaYou.PeaPdf.CS
{
    /// <summary>Begin a text object, initializing the text matrix, Tm, and the text line matrix, Tlm, to the identity matrix.</summary>
    class BT : Instruction
    {
        public BT(List<PdfObject> _) { }
        public BT() { }

        public override string Keyword => "BT";
        public override IList<PdfObject> GetOperands() => null;
    }

    /// <summary>End a text object, discarding the text matrix.</summary>
    class ET : Instruction
    {
        public ET(List<PdfObject> _) { }
        public ET() { }

        public override string Keyword => "ET";
        public override IList<PdfObject> GetOperands() => null;
    }

    /// <summary>Show a text string.</summary>
    class Tj : Instruction
    {
        public PdfString @string;

        public Tj(List<PdfObject> operands) => @string = (PdfString)operands[0];
        public Tj(PdfString @string) { this.@string = @string; }

        public override string Keyword => "Tj";
        public override IList<PdfObject> GetOperands() => new[] { @string };
    }

    /// <summary>Move to the next line and show a text string.</summary>
    class Apostrophe : Instruction
    {
        public PdfString @string;

        public Apostrophe(List<PdfObject> operands) => @string = (PdfString)operands[0];
        public Apostrophe(PdfString @string) { this.@string = @string; }

        public override string Keyword => "'";
        public override IList<PdfObject> GetOperands() => new[] { @string };
    }

    /// <summary>Move to the next line and show a text string, using aw as the word spacing and ac as the character spacing.</summary>
    class Quote : Instruction
    {
        public float aw, ac;
        public PdfString @string;

        public Quote(List<PdfObject> operands)
        {
            aw = (float)operands[0];
            ac = (float)operands[1];
            @string = (PdfString)operands[2];
        }
        public Quote(float aw, float ac, PdfString @string) { this.aw = aw; this.ac = ac; this.@string = @string; }

        public override string Keyword => "\"";
        public override IList<PdfObject> GetOperands() => new PdfObject[] { (PdfNumeric)aw, (PdfNumeric)ac, @string };
    }

    /// <summary>
    /// Show zero or more text strings, allowing individual glyph positioning.
    /// Each element of array shall be either a string or a number.
    /// If the element is a string, this operator shall show the string.
    /// If it is a number, the operator shall adjust the text position by that amount; that is, it shall translate the text matrix, Tm.
    /// The number shall be expressed in thousandths of a unit of text space.
    /// </summary>
    class TJ : Instruction
    {
        public PdfArray Array;

        public TJ(List<PdfObject> operands) => Array = (PdfArray)operands[0];
        public TJ(PdfArray Array) => this.Array = Array;

        public override string Keyword => "TJ";
        public override IList<PdfObject> GetOperands() => new[] { Array };
    }

}
