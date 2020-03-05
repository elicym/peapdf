/*
 * Copyright 2020 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SeaPeaYou.PeaPdf
{
    partial class ICCProfile
    {

        abstract class TagType { }
        abstract class CurveType : TagType
        {
            public abstract float Convert(float d);
        }
        abstract class ConversionType : TagType
        {
            public abstract void Convert(Span<float> input, Span<float> output);
        }

        class curveType : CurveType
        {
            float? gamma;
            float[] entries;
            int entriesUpper;

            public curveType(ByteReader r)
            {
                int entryC = r.ReadInt();
                if (entryC > 0)
                {
                    if (entryC == 1)
                    {
                        gamma = r.ReadByte();
                    }
                    else
                    {
                        entries = new float[entryC];
                        entriesUpper = entryC - 1;
                        for (var i = 0; i < entryC; i++)
                            entries[i] = r.ReadBytes(2) / ushortMax;
                    }
                }
            }

            public override float Convert(float d)
            {
                if (gamma != null)
                    return (float)Math.Pow(d, gamma.Value);
                if (entries != null)
                {
                    float pos = d * entriesUpper;
                    int posLeft = (int)Math.Floor(pos), posRight = (int)Math.Ceiling(pos);
                    if (posLeft == posRight)
                    {
                        return entries[posLeft];
                    }
                    else
                    {
                        float entryLeft = entries[posLeft], entryRight = entries[posRight], diff = entryRight - entryLeft, fraction = pos - posLeft;
                        return entryLeft + diff * fraction;
                    }
                }
                return d;
            }
        }

        class lutType : ConversionType
        {
            int inputChannelC, outputChannelC, clutGridPointC, clutGridUpper, inputTableEntryC, outputTableEntryC, inputTableUpper, outputTableUpper;
            float[] e = new float[9];
            bool isIdentityMatrix;
            float[,] inputTables, outputTables;
            float[] clut;

            public lutType(ByteReader r, bool precision16, bool? fromLab)
            {
                inputChannelC = r.ReadBytes(1);
                outputChannelC = r.ReadBytes(1);
                clutGridPointC = r.ReadBytes(1); clutGridUpper = clutGridPointC - 1;
                r.SkipBytes(1);
                for (var i = 0; i < 9; i++)
                    e[i] = r.Read_s15Fixed16();
                isIdentityMatrix = e.SequenceEqual(new float[] { 1, 0, 0, 0, 1, 0, 0, 0, 1 });
                if (precision16)
                {
                    inputTableEntryC = Math.Min(4096, r.ReadBytes(2)); //spec mistake: field length
                    outputTableEntryC = Math.Min(4096, r.ReadBytes(2));
                }
                else
                {
                    inputTableEntryC = outputTableEntryC = 256;
                }
                inputTableUpper = inputTableEntryC - 1;
                outputTableUpper = outputTableEntryC - 1;
                inputTables = new float[inputChannelC, inputTableEntryC];
                int bytesPerEntry = precision16 ? 2 : 1;
                float divisor = precision16 ? ushortMax : 255f;
                for (var i = 0; i < inputChannelC; i++)
                {
                    for (var j = 0; j < inputTableEntryC; j++)
                    {
                        //not clear from docs whether we should use the complex divisor that we use in CLUT or simple, but experience shows simple
                        inputTables[i, j] = r.ReadBytes(bytesPerEntry) / divisor;
                    }
                }
                var clutC = (int)Math.Pow(clutGridPointC, inputChannelC) * outputChannelC;
                clut = new float[clutC];
                for (var i = 0; i < clutC; i++)
                {
                    var _divisor = precision16 ? ((fromLab == false && (i % outputChannelC) == 0) ? ff00 : 65536f) : 255f;
                    clut[i] = r.ReadBytes(bytesPerEntry) / _divisor;
                }
                outputTables = new float[outputChannelC, outputTableEntryC];
                for (var i = 0; i < outputChannelC; i++)
                {
                    for (var j = 0; j < outputTableEntryC; j++)
                    {
                        //see comment by input tables
                        outputTables[i, j] = r.ReadBytes(bytesPerEntry) / divisor;
                    }
                }
            }

            public override void Convert(Span<float> input, Span<float> output)
            {
                //matrix
                if (!isIdentityMatrix)
                {
                    float x = e[0] * input[0] + e[1] * input[1] + e[2] * input[2],
                        y = e[3] * input[0] + e[4] * input[1] + e[5] * input[2],
                        z = e[6] * input[0] + e[7] * input[1] + e[8] * input[2];
                    input = new[] { x, y, z };
                }
                //input tables
                for (int i = 0; i < inputChannelC; i++)
                {
                    float pos = input[i] * inputTableUpper;
                    int posLeft = (int)Math.Floor(pos), posRight = (int)Math.Ceiling(pos);
                    if (posLeft == posRight)
                    {
                        input[i] = inputTables[i, posLeft];
                    }
                    else
                    {
                        float entryLeft = inputTables[i, posLeft], entryRight = inputTables[i, posRight], diff = entryRight - entryLeft, fraction = pos - posLeft;
                        input[i] = entryLeft + diff * fraction;
                    }
                }
                //clut //TODO interpolate
                int clutIX = 0;
                for (int i = 0; i < inputChannelC; i++)
                {
                    clutIX += (int)(Math.Floor(input[i] * clutGridUpper) * Math.Pow(clutGridPointC, inputChannelC - i - 1)) * outputChannelC;
                }
                for (var i = 0; i < outputChannelC; i++)
                    output[i] = clut[clutIX + i];
                //output tables
                for (int i = 0; i < outputChannelC; i++)
                {
                    float pos = output[i] * outputTableUpper;
                    int posLeft = (int)Math.Floor(pos), posRight = (int)Math.Ceiling(pos);
                    if (posLeft == posRight)
                    {
                        output[i] = outputTables[i, posLeft];
                    }
                    else
                    {
                        float entryLeft = outputTables[i, posLeft], entryRight = outputTables[i, posRight], diff = entryRight - entryLeft, fraction = pos - posLeft;
                        output[i] = entryLeft + diff * fraction;
                    }
                }
            }
        }

        class lutABToABType : ConversionType
        {
            bool bToA;
            int inputChannelC, outputChannelC;
            CurveType[] aCurves, bCurves, mCurves;
            float[] matrix;
            byte[] gridPointCs;
            float[] gridUppers;
            float[] clut;
            List<Action<float[]>> flow = new List<Action<float[]>>();

            public lutABToABType(ByteReader r, bool bToA)
            {
                this.bToA = bToA;
                var tagStart = r.Pos - 8;
                inputChannelC = r.ReadByte(); outputChannelC = r.ReadByte(); r.SkipBytes(2);
                int bCurveOffset = r.ReadInt(), matrixOffset = r.ReadInt(), mCurveOffset = r.ReadInt(), clutOffset = r.ReadInt(), aCurveOffset = r.ReadInt();
                if (bCurveOffset > 0)
                {
                    bCurves = getCurves(outputChannelC, bCurveOffset);
                }
                if (matrixOffset > 0)
                {
                    var _r = r.Clone(matrixOffset + tagStart);
                    matrix = new float[12];
                    for (var i = 0; i < 12; i++)
                        matrix[i] = _r.Read_s15Fixed16();
                    if (matrix.SequenceEqual(new float[] { 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0 }))
                        matrix = null; //ignore identity
                }
                if (mCurveOffset > 0)
                {
                    mCurves = getCurves(outputChannelC, mCurveOffset);
                }
                if (clutOffset > 0)
                {
                    var _r = r.Clone(clutOffset + tagStart);
                    gridPointCs = _r.ReadByteArray(16);
                    gridUppers = gridPointCs.Take(inputChannelC).Select(x => (float)x - 1).ToArray();
                    int bytesPerEntry = _r.ReadByte(); _r.SkipBytes(3);
                    float divisor = bytesPerEntry == 2 ? ushortMax : 255f;
                    int clutC = outputChannelC;
                    foreach (var gridPointC in gridPointCs.Take(inputChannelC))
                        clutC *= gridPointC;
                    clut = new float[clutC];
                    for (var i = 0; i < clutC; i++)
                        clut[i] = _r.ReadBytes(bytesPerEntry) / divisor;
                }
                if (aCurveOffset > 0)
                {
                    aCurves = getCurves(inputChannelC, aCurveOffset);
                }

                CurveType[] getCurves(int channelC, int offset)
                {
                    var _r = r.Clone(offset + tagStart);
                    var curves = new CurveType[channelC];
                    for (int i = 0; i < channelC; i++)
                    {
                        var typeSignature = _r.ReadInt();
                        _r.SkipBytes(4); //reserved
                        curves[i] = typeSignature switch
                        {
                            0x63757276 => new curveType(_r),
                            0x70617261 => new parametricCurveType(_r),
                            _ => throw new Exception("unexpected curve type")
                        };
                    }
                    return curves;
                }

                if (!bToA)
                {
                    if (aCurves != null)
                        flow.Add(v => UseCurves(aCurves, v));

                }

            }

            void UseCurves(CurveType[] curves, Span<float> vals)
            {
                for (var i = 0; i < curves.Length; i++)
                    vals[i] = Math.Max(0, Math.Min(1, curves[i].Convert(vals[i])));
            }
            void UseCLUT(Span<float> vals, Span<float> output)
            {
                //Need to go through all corners of unit where point is in, taking each corner proportionally.
                //A corner is either before the point, or after, 1 represents after.

                Span<int> dimsIsAfter = stackalloc int[inputChannelC];
                doDim(0, vals, output, dimsIsAfter);
            }
            void doDim(int dimIX, Span<float> vals, Span<float> output, Span<int> dimsIsAfter)
            {
                for (int i = 0; i < 2; i++)
                {
                    dimsIsAfter[dimIX] = i;
                    if (dimIX == inputChannelC - 1)
                    {
                        DoCLUTCorner(vals, output, dimsIsAfter);
                    }
                    else
                    {
                        doDim(dimIX + 1, vals, output, dimsIsAfter);
                    }
                }
            }
            void DoCLUTCorner(Span<float> vals, Span<float> output, Span<int> dimsIsAfter)
            {
                int clutIX = 0;
                int channelsSize = outputChannelC;
                float fraction = 1;
                for (int c = inputChannelC - 1; c >= 0; c--)
                {
                    float pos = vals[c] * gridUppers[c], posFloor = (float)Math.Floor(pos);
                    int posLeft = (int)posFloor;

                    if (pos == posFloor) //means we're on a boundary, only use left
                    {
                        if (dimsIsAfter[c] == 1)
                            return;
                    }
                    else
                    {
                        float diff = pos - posLeft;
                        fraction *= dimsIsAfter[c] == 1 ? diff : 1 - diff;
                    }

                    clutIX += (posLeft + dimsIsAfter[c]) * channelsSize;
                    channelsSize *= gridPointCs[c];
                }

                for (var c = 0; c < outputChannelC; c++)
                    output[c] += clut[clutIX + c] * fraction;
            }
            void UseMatrix(Span<float> vals)
            {
                float x = matrix[0] * vals[0] + matrix[1] * vals[1] + matrix[2] * vals[2] + matrix[9],
                    y = matrix[3] * vals[0] + matrix[4] * vals[1] + matrix[5] * vals[2] + matrix[10],
                    z = matrix[6] * vals[0] + matrix[7] * vals[1] + matrix[8] * vals[2] + matrix[11];
                vals[0] = x; vals[1] = y; vals[2] = z;
            }

            public override void Convert(Span<float> input, Span<float> output)
            {
                if (!bToA)
                {
                    if (aCurves != null)
                    {
                        UseCurves(aCurves, input);
                    }

                    if (clut != null)
                        UseCLUT(input, output);
                    else
                        input.CopyTo(output);
                    if (mCurves != null)
                    {
                        UseCurves(mCurves, output);
                    }
                    if (matrix != null && outputChannelC == 3)
                    {
                        UseMatrix(output);
                    }
                    if (bCurves != null)
                    {
                        UseCurves(bCurves, output);
                    }
                }
                else
                {
                    if (bCurves != null)
                    {
                        UseCurves(bCurves, input);
                    }
                    if (matrix != null && inputChannelC == 3)
                    {
                        UseMatrix(input);
                    }
                    if (mCurves != null)
                    {
                        UseCurves(mCurves, input);
                    }
                    if (clut != null)
                        UseCLUT(input, output);
                    else
                        input.CopyTo(output);
                    if (aCurves != null)
                    {
                        UseCurves(aCurves, output);
                    }
                }
            }

        }

        class chromaticityType : TagType
        {
            (float x, float y)[] xyCoords;
            public chromaticityType(ByteReader r)
            {
                int channelsC = r.ReadBytes(2);
                var phosphorColorant = (PhosphorColorant)r.ReadBytes(2);
                xyCoords = new (float, float)[channelsC];
                for (int i = 0; i < channelsC; i++)
                {
                    xyCoords[i] = (r.Read_u16Fixed16(), r.Read_u16Fixed16());
                }
            }
        }

        class colorantOrderType : TagType
        {
            int[] colorantIXs;
            public colorantOrderType(ByteReader r)
            {
                int colorantsC = r.ReadInt();
                colorantIXs = new int[colorantsC];
                for (int i = 0; i < colorantsC; i++)
                {
                    colorantIXs[i] = r.ReadBytes(1);
                }
            }
        }

        class multiLocalizedUnicodeType : TagType
        {
            Record[] records;
            public multiLocalizedUnicodeType(ByteReader r)
            {
                var tagStart = r.Pos - 8;
                int recordC = r.ReadInt();
                r.SkipBytes(4); //record size: always 12
                records = new Record[recordC];
                for (int i = 0; i < recordC; i++)
                {
                    var record = new Record
                    {
                        LanguageCode = r.ReadBytes(2),
                        CountryCode = r.ReadBytes(2),
                        Length = r.ReadInt(),
                        Offset = r.ReadInt()
                    };
                    records[i] = record;
                    var stringR = r.Clone(tagStart + record.Offset);
                    record.Text = Encoding.BigEndianUnicode.GetString(stringR.ReadByteArray(record.Length));
                }

            }

            class Record
            {
                public int LanguageCode, CountryCode, Length, Offset;
                public string Text;
            }
        }

        class parametricCurveType : CurveType
        {
            Formula formula;
            public parametricCurveType(ByteReader r)
            {
                int formulaType = r.ReadBytes(2); r.SkipBytes(2);
                formula = formulaType switch
                {
                    0 => new Formula0(r),
                    1 => new Formula1(r),
                    2 => new Formula2(r),
                    3 => new Formula3(r),
                    4 => new Formula4(r),
                    _ => throw new Exception("unknown formula type")
                };
            }

            public override float Convert(float x) => formula.Calc(x);

            abstract class Formula
            {
                public abstract float Calc(float X);
            }
            //Error in specs, as they say if X>=d use this formula, and if X>d use that formula.
            class Formula0 : Formula
            {
                float g;
                public Formula0(ByteReader r) => g = r.Read_s15Fixed16();
                public override float Calc(float X) => (float)Math.Pow(X, g);
            }
            class Formula1 : Formula
            {
                float g, a, b, boundary;
                public Formula1(ByteReader r)
                {
                    (g, a, b) = (r.Read_s15Fixed16(), r.Read_s15Fixed16(), r.Read_s15Fixed16());
                    boundary = -b / a;
                }
                public override float Calc(float X) => X >= boundary ? (float)Math.Pow(a * X + b, g) : 0;
            }
            class Formula2 : Formula
            {
                float g, a, b, c, boundary;
                public Formula2(ByteReader r)
                {
                    (g, a, b, c) = (r.Read_s15Fixed16(), r.Read_s15Fixed16(), r.Read_s15Fixed16(), r.Read_s15Fixed16());
                    boundary = -b / a;
                }
                public override float Calc(float X) => X >= boundary ? (float)Math.Pow(a * X + b, g) + c : c;
            }
            class Formula3 : Formula
            {
                float g, a, b, c, d, boundary;
                public Formula3(ByteReader r)
                {
                    (g, a, b, c, d) = (r.Read_s15Fixed16(), r.Read_s15Fixed16(), r.Read_s15Fixed16(), r.Read_s15Fixed16(), r.Read_s15Fixed16());
                    boundary = d;
                }
                public override float Calc(float X) => X >= boundary ? (float)Math.Pow(a * X + b, g) : (c * X);
            }
            class Formula4 : Formula
            {
                float g, a, b, c, d, e, f, boundary;
                public Formula4(ByteReader r)
                {
                    (g, a, b, c, d, e, f) = (r.Read_s15Fixed16(), r.Read_s15Fixed16(), r.Read_s15Fixed16(), r.Read_s15Fixed16(), r.Read_s15Fixed16(), r.Read_s15Fixed16(), r.Read_s15Fixed16());
                    boundary = d;
                }
                public override float Calc(float X) => X >= boundary ? (float)Math.Pow(a * X + b, g) + e : (c * X + f);
            }
        }

        class profileDescriptionType : TagType
        {
            public string Text;
            public profileDescriptionType(ByteReader r)
            {
                var size = r.ReadInt();
                Text = Encoding.ASCII.GetString(r.ReadByteArray(size - 1));
            }
        }

        class textType : TagType
        {
            public string Text;
            public textType(ByteReader r, int size)
            {
                Text = Encoding.ASCII.GetString(r.ReadByteArray(size - 1));
            }
        }

        class XYZType : TagType
        {
            XYZ[] XYZs;
            public XYZType(ByteReader r, int size)
            {
                XYZs = new XYZ[size / 12];
                for (var i = 0; i < XYZs.Length; i++)
                    XYZs[i] = r.ReadXYZ();
            }
        }

    }
}
