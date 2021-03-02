/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.Text;

#pragma warning disable IDE1006

namespace SeaPeaYou.PeaPdf.CS
{

    /// <summary>Set the current colour space to use for stroking operations.</summary>
    class CS : Instruction
    {
        public string name;

        public CS(List<PdfObject> operands) => name = operands[0].As<PdfName>().String;
        public CS(string name) => this.name = name;

        public override string Keyword => "CS";
        public override IList<PdfObject> GetOperands() => new[] { (PdfName)name };
    }

    /// <summary>Same as CS but used for nonstroking operations.</summary>
    class cs : Instruction
    {
        public string name;

        public cs(List<PdfObject> operands) => name = operands[0].As<PdfName>().String;
        public cs(string name) => this.name = name;

        public override string Keyword => "cs";
        public override IList<PdfObject> GetOperands() => new[] { (PdfName)name };
    }

    /// <summary></summary>
    abstract class ColorFamily : Other
    {
        public bool IsStroke;
        public ColorSpace? ColorSpace;

        public ColorFamily(string keyword, IList<PdfObject> operands, bool isStroke, ColorSpace? colorSpace) : base(keyword, operands)
        {
            IsStroke = isStroke;
            ColorSpace = colorSpace;
        }
    }

    /// <summary>Set the colour to use for stroking operations.</summary>
    class SC : ColorFamily
    {
        public SC(IList<PdfObject> operands) : base("SC", operands, true, null) { }
    }

    /// <summary>Same as SC but used for nonstroking operations.</summary>
    class sc : ColorFamily
    {
        public sc(IList<PdfObject> operands) : base("sc", operands, false, null) { }
    }

    /// <summary>
    /// Set the stroking colour space to DeviceGray and set the gray level to use for stroking
    /// operations. gray shall be a number between 0.0 (black) and 1.0 (white).
    /// </summary>
    class G : ColorFamily
    {
        public G(IList<PdfObject> operands) : base("G", operands, true, PeaPdf.ColorSpace.DeviceGray) { }
        public G(float gray) : this(new[] { (PdfNumeric)gray }) {}
    }

    /// <summary>Same as G but used for nonstroking operations.</summary>
    class g : ColorFamily
    {
        public g(IList<PdfObject> operands) : base("g", operands, false, PeaPdf.ColorSpace.DeviceGray) { }
        public g(float gray) : this(new[] { (PdfNumeric)gray }) { }
    }

    /// <summary>
    /// Set the stroking colour space to DeviceRGB and set the colour to use for stroking
    /// operations.Each operand shall be a number between 0.0 (minimum intensity)
    /// and 1.0 (maximum intensity).
    /// </summary>
    class RG : ColorFamily
    {
        public RG(IList<PdfObject> operands) : base("RG", operands, true, PeaPdf.ColorSpace.DeviceRGB) { }
        public RG(float r, float g, float b) : this(new[] { (PdfNumeric)r, (PdfNumeric)g, (PdfNumeric)b }) { }
        public RG(SkiaSharp.SKColor color) : this(new[] { (PdfNumeric)(color.Red/255f), (PdfNumeric)(color.Green / 255f), (PdfNumeric)(color.Blue / 255f) }) { }
    }

    /// <summary>Same as RG but used for nonstroking operations.</summary>
    class rg : ColorFamily
    {
        public rg(IList<PdfObject> operands) : base("rg", operands, false, PeaPdf.ColorSpace.DeviceRGB) { }
        public rg(float r, float g, float b) : this(new[] { (PdfNumeric)r, (PdfNumeric)g, (PdfNumeric)b }) { }
        public rg(SkiaSharp.SKColor color) : this(new[] { (PdfNumeric)(color.Red/255f), (PdfNumeric)(color.Green / 255f), (PdfNumeric)(color.Blue / 255f) }) { }
    }

    /// <summary>
    /// Set the stroking colour space to DeviceCMYK and set the colour to use for stroking operations.
    /// Each operand shall be a number between 0.0 (zero concentration) and 1.0 (maximum concentration_/
    /// </summary>
    class K : ColorFamily
    {
        public K(IList<PdfObject> operands) : base("K", operands, true, PeaPdf.ColorSpace.DeviceCMYK) { }
        public K(float c, float m, float y, float k) : this(new[] { (PdfNumeric)c, (PdfNumeric)m, (PdfNumeric)y, (PdfNumeric)k }) { }
    }

    /// <summary>Same as K but used for nonstroking operations.</summary>
    class k : ColorFamily
    {
        public k(IList<PdfObject> operands) : base("k", operands, false, PeaPdf.ColorSpace.DeviceCMYK) { }
        public k(float c, float m, float y, float k) : this(new[] { (PdfNumeric)c, (PdfNumeric)m, (PdfNumeric)y, (PdfNumeric)k }) { }
    }

}
