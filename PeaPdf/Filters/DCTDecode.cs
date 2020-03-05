/*
 * Copyright 2020 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace SeaPeaYou.PeaPdf.Filters
{

    class DCTDecode
    {

        public static byte[] Decode(PdfDict decodeParms, byte[] bytes) => new DCTDecode(decodeParms, bytes).result;

        DCTDecode(PdfDict decodeParms, byte[] bytes)
        {
            noYUV = (int?)decodeParms?["ColorTransform"] == 0;

            var r = new ByteReader(bytes);
            //SOI
            if ((MarkerCode)r.ReadShort() != MarkerCode.SOI) throw new FormatException("no SOI");
            //Frame
            //SOF
            var sof = InterpretMarkers(r, false);
            if (sof != MarkerCode.SOF0) throw new NotImplementedException("non SOF0");
            var frameLen = r.ReadShort() - 2;
            //Frame header
            int P = r.ReadByte(), Y = r.ReadShort(), X = r.ReadShort(), Nf = r.ReadByte();
            if (Y == 0 || X == 0) throw new NotImplementedException(); //permitted, uses DNL
            var frameCSs = new FrameComponentSpec[Nf];
            var frameCSDict = new Dictionary<int, FrameComponentSpec>();
            int maxH = 0, maxV = 0;
            for (int i = 0; i < Nf; i++)
            {
                var cs = new FrameComponentSpec();
                cs.C = r.ReadByte();
                (cs.H, cs.V) = r.Read2Halfs();
                cs.Tq = r.ReadByte();
                frameCSs[i] = cs;
                frameCSDict[cs.C] = cs;
                if (maxH < cs.H) maxH = cs.H;
                if (maxV < cs.V) maxV = cs.V;
            }
            foreach (var cs in frameCSs)
            {
                cs.HFactor = maxH / cs.H;
                cs.VFactor = maxV / cs.V;
            }
            result = new byte[Y * X * Nf];

            //Scans
            while (!r.AtEnd) //loop scans
            {
                if (InterpretMarkers(r, true) == MarkerCode.EOI)
                    break;
                //Scan header
                var scanLen = r.ReadShort() - 2;
                var Ns = r.ReadByte();
                var scanCSs = new ScanComponentSpec[Ns];
                for (int i = 0; i < Ns; i++)
                {
                    var cs = new ScanComponentSpec();
                    cs.Cs = r.ReadByte();
                    (cs.Td, cs.Ta) = r.Read2Halfs();
                    var frameCS = frameCSDict[cs.Cs];
                    cs.Q = qTables[frameCS.Tq]; //this may have changed since the previous scan
                    cs.FrameCS = frameCS;
                    scanCSs[i] = cs;
                }
                int Ss = r.ReadByte(), Se = r.ReadByte();
                var (Ah, Al) = r.Read2Halfs();
                int bmpY = 0, bmpX = 0;
                while (true) //loop restart intervals
                {
                    var bitReader = new BitReader(r);
                    for (int i = 0; i < Ns; i++)
                        scanCSs[i].PRED = 0;
                    for (int mcuIX = 0; Ri == 0 || mcuIX < Ri; mcuIX++) //ECS (entropy coded segment)
                    {
                        for (int i = 0; i < Ns; i++) //loop components
                        {
                            var scanCS = scanCSs[i];
                            for (int regionY = 0; regionY < scanCS.FrameCS.V; regionY++)
                            {
                                for (int regionX = 0; regionX < scanCS.FrameCS.H; regionX++)
                                {
                                    var ZZ = new int[64];
                                    //decode dc
                                    var dcHuffmanTable = dcHuffmanTables[scanCS.Td];
                                    {
                                        var T = dcHuffmanTable.GetVal(bitReader);
                                        var DIFF = bitReader.ReadBits(T);
                                        DIFF = EXTEND(DIFF, T);
                                        ZZ[0] = scanCS.PRED + DIFF;
                                        scanCS.PRED = ZZ[0];
                                    }
                                    //decode ac
                                    var acHuffmanTable = acHuffmanTables[scanCS.Ta];
                                    int K = 1;
                                    while (true)
                                    {
                                        int RS = acHuffmanTable.GetVal(bitReader);
                                        int SSSS = RS % 16, RRRR = RS >> 4, R = RRRR;
                                        if (SSSS == 0)
                                        {
                                            if (R == 15)
                                            {
                                                K = K + 16;
                                                continue;
                                            }
                                            else
                                            {
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            K = K + R;
                                            ZZ[K] = EXTEND(bitReader.ReadBits(SSSS), SSSS);
                                            if (K == 63) { break; }
                                            else
                                            {
                                                K = K + 1;
                                                continue;
                                            }
                                        }
                                    }
                                    //dequantize
                                    var S = new int[64];
                                    for (int sIX = 0; sIX < ZZ.Length; sIX++)
                                    {
                                        var zzIX = s2zz[sIX];
                                        S[sIX] = ZZ[zzIX] * scanCS.Q[zzIX];
                                    }
                                    //IDCT
                                    var s = new byte[8, 8];
                                    for (int y = 0; y < 8; y++)
                                    {
                                        for (int x = 0; x < 8; x++)
                                        {
                                            var _dctCoefficients = dctCoefficients[y * 8 + x];
                                            double sVal = 0;
                                            for (int sIX = 0; sIX < 64; sIX++)
                                            {
                                                sVal += S[sIX] * _dctCoefficients[sIX];
                                            }
                                            sVal /= 4;
                                            sVal += 128;
                                            s[y, x] = (byte)Math.Min(255, Math.Max(0, Math.Round(sVal)));
                                        }
                                    }
                                    //copy to bmp
                                    int _bmpY = bmpY + regionY * 8 * scanCS.FrameCS.VFactor, _bmpX = bmpX + regionX * 8 * scanCS.FrameCS.HFactor;
                                    for (int y = 0; y < 8; y++)
                                    {
                                        var _y = y * scanCS.FrameCS.VFactor + _bmpY;
                                        for (int x = 0; x < 8; x++)
                                        {
                                            var _x = x * scanCS.FrameCS.HFactor + _bmpX;
                                            for (int vRep = 0; vRep < scanCS.FrameCS.VFactor; vRep++)
                                            {
                                                for (int hRep = 0; hRep < scanCS.FrameCS.HFactor; hRep++)
                                                {
                                                    int __y = _y + vRep, __x = _x + hRep;
                                                    if (__y < Y && __x < X)
                                                        result[__y * X * Nf + __x * Nf + i] = s[y, x];
                                                }
                                            }
                                        }
                                    }

                                    int EXTEND(int V, int T)
                                    {
                                        int Vt = 1 << (T - 1);
                                        if (V < Vt)
                                        {
                                            Vt = (-1 << T) + 1;
                                            V = V + Vt;
                                        }
                                        return V;
                                    }

                                }
                            }
                        }

                        bmpX += 8 * maxH;
                        if (bmpX >= X)
                        {
                            bmpX = 0;
                            bmpY += 8 * maxV;
                            if (bmpY >= Y)
                                break;
                        }
                    }
                    if (bmpY >= Y)
                        break;
                    var resetMarkerCode = (MarkerCode)r.ReadShort();
                    if (resetMarkerCode < MarkerCode.RST0 || resetMarkerCode > MarkerCode.RST7)
                        throw new Exception("invalid reset marker code");
                }
            }

            if (isYCCK)
            {
                for (int y = 0; y < Y; y++)
                {
                    for (int x = 0; x < X; x++)
                    {
                        int ix = y * X * Nf + x * Nf;
                        float _Y = result[ix + 0], _Cb = result[ix + 1], _Cr = result[ix + 2];
                        result[ix + 0] = toByte(_Y + 1.402f * _Cr - 179.456f);
                        result[ix + 1] = toByte(_Y - 0.34414f * _Cb - 0.71414f * _Cr + 135.45984f);
                        result[ix + 2] = toByte(_Y + 1.772f * _Cb - 226.816f);
                        static byte toByte(float _f) => (byte)(255 - Math.Min(255, Math.Max(0, (int)Math.Round(_f))));
                    }
                }
            }
            else if (Nf == 3 && !noYUV)
            {
                for (int y = 0; y < Y; y++)
                {
                    for (int x = 0; x < X; x++)
                    {
                        int ix = y * X * Nf + x * Nf;
                        float _Y = result[ix + 0], _Cb = result[ix + 1], _Cr = result[ix + 2];
                        result[ix + 0] = toByte(_Y + 1.402f * (_Cr - 128));
                        result[ix + 1] = toByte(_Y - 0.3441f * (_Cb - 128) - 0.7141f * (_Cr - 128));
                        result[ix + 2] = toByte(_Y + 1.772f * (_Cb - 128));
                        static byte toByte(float _f) => (byte)Math.Min(255, Math.Max(0, (int)Math.Round(_f)));
                    }
                }
            }

        }

        bool noYUV, isYCCK;
        byte[][] qTables = new byte[4][];
        HuffmanTable<byte>[] dcHuffmanTables = new HuffmanTable<byte>[4], acHuffmanTables = new HuffmanTable<byte>[4];
        int Ri;
        byte[] result;

        enum MarkerCode
        {
            SOF0 = 0xFFC0,
            SOF1 = 0xFFC1,
            SOF2 = 0xFFC2,
            SOF3 = 0xFFC3,
            SOF5 = 0xFFC5,
            SOF6 = 0xFFC6,
            SOF7 = 0xFFC7,
            JPG = 0xFFC8,
            SOF9 = 0xFFC9,
            SOF10 = 0xFFCA,
            SOF11 = 0xFFCB,
            SOF13 = 0xFFCD,
            SOF14 = 0xFFCE,
            SOF15 = 0xFFCF,
            DHT = 0xFFC4,
            DAC = 0xFFCC,
            RST0 = 0xFFD0,
            RST7 = 0xFFD7,
            SOI = 0xFFD8,
            EOI = 0xFFD9,
            SOS = 0xFFDA,
            DQT = 0xFFDB,
            DNL = 0xFFDC,
            DRI = 0xFFDD,
            DHP = 0xFFDE,
            EXP = 0xFFDF,
            APP0 = 0xFFE0,
            APP14 = 0xFFEE,
            APP15 = 0xFFEF,
            JPGmin = 0xFFF0,
            JPGmax = 0xFFFD,
            COM = 0xFFFE,
        }
        MarkerCode InterpretMarkers(ByteReader r, bool forScan)
        {
            while (true)
            {
                var markerCode = (MarkerCode)r.ReadShort();
                if (forScan)
                {
                    if (markerCode == MarkerCode.SOS || markerCode == MarkerCode.EOI)
                        return markerCode;
                }
                else
                {
                    if (markerCode >= MarkerCode.SOF0 && markerCode <= MarkerCode.SOF15)
                        return markerCode;
                }
                int len = r.ReadShort() - 2, startPos = r.Pos;
                switch (markerCode)
                {
                    case MarkerCode.DQT:
                        {
                            int remaining = len;
                            while (remaining > 0)
                            {
                                var (P, T) = r.Read2Halfs();
                                if (P > 0) throw new NotImplementedException("Q values only supported at 8 bit.");
                                var Q = new byte[64];
                                for (int i = 0; i < 64; i++)
                                {
                                    Q[i] = r.ReadByte();
                                }
                                qTables[T] = Q;
                                remaining -= 1 + 64 * (P + 1);
                            }
                            break;
                        }
                    case MarkerCode.DHT:
                        {
                            int remaining = len;
                            while (remaining > 0)
                            {
                                var (Tc, Th) = r.Read2Halfs();
                                var BITS = new byte[16];
                                int m = 0;
                                for (var i = 0; i < 16; i++)
                                {
                                    BITS[i] = r.ReadByte();
                                    m += BITS[i];
                                }
                                var HUFFVAL = new byte[m];
                                for (var i = 0; i < m; i++)
                                    HUFFVAL[i] = r.ReadByte();
                                //Generate_size_table
                                var HUFFSIZE = new int[m + 1];
                                {
                                    int K = 0, I = 1/*code-length - must subtract 1 to use in array*/, J = 1;
                                    while (true)
                                    {
                                        if (J > BITS[I - 1])
                                        {
                                            I = I + 1;
                                            J = 1;
                                            if (I > 16)
                                                break;
                                        }
                                        else
                                        {
                                            HUFFSIZE[K] = I;
                                            K = K + 1;
                                            J = J + 1;
                                        }
                                    }
                                    HUFFSIZE[K] = 0;
                                }
                                //Generate_code_table
                                var HUFFCODE = new HuffmanBitCode[m];
                                {
                                    int K = 0, CODE = 0;
                                    int SI = HUFFSIZE[0];
                                    while (true)
                                    {
                                        do
                                        {
                                            HUFFCODE[K] = new HuffmanBitCode(CODE, SI);
                                            CODE = CODE + 1;
                                            K = K + 1;
                                        } while (HUFFSIZE[K] == SI);
                                        if (HUFFSIZE[K] == 0)
                                            break;
                                        do
                                        {
                                            CODE <<= 1;
                                            SI = SI + 1;
                                        } while (HUFFSIZE[K] != SI);
                                    }
                                }
                                var t = new HuffmanTable<byte>(HUFFCODE, HUFFVAL);
                                (Tc == 0 ? dcHuffmanTables : acHuffmanTables)[Th] = new HuffmanTable<byte>(HUFFCODE, HUFFVAL);
                                remaining -= 17 + m;
                            }
                            break;
                        }
                    case MarkerCode.DAC:
                        throw new NotImplementedException();
                    case MarkerCode.DRI:
                        Ri = r.ReadShort();
                        break;
                    case MarkerCode.APP14:
                        r.SkipBytes(11); //identifier 'Adobe', Version, Flags0, Flags1
                        isYCCK = r.ReadByte() == 2;
                        r.SkipBytes(len - 12);
                        break;
                    default: //APPn,COM
                        if (markerCode != MarkerCode.COM && (markerCode < MarkerCode.APP0 || markerCode > MarkerCode.APP15))
                            throw new Exception("unrecognized marker code");
                        r.SkipBytes(len);
                        break;
                }
            }
        }

        static int[] s2zz = new[]
        {
            0,1,5,6,14,15,27,28,
            2,4,7,13,16,26,29,42,
            3,8,12,17,25,30,41,43,
            9,11,18,24,31,40,44,53,
            10,19,23,32,39,45,52,54,
            20,22,33,38,46,51,55,60,
            21,34,37,47,50,56,59,61,
            35,36,48,49,57,58,62,63
        };
        static double oneOverSqr2 = 0.7071067811865475;
        static double[][] dctCoefficients = new double[64][];
        static DCTDecode()
        {
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    var coeffs = new double[64];
                    for (int v = 0; v < 8; v++)
                    {
                        double Cv = v == 0 ? oneOverSqr2 : 1;
                        for (int u = 0; u < 8; u++)
                        {
                            double Cu = u == 0 ? oneOverSqr2 : 1;
                            coeffs[v * 8 + u] = Cu * Cv * Math.Cos((2 * x + 1) * u * Math.PI / 16) * Math.Cos((2 * y + 1) * v * Math.PI / 16);
                        }
                    }
                    dctCoefficients[y * 8 + x] = coeffs;
                }
            }
        }

        class FrameComponentSpec
        {
            public int C, H, V, Tq, HFactor, VFactor;
        }
        class ScanComponentSpec
        {
            public int Cs, Td, Ta;
            public int PRED;
            public byte[] Q;
            public FrameComponentSpec FrameCS;
        }

        class BitReader : PeaPdf.BitReader
        {
            public BitReader(ByteReader byteReader) : base(byteReader) { }

            protected override byte GetNextByte()
            {
                var b = base.GetNextByte();
                if (b == 0xFF)
                {
                    var b2 = base.GetNextByte();
                    if (b2 != 0) throw new Exception("unexpected byte in bit stream");
                }
                return b;
            }
        }

    }
}

