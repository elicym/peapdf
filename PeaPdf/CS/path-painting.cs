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

    /// <summary>Stroke the path.</summary>
    class S : Instruction
    {
        public S(List<PdfObject> _) { }
        public S() { }

        public override string Keyword => "S";
        public override IList<PdfObject> GetOperands() => null;
    }

    /// <summary></summary>
    /// <summary>Close and stroke the path</summary>
    class s : Instruction
    {
        public s(List<PdfObject> _) { }
        public s() { }

        public override string Keyword => "s";
        public override IList<PdfObject> GetOperands() => null;
    }

    /// <summary>Fill the path, using the non-zero winding number rule to determine the region to fill.</summary>
    class f : Instruction
    {
        public f(List<PdfObject> _) { }
        public f() { }

        public override string Keyword => "f";
        public override IList<PdfObject> GetOperands() => null;
    }

    /// <summary>Equivalent to f; included only for compatibility.</summary>
    class F : Instruction
    {
        public F(List<PdfObject> _) { }
        public F() { }

        public override string Keyword => "F";
        public override IList<PdfObject> GetOperands() => null;
    }

    /// <summary>Fill the path, using the even-odd rule to determine the region to fill.</summary>
    class f_ : Instruction
    {
        public f_(List<PdfObject> _) { }
        public f_() { }

        public override string Keyword => "f*";
        public override IList<PdfObject> GetOperands() => null;
    }

    /// <summary>Fill and then stroke the path, using the non-zero winding number rule to determine the region to fill.</summary>
    class B : Instruction
    {
        public B(List<PdfObject> _) { }
        public B() { }

        public override string Keyword => "B";
        public override IList<PdfObject> GetOperands() => null;
    }

    /// <summary>Fill and then stroke the path, using the even-odd rule to determine the region to fill.</summary>
    class B_ : Instruction
    {
        public B_(List<PdfObject> _) { }

        public override string Keyword => "B*";
        public override IList<PdfObject> GetOperands() => null;
    }

    /// <summary>Close, fill, and then stroke the path, using the non-zero winding number rule to determine the region to fill.</summary>
    class b : Instruction
    {
        public b(List<PdfObject> _) { }
        public b() { }

        public override string Keyword => "b";
        public override IList<PdfObject> GetOperands() => null;
    }

    /// <summary>Close, fill, and then stroke the path, using the even-odd rule to determine the region to fill.</summary>
    class b_ : Instruction
    {
        public b_(List<PdfObject> _) { }
        public b_() { }

        public override string Keyword => "b*";
        public override IList<PdfObject> GetOperands() => null;
    }

    /// <summary>End the path object without filling or stroking it.</summary>
    class n : Instruction
    {
        public n(List<PdfObject> _) { }
        public n() { }

        public override string Keyword => "n";
        public override IList<PdfObject> GetOperands() => null;
    }

    /// <summary>
    /// Modify the current clipping path by intersecting it with the current path, using the
    /// non-zero winding number rule to determine which regions lie inside the clipping
    /// path.
    /// </summary>
    class W : Instruction
    {
        public W(List<PdfObject> _) { }
        public W() { }

        public override string Keyword => "W";
        public override IList<PdfObject> GetOperands() => null;
    }

    /// <summary>
    /// Modify the current clipping path by intersecting it with the current path, using the
    /// even-odd rule to determine which regions lie inside the clipping path.
    /// </summary>
    class W_ : Instruction
    {
        public W_(List<PdfObject> _) { }
        public W_() { }

        public override string Keyword => "W*";
        public override IList<PdfObject> GetOperands() => null;
    }
}
