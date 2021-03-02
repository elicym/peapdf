/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace SeaPeaYou.PeaPdf.CS
{
    class MP : Instruction
    {
        public string tag;

        public MP(List<PdfObject> operands) => tag = operands[0].As<PdfName>().String;

        public override string Keyword => "MP";
        public override IList<PdfObject> GetOperands() => new[] { (PdfName)tag };
    }

    class DP : Instruction
    {
        public string tag;
        public PdfObject properties;

        public DP(List<PdfObject> operands)
        {
            tag = operands[0].As<PdfName>().String;
            properties = operands[1];
        }

        public override string Keyword => "DP";
        public override IList<PdfObject> GetOperands() => new[] { (PdfName)tag, properties };
    }

    class BMC : Instruction
    {
        public string tag;

        public BMC(List<PdfObject> operands) => tag = operands[0].As<PdfName>().String;
        public BMC(string tag) => this.tag = tag;

        public override string Keyword => "BMC";
        public override IList<PdfObject> GetOperands() => new[] { (PdfName)tag };
    }

    class BDC : Instruction
    {
        public string tag;
        public PdfObject properties;

        public BDC(List<PdfObject> operands)
        {
            tag = operands[0].As<PdfName>().String;
            properties = operands[1];
        }

        public override string Keyword => "BDC";
        public override IList<PdfObject> GetOperands() => new[] { (PdfName)tag, properties };
    }

    class EMC : Instruction
    {
        public EMC(List<PdfObject> _) { }
        public EMC() { }

        public override string Keyword => "EMC";
        public override IList<PdfObject> GetOperands() => null;
    }

}
