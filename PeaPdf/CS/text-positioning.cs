/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SeaPeaYou.PeaPdf.CS
{
    /// <summary>
    /// Move to the start of the next line, offset from the start of the current line by (tx, ty).
    /// </summary>
    class Td : Instruction
    {
        public float tx, ty;

        public Td(List<PdfObject> operands)
        {
            tx = (float)operands[0];
            ty = (float)operands[1];
        }
        public Td(float tx, float ty) => (this.tx, this.ty) = (tx, ty);

        public override string Keyword => "Td";
        public override IList<PdfObject> GetOperands() => new[] { (PdfNumeric)tx, (PdfNumeric)ty };
    }

    /// <summary>
    /// Move to the start of the next line, offset from the start of the
    /// current line by (tx, ty). As a side effect, this operator shall set
    /// the leading parameter in the text state.
    /// </summary>
    class TD : Instruction
    {
        public float tx, ty;

        public TD(List<PdfObject> operands)
        {
            tx = (float)operands[0];
            ty = (float)operands[1];
        }
        public TD(float tx, float ty) => (this.tx, this.ty) = (tx, ty);

        public override string Keyword => "TD";
        public override IList<PdfObject> GetOperands() => new[] { (PdfNumeric)tx, (PdfNumeric)ty };
    }

    /// <summary>Set the text matrix, Tm, and the text line matrix, Tlm.</summary>
    class Tm : Instruction
    {
        public SKMatrix Matrix;

        public Tm(List<PdfObject> operands)
        {
            Matrix = Utils.MatrixFromArray(operands.Select(x => (float)x).ToArray());
        }
        public Tm(float a, float b, float c, float d, float e, float f) => Matrix = Utils.MatrixFromArray(new[] { a, b, c, d, e, f });

        public override string Keyword => "Tm";
        public override IList<PdfObject> GetOperands() => new[] { (PdfNumeric)Matrix.ScaleX, (PdfNumeric)Matrix.SkewY, (PdfNumeric)Matrix.SkewX, (PdfNumeric)Matrix.ScaleY, (PdfNumeric)Matrix.TransX, (PdfNumeric)Matrix.TransY };
    }

    /// <summary>
    /// Move to the start of the next line. This operator has the same effect as the code
    /// 0–Tl TD where Tl denotes the current leading parameter in the text state.
    /// </summary>
    class T_ : Instruction
    {
        public T_(List<PdfObject> _) { }
        public T_() { }

        public override string Keyword => "T*";
        public override IList<PdfObject> GetOperands() => null;
    }

}
