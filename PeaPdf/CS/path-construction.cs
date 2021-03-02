/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SeaPeaYou.PeaPdf.CS
{

    /// <summary>
    /// Begin a new subpath by moving the current point to coordinates (x, y),
    /// omitting any connecting line segment.
    /// </summary>
    class m : Instruction
    {
        public float x, y;

        public m(List<PdfObject> operands)
        {
            x = (float)operands[0];
            y = (float)operands[1];
        }
        public m(float x, float y) => (this.x, this.y) = (x, y);

        public override string Keyword => "m";
        public override IList<PdfObject> GetOperands() => new[] { (PdfNumeric)x, (PdfNumeric)y };
    }

    /// <summary>
    /// Append a straight line segment from the current point to the point (x, y).
    /// The new current point shall be (x, y).
    /// </summary>
    class l : Instruction
    {
        public float x, y;

        public l(List<PdfObject> operands)
        {
            x = (float)operands[0];
            y = (float)operands[1];
        }
        public l(float x, float y) => (this.x, this.y) = (x, y);

        public override string Keyword => "l";
        public override IList<PdfObject> GetOperands() => new[] { (PdfNumeric)x, (PdfNumeric)y };
    }

    /// <summary>
    /// Append a cubic Bézier curve to the current path. The curve shall extend
    /// from the current point to the point(x3, y3), using (x1, y1) and(x2, y2 ) as
    /// the Bézier control points
    /// </summary>
    class c : Instruction
    {
        public float x1, y1, x2, y2, x3, y3;

        public c(List<PdfObject> operands)
        {
            x1 = (float)operands[0];
            y1 = (float)operands[1];
            x2 = (float)operands[2];
            y2 = (float)operands[3];
            x3 = (float)operands[4];
            y3 = (float)operands[5];
        }
        public c(float x1, float y1, float x2, float y2, float x3, float y3) => (this.x1, this.y1, this.x2, this.y2, this.x3, this.y3) = (x1, y1, x2, y2, x3, y3);

        public override string Keyword => "c";
        public override IList<PdfObject> GetOperands() => new[] { (PdfNumeric)x1, (PdfNumeric)y1, (PdfNumeric)x2, (PdfNumeric)y2, (PdfNumeric)x3, (PdfNumeric)y3 };
    }

    /// <summary>
    /// Append a cubic Bézier curve to the current path. The curve shall extend
    /// from the current point to the point(x3, y3 ), using the current point and
    /// (x2, y2 ) as the Bézier control points.
    /// </summary>
    class v : Instruction
    {
        public float x2, y2, x3, y3;

        public v(List<PdfObject> operands)
        {
            x2 = (float)operands[0];
            y2 = (float)operands[1];
            x3 = (float)operands[2];
            y3 = (float)operands[3];
        }
        public v(float x2, float y2, float x3, float y3) => (this.x2, this.y2, this.x3, this.y3) = (x2, y2, x3, y3);

        public override string Keyword => "v";
        public override IList<PdfObject> GetOperands() => new[] { (PdfNumeric)x2, (PdfNumeric)y2, (PdfNumeric)x3, (PdfNumeric)y3 };
    }

    /// <summary>
    /// Append a cubic Bézier curve to the current path. The curve shall extend
    /// from the current point to the point (x3, y3), using (x1, y1) and (x3, y3) as
    /// the Bézier control points.
    /// </summary>
    class y : Instruction
    {
        public float x1, y1, x3, y3;

        public y(List<PdfObject> operands)
        {
            x1 = (float)operands[0];
            y1 = (float)operands[1];
            x3 = (float)operands[2];
            y3 = (float)operands[3];
        }
        public y(float x1, float y1, float x3, float y3) => (this.x1, this.y1, this.x3, this.y3) = (x1, y1, x3, y3);

        public override string Keyword => "y";
        public override IList<PdfObject> GetOperands() => new[] { (PdfNumeric)x1, (PdfNumeric)y1, (PdfNumeric)x3, (PdfNumeric)y3 };
    }

    /// <summary>
    /// Close the current subpath by appending a straight line segment from the
    /// current point to the starting point of the subpath.
    /// </summary>
    class h : Instruction
    {
        public h(List<PdfObject> _) { }
        public h() { }

        public override string Keyword => "h";
        public override IList<PdfObject> GetOperands() => null;
    }

    /// <summary>
    /// Append a rectangle to the current path as a complete subpath, with
    /// lower-left corner(x, y) and dimensions width and height in user space.
    /// </summary>
    class re : Instruction
    {
        public float x, y, width, height;

        public re(List<PdfObject> operands)
        {
            x = (float)operands[0];
            y = (float)operands[1];
            width = (float)operands[2];
            height = (float)operands[3];
        }
        public re(float x, float y, float width, float height) => (this.x, this.y, this.width, this.height) = (x, y, width, height);

        public override string Keyword => "re";
        public override IList<PdfObject> GetOperands() => new[] { (PdfNumeric)x, (PdfNumeric)y, (PdfNumeric)width, (PdfNumeric)height };
    }

}
