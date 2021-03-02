/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SeaPeaYou.PeaPdf.CS
{
    class Other : Instruction
    {
        private readonly string keyword;
        private readonly PdfObject[] operands;

        public Other(string keyword, IList<PdfObject> operands)
        {
            this.keyword = keyword;
            this.operands = operands.ToArray();
        }

        public override string Keyword => keyword;
        public override IList<PdfObject> GetOperands() => operands;
    }
}
