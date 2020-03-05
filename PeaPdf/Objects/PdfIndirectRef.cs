/*
 * Copyright 2020 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SeaPeaYou.PeaPdf
{
    class PdfIndirectReference : PdfObject, IEquatable<PdfIndirectReference>
    {
        public readonly int ObjectNum, GenerationNum;

        public PdfIndirectReference(int objectNum, int generationNum)
        {
            ObjectNum = objectNum;
            GenerationNum = generationNum;
        }

        public bool Equals(PdfIndirectReference other) =>
            other.ObjectNum == ObjectNum && other.GenerationNum == GenerationNum;

        public override bool Equals(object obj)
        {
            if (obj is PdfIndirectReference other)
                return Equals(other);
            return false;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = unchecked(hash * 31 + ObjectNum);
            hash = unchecked(hash * 31 + GenerationNum);
            return hash;
        }

        public override void Write(Stream stream, PDF pdf, PdfIndirectReference iRef)
        {
            var twoNums = new TwoNums(ObjectNum, GenerationNum, "R");
            twoNums.Write(stream, true);
        }

        public static PdfIndirectReference TryRead(FParse fParse)
        {
            var twoNums = TwoNums.TryRead(fParse, "R");
            if (twoNums == null)
                return null;

            return new PdfIndirectReference(twoNums.Num1, twoNums.Num2);
        }

    }
}
