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
    /// <summary>Save the current graphics state on the graphics state stack.</summary>
    class q : Instruction
    {
        public q(List<PdfObject> _) { }
        public q() { }

        public override string Keyword => "q";
        public override IList<PdfObject> GetOperands() => null;
    }

    /// <summary>Restore the graphics state by removing the most recently saved state from the stack and making it the current state.</summary>
    class Q : Instruction
    {
        public Q(List<PdfObject> _) { }
        public Q() { }

        public override string Keyword => "Q";
        public override IList<PdfObject> GetOperands() => null;
    }

    /// <summary>Modify the current transformation matrix (CTM) by concatenating the specified matrix.</summary>
    class cm : Instruction
    {
        public SKMatrix Matrix;

        public cm(List<PdfObject> operands)
        {
            Matrix = Utils.MatrixFromArray(operands.Select(x => (float)x).ToArray());
        }
        public cm(float scaleX, float skewY, float skewX, float scaleY, float transX, float transY) => Matrix = Utils.MatrixFromArray(new[] { scaleX, skewY, skewX, scaleY, transX, transY });
        public cm(SKMatrix matrix) => Matrix = matrix;

        public override string Keyword => "cm";
        public override IList<PdfObject> GetOperands() 
            => new[] { (PdfNumeric)Matrix.ScaleX, (PdfNumeric)Matrix.SkewY, (PdfNumeric)Matrix.SkewX, (PdfNumeric)Matrix.ScaleY, (PdfNumeric)Matrix.TransX, (PdfNumeric)Matrix.TransY };
    }

    class w : Instruction
    {
        public float lineWidth;

        public w(List<PdfObject> operands) => lineWidth = (float)operands[0];
        public w(float lineWidth) => this.lineWidth = lineWidth;

        public override string Keyword => "w";
        public override IList<PdfObject> GetOperands() => new[] { (PdfNumeric)lineWidth };
    }

    class J : Instruction
    {
        public SKStrokeCap lineCap;

        public J(List<PdfObject> operands) => lineCap = (SKStrokeCap)(int)operands[0];
        public J(SKStrokeCap lineCap) => this.lineCap = lineCap;

        public override string Keyword => "J";
        public override IList<PdfObject> GetOperands() => new[] { (PdfNumeric)(int)lineCap };
    }

    class j : Instruction
    {
        public SKStrokeJoin lineJoin;

        public j(List<PdfObject> operands) => lineJoin = (SKStrokeJoin)(int)operands[0];
        public j(SKStrokeJoin lineJoin) => this.lineJoin = lineJoin;

        public override string Keyword => "j";
        public override IList<PdfObject> GetOperands() => new[] { (PdfNumeric)(int)lineJoin };
    }

    class M : Instruction
    {
        public float miterLimit;

        public M(List<PdfObject> operands) => miterLimit = (float)operands[0];
        public M(float miterLimit) => this.miterLimit = miterLimit;

        public override string Keyword => "M";
        public override IList<PdfObject> GetOperands() => new[] { (PdfNumeric)miterLimit };
    }

    class d : Instruction
    {
        public float[] dashArray;
        public float dashPhase;

        public d(List<PdfObject> operands)
        {
            dashArray = ((PdfArray)operands[0]).Select(x => (float)x).ToArray();
            dashPhase = (float)operands[1];
        }
        public d(float[] dashArray, float dashPhase) => (this.dashArray, this.dashPhase) = (dashArray, dashPhase);

        public override string Keyword => "d";
        public override IList<PdfObject> GetOperands() => new PdfObject[] { new PdfArray(dashArray.Select(x => (PdfNumeric)x).ToArray()), (PdfNumeric)dashPhase };
    }

    class ri : Instruction
    {
        public string intent;

        public ri(List<PdfObject> operands) { intent = operands[0].As<PdfName>().String; }
        public ri(string intent) => this.intent = intent;

        public override string Keyword => "ri";
        public override IList<PdfObject> GetOperands() => new[] { (PdfName)intent };
    }

    class i : Instruction
    {
        public float flatness;

        public i(List<PdfObject> operands) => flatness = (float)operands[0];
        public i(float flatness) => this.flatness = flatness;

        public override string Keyword => "i";
        public override IList<PdfObject> GetOperands() => new[] { (PdfNumeric)flatness };
    }

    class gs : Instruction
    {
        public string dictName;

        public gs(List<PdfObject> operands) => dictName = ((PdfName)operands[0]).String;
        public gs(string dictName) => this.dictName = dictName;

        public override string Keyword => "gs";
        public override IList<PdfObject> GetOperands() => new[] { (PdfName)dictName };
    }


}
