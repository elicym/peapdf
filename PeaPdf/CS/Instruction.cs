/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace SeaPeaYou.PeaPdf.CS
{
    abstract class Instruction
    {
        public abstract string Keyword { get; }
        public abstract IList<PdfObject> GetOperands();
    }
}
