/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SeaPeaYou.PeaPdf
{

    public enum StandardFont { Times, Helvetica, Courier, Symbol }

    class Font
    {
        public SKTypeface Typeface;
        public PdfDict Type3Font;
        public CharEncoding Encoding;
        public Dictionary<byte, string> Code2Names;
        public bool Type0;
        public List<float> Widths;
        public int? FirstChar;
        public byte[] CIDToGID;
        public List<int> CodeMap;
        public int GlyphHeight;

        public Font(PdfDict fontPdfDict)
        {
            Encoding = CharEncoding.StdEncoding;

            var subtype = fontPdfDict["Subtype"].ToString();
            var _fontObj = subtype == "Type0" ? fontPdfDict["DescendantFonts"].As<PdfArray>()[0].As<PdfDict>() : fontPdfDict;

            var encodingObj = fontPdfDict["Encoding"];
            if (encodingObj != null)
            {
                if (encodingObj is PdfName encodingName)
                {
                    Encoding = CharEncoding.FromName(encodingName.String);
                }
                else if (encodingObj is PdfDict encodingDict)
                {
                    var baseEncoding = (PdfName)encodingDict["BaseEncoding"];
                    if (baseEncoding != null)
                        Encoding = CharEncoding.FromName(baseEncoding.String);
                    var differences = (PdfArray)encodingDict["Differences"];
                    if (differences != null)
                    {
                        Code2Names = new Dictionary<byte, string>();
                        int code = 0;
                        foreach (var item in differences)
                        {
                            if (item is PdfNumeric)
                                code = (int)item;
                            else
                            {
                                Code2Names.Add((byte)code, item.As<PdfName>().String);
                                code++;
                            }
                        }
                    }
                }
            }

            var widthsObj = (PdfArray)fontPdfDict["Widths"];
            if (widthsObj != null)
            {
                var divisor = subtype == "Type3" ? 1 : 1000;
                Widths = widthsObj.Select(x => (float)x / divisor).ToList();
            }
            FirstChar = (int?)fontPdfDict["FirstChar"];
            var fontDescriptor = _fontObj["FontDescriptor"]?.As<PdfDict>();

            if (subtype == "Type3")
            {
                Type3Font = fontPdfDict;
            }
            else if (Typeface == null)
            {
                if (fontDescriptor != null)
                {
                    OTFFont otfFont = null;
                    var fontFile = (PdfStream)fontDescriptor["FontFile2"];
                    if (fontFile != null)
                    {
                        var fontBytes = fontFile.GetDecodedBytes();
                        otfFont = OTFFont.FromTT(fontBytes);
                        if (subtype == "Type0")
                            otfFont.AddIdentityCMap();
                    }
                    else
                    {
                        fontFile = (PdfStream)fontDescriptor["FontFile3"];
                        if (fontFile != null)
                        {
                            var fontBytes = fontFile.GetDecodedBytes();
                            otfFont = OTFFont.FromCFF(fontBytes, Encoding, Code2Names);
                            Code2Names = null;
                            Encoding = null; //since we created the cmap
                        }
                    }
                    if (otfFont != null)
                    {

                        if (subtype == "Type0")
                        {
                            Encoding = null;
                            Type0 = true;
                            var cid2GIDObj = _fontObj["CIDToGIDMap"] as PdfStream;
                            if (cid2GIDObj != null)
                            {
                                CIDToGID = cid2GIDObj.GetDecodedBytes();
                            }
                            else
                            {
                                var cidSetObj = _fontObj["CIDSet"];
                                if (cidSetObj != null)
                                {
                                    CodeMap = new List<int>();
                                    var cidSetBytes = cidSetObj.As<PdfStream>().GetDecodedBytes();
                                    var bitStream = new BitReader(new ByteReader(cidSetBytes));
                                    for (int i = 0; i < cidSetBytes.Length * 8; i++)
                                    {
                                        if (bitStream.ReadBit())
                                            CodeMap.Add(i);
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (otfFont.NoUnicodeCMap)
                            {
                                Encoding = null;
                            }
                        }
                        MemoryStream ms = new MemoryStream();
                        otfFont.Write(ms);
                        ms.Seek(0, SeekOrigin.Begin);
                        Typeface = SKTypeface.FromStream(ms);
                    }
                }

            }
            var baseFont = _fontObj["BaseFont"]?.ToString();
            if (Typeface == null)
            {
                if (baseFont != null)
                {
                    Typeface = FromStandard14(baseFont);
                    if (baseFont == "Symbol")
                        Encoding = null;
                }
            }
            if (Typeface == null && fontDescriptor != null)
            {
                var fontFlags = (FontDescriptorFlags)(int)fontDescriptor["Flags"];
                string fontFamily = ((fontFlags & FontDescriptorFlags.Serif) > 0 || (baseFont != null && baseFont.StartsWith("Times"))) ? "Times New Roman" : "Arial";
                var isItalic = (fontFlags & FontDescriptorFlags.Italic) > 0;
                var isBold = (int?)fontDescriptor["FontWeight"] >= 700;
                var style = (!isItalic && !isBold) ? SKFontStyle.Normal : (isItalic && isBold ? SKFontStyle.BoldItalic : (isItalic ? SKFontStyle.Italic : SKFontStyle.Bold));
                Typeface = SKTypeface.FromFamilyName(fontFamily, style);
            }
        }

        public static SKTypeface FromStandard14(string fontName)
        {
            switch (fontName)
            {
                case "Helvetica": return getHelvetica(SKFontStyle.Normal);
                case "Helvetica-Bold": return getHelvetica(SKFontStyle.Bold);
                case "Helvetica-Oblique": return getHelvetica(SKFontStyle.Italic);
                case "Helvetica-BoldOblique": return getHelvetica(SKFontStyle.BoldItalic);
                case "Times-Roman": return SKTypeface.FromFamilyName("Times New Roman", SKFontStyle.Normal);
                case "Times-Bold": return SKTypeface.FromFamilyName("Times New Roman", SKFontStyle.Bold);
                case "Times-Italic": return SKTypeface.FromFamilyName("Times New Roman", SKFontStyle.Italic);
                case "Times-BoldItalic": return SKTypeface.FromFamilyName("Times New Roman", SKFontStyle.BoldItalic);
                case "Courier": return SKTypeface.FromFamilyName("Courier New", SKFontStyle.Normal);
                case "Courier-Bold": return SKTypeface.FromFamilyName("Courier New", SKFontStyle.Bold);
                case "Courier-Oblique": return SKTypeface.FromFamilyName("Courier New", SKFontStyle.Italic);
                case "Courier-BoldOblique": return SKTypeface.FromFamilyName("Courier New", SKFontStyle.BoldItalic);
                case "Symbol": return SKTypeface.FromFamilyName("Symbol");
                default: return null;
            }
            SKTypeface getHelvetica(SKFontStyle style)
            {
                var typeface = SKTypeface.FromFamilyName("Helvetica", style);
                if (typeface.FamilyName != "Helvetica")
                    typeface = SKTypeface.FromFamilyName("Arial", style);
                return typeface;
            }
        }

        enum FontDescriptorFlags { FixedPitch = 0b1, Serif = 0b10, Symbolic = 0b100, Script = 0b1000, NonSymbolic = 0b10_0000, Italic = 0b100_0000, AllCap = 0x10000, SmallCap = 0x20000, ForceBold = 0x40000 };

    }

    static class BuiltInFonts
    {
        public static PdfDict Helvetica() => new PdfDict() {
            { "Type", (PdfName)"Font" },
            { "Subtype", (PdfName)"Type1" },
            { "BaseFont", (PdfName)"Helvetica" }
        };
        public static PdfDict TimesRoman() => new PdfDict() {
            { "Type", (PdfName)"Font" },
            { "Subtype", (PdfName)"Type1" },
            { "BaseFont", (PdfName)"Times-Roman" }
        };
        public static PdfDict Courier() => new PdfDict() {
            { "Type", (PdfName)"Font" },
            { "Subtype", (PdfName)"Type1" },
            { "BaseFont", (PdfName)"Courier" }
        };
    }

}
