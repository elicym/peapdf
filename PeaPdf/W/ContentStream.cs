/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using SeaPeaYou.PeaPdf.CS;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ve = SeaPeaYou.PeaPdf.VisualElements;

namespace SeaPeaYou.PeaPdf.W
{
    class ContentStream
    {

        public readonly PdfStream PdfStream; //PdfObject if it's a stream, otherwise a new stream

        public readonly ResourceDictionary Resources;

        internal readonly List<Instruction> Instructions;

        ///<summary>Set resources to null, to use the Resources entry on the stream.</summary>
        public ContentStream(PdfObject obj, ResourceDictionary resources)
        {

            if (obj == null)
            {
                Instructions = new List<Instruction>();
                PdfStream = new PdfStream();
                Resources = resources ?? new ResourceDictionary();
            }
            else
            {
                byte[] bytes;
                if (obj is PdfStream stream)
                {
                    PdfStream = stream;
                    bytes = stream.GetDecodedBytes();
                    Resources = resources ?? stream.Dict["Resources"].To(x => new ResourceDictionary((PdfDict)x), () => new ResourceDictionary());
                }
                else
                {
                    var arr = (PdfArray)obj;
                    var streams = arr.AsArray<PdfStream>();
                    Resources = resources;
                    if (Resources == null)
                    {
                        Resources = new ResourceDictionary();
                        foreach (var _stream in streams)
                        {
                            if (_stream.Dict["Resources"] is PdfDict _resources)
                                Resources.ConcatWith(_resources);
                        }
                    }
                    var byteArrList = streams.Select(x => x.GetDecodedBytes()).ToList();
                    var byteLen = byteArrList.Sum(x => x.Length) + byteArrList.Count;
                    bytes = new byte[byteLen];
                    var bytesIX = 0;
                    foreach (var byteArr in byteArrList)
                    {
                        Array.Copy(byteArr, 0, bytes, bytesIX, byteArr.Length);
                        bytesIX += byteArr.Length;
                        bytes[bytesIX++] = (int)' ';
                    }
                    PdfStream = new PdfStream();
                }
                Instructions = ParseInstructions(bytes);
            }
        }

        public ContentStream() : this(null, null)
        {
        }

        internal void UpdateObjects()
        {
            PdfStream.SetDecodedBytes(GetInstructionBytes(Instructions));
        }

        public void PrepareForVisualElements(Page page)
        {
            var qCount = 0;
            SKMatrix matrix = SKMatrix.CreateIdentity();
            foreach (var inst in Instructions)
            {
                if (inst is q) qCount++;
                else if (inst is Q) qCount--;
                else if (inst is cm cm && qCount == 0)
                {
                    matrix = matrix.PreConcat(cm.Matrix);
                }
            }
            for (int i = 0; i < qCount; i++)
            {
                Instructions.Add(new Q());
            }
            var isIdentity = matrix.ScaleX == 1 && matrix.ScaleY == 1 && matrix.SkewX == 0 && matrix.SkewY == 0 && matrix.TransX == 0 && matrix.TransY == 0;
            if (!isIdentity)
            {
                if (!matrix.TryInvert(out var inverse))
                    throw new Exception("Could not invert the page's matrix.");
                Instructions.Add(new cm(inverse));
            }
            Instructions.Add(new cm(1, 0, 0, -1, 0, page.MediaBox.UpperRightY));
        }

        internal static List<Instruction> ParseInstructions(byte[] bytes)
        {
            var instructions = new List<Instruction>();
            var r = new PdfReader(bytes);
            var operands = new List<PdfObject>();
            while (true)
            {
                r.SkipWhiteSpace();
                if (r.AtEnd)
                    break;
                var b = r.PeekByte;
                var pdfBool = PdfBool.TryRead(r);
                if (pdfBool != null)
                {
                    operands.Add(pdfBool);
                }
                else if ((b >= 'a' && b <= 'z') || (b >= 'A' && b <= 'Z') || b == '\'' || b == '"')
                {
                    var keyword = r.ReadStringUntilDelimiter();
                    Instruction instruction = GetInstruction(keyword, operands);
                    instructions.Add(instruction);
                    operands.Clear();
                    if (instruction is BI bi)
                    {
                        bi.Read(r);
                    }
                }
                else
                {
                    operands.Add(r.ReadPdfObject(null));
                }
            }
            return instructions;
        }

        internal static byte[] GetInstructionBytes(List<Instruction> instructions)
        {
            var w = new PdfWriter();
            foreach (var instruction in instructions)
            {
                var operands = instruction.GetOperands();
                if (operands != null)
                {
                    foreach (var o in operands)
                    {
                        w.WriteObj(o, null, true);
                    }
                }
                w.EnsureDeliminated();
                w.WriteString(instruction.Keyword);
                w.WriteNewLine();
            }
            return w.ToArray();
        }

        static Instruction GetInstruction(string keyword, List<PdfObject> operands)
        {
            switch (keyword)
            {
                case "w": return new w(operands);
                case "J": return new J(operands);
                case "j": return new j(operands);
                case "M": return new M(operands);
                case "d": return new d(operands);
                case "ri": return new ri(operands);
                case "i": return new i(operands);
                case "gs": return new gs(operands);
                case "q": return new q(operands);
                case "Q": return new Q(operands);
                case "cm": return new cm(operands);
                case "m": return new m(operands);
                case "l": return new l(operands);
                case "c": return new c(operands);
                case "v": return new v(operands);
                case "y": return new y(operands);
                case "h": return new h(operands);
                case "re": return new re(operands);
                case "S": return new S(operands);
                case "s": return new s(operands);
                case "f": return new f(operands);
                case "F": return new F(operands);
                case "f*": return new f_(operands);
                case "B": return new B(operands);
                case "B*": return new B_(operands);
                case "b": return new b(operands);
                case "b*": return new b_(operands);
                case "n": return new n(operands);
                case "W": return new CS.W(operands);
                case "W*": return new W_(operands);
                case "BT": return new BT(operands);
                case "ET": return new ET(operands);
                case "Tc": return new Tc(operands);
                case "Tw": return new Tw(operands);
                case "Tz": return new Tz(operands);
                case "TL": return new TL(operands);
                case "Tf": return new Tf(operands);
                case "Tr": return new Tr(operands);
                case "Ts": return new Ts(operands);
                case "Td": return new Td(operands);
                case "TD": return new TD(operands);
                case "Tm": return new Tm(operands);
                case "T*": return new T_(operands);
                case "Tj": return new Tj(operands);
                case "TJ": return new TJ(operands);
                case "'": return new Apostrophe(operands);
                case "\"": return new Quote(operands);
                case "d0": return new d0(operands);
                case "d1": return new d1(operands);
                case "CS": return new CS.CS(operands);
                case "cs": return new cs(operands);
                case "SC": return new SC(operands);
                case "SCN": return new SC(operands);
                case "sc": return new sc(operands);
                case "scn": return new sc(operands);
                case "G": return new G(operands);
                case "g": return new g(operands);
                case "RG": return new RG(operands);
                case "rg": return new rg(operands);
                case "K": return new K(operands);
                case "k": return new k(operands);
                case "sh": return new sh(operands);
                case "BI": return new BI(operands);
                case "Do": return new Do(operands);
                case "MP": return new MP(operands);
                case "DP": return new DP(operands);
                case "BMC": return new BMC(operands);
                case "BDC": return new BDC(operands);
                case "EMC": return new EMC(operands);
                case "BX": return new BX(operands);
                case "EX": return new EX(operands);
                default: throw new NotImplementedException(keyword);
            }
        }


    }
}
