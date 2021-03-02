/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SeaPeaYou.PeaPdf
{
    static class Native
    {

        public static unsafe byte[] CMYK2RGB(byte[] input)
        {
            SetupCMYK();
            var output = new byte[input.Length];
            fixed (byte* inputPtr = input)
            fixed (byte* outputPtr = output)
            {
                _cmyk2RGB((IntPtr)inputPtr, input.Length, (IntPtr)outputPtr);
            }
            return output;
        }

        public static SKColor CMYK2RGB_Single(float[] input)
        {
            SetupCMYK();
            byte r = 0, g = 0, b = 0;
            _cmyk2RGB_Single(toByte(input[0]), toByte(input[1]), toByte(input[2]), toByte(input[3]), ref r, ref g, ref b);
            return new SKColor(r, g, b);
            byte toByte(float f) => (byte)Math.Min(255, Math.Max(0, (int)(f * 255)));
        }

        static bool isCMYK_Setup;
        static unsafe void SetupCMYK()
        {
            if (isCMYK_Setup) return;
            fixed (byte* profilePtr = Properties.Resources.USWebUncoated)
            {
                _cmykSetup((IntPtr)profilePtr, Properties.Resources.USWebUncoated.Length);
            }
            isCMYK_Setup = true;
        }

        public static unsafe byte[] DecodeDCT(byte[] input, int pixelC)
        {
            SetupCMYK();
            var output = new byte[pixelC * 4];
            fixed (byte* inputPtr = input)
            fixed (byte* outputPtr = output)
            {
                _decodeDCT((IntPtr)inputPtr, input.Length, (IntPtr)outputPtr);
            }
            return output;
        }

        public static unsafe byte[] DecodeJPX(byte[] input, int pixelC)
        {
            var output = new byte[pixelC * 4];
            fixed (byte* inputPtr = input)
            fixed (byte* outputPtr = output)
            {
                _decodeJPX((IntPtr)inputPtr, input.Length, (IntPtr)outputPtr);
            }
            return output;
        }

        static Action<IntPtr, int> _cmykSetup;
        static Action<IntPtr,int,IntPtr> _cmyk2RGB;
        delegate void CMYK2RGB_Single_Delegate(byte c, byte m, byte y, byte k, ref byte r, ref byte g, ref byte b);
        static CMYK2RGB_Single_Delegate _cmyk2RGB_Single;
        static Action<IntPtr, int, IntPtr> _decodeDCT;
        static Action<IntPtr, int, IntPtr> _decodeJPX;

        static Native()
        {
            if (IntPtr.Size == 4)
            {
                _cmykSetup = x86.CMYK_Setup;
                _cmyk2RGB = x86.CMYK2RGB;
                _cmyk2RGB_Single = x86.CMYK2RGB_Single;
                _decodeDCT = x86.DecodeDCT;
                _decodeJPX = x86.DecodeJPX;
            }
            else
            {
                _cmykSetup = x64.CMYK_Setup;
                _cmyk2RGB = x64.CMYK2RGB;
                _cmyk2RGB_Single = x64.CMYK2RGB_Single;
                _decodeDCT = x64.DecodeDCT;
                _decodeJPX = x64.DecodeJPX;
            }
        }

        static class x86
        {
            [DllImport(@"runtimes\win-x86\native\PeaPdfNative.dll")]
            public static extern void CMYK_Setup(IntPtr profileBytes, int profileBytesC);

            [DllImport(@"runtimes\win-x86\native\PeaPdfNative.dll")]
            public static extern void CMYK2RGB(IntPtr input, int pixelC, IntPtr output);

            [DllImport(@"runtimes\win-x86\native\PeaPdfNative.dll")]
            public static extern void CMYK2RGB_Single(byte c, byte m, byte y, byte k, ref byte r, ref byte g, ref byte b);

            [DllImport(@"runtimes\win-x86\native\PeaPdfNative.dll")]
            public static extern void DecodeDCT(IntPtr input, int inputC, IntPtr output);

            [DllImport(@"runtimes\win-x86\native\PeaPdfNative.dll")]
            public static extern void DecodeJPX(IntPtr input, int inputC, IntPtr output);
        }

        static class x64
        {
            [DllImport(@"runtimes\win-x64\native\PeaPdfNative.dll")]
            public static extern void CMYK_Setup(IntPtr profileBytes, int profileBytesC);

            [DllImport(@"runtimes\win-x64\native\PeaPdfNative.dll")]
            public static extern void CMYK2RGB(IntPtr input, int pixelC, IntPtr output);

            [DllImport(@"runtimes\win-x64\native\PeaPdfNative.dll")]
            public static extern void CMYK2RGB_Single(byte c, byte m, byte y, byte k, ref byte r, ref byte g, ref byte b);

            [DllImport(@"runtimes\win-x64\native\PeaPdfNative.dll")]
            public static extern void DecodeDCT(IntPtr input, int inputC, IntPtr output);

            [DllImport(@"runtimes\win-x64\native\PeaPdfNative.dll")]
            public static extern void DecodeJPX(IntPtr input, int inputC, IntPtr output);

        }

    }
}
