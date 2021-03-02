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

namespace SeaPeaYou.PeaPdf.VisualElements
{


    public class Paragraph : VisualElement
    {
        public Alignment Alignment;
        public List<TextRun> TextRuns = new List<TextRun>();
        public bool BreakWord;

        public Paragraph() { }
        public Paragraph(TextRun textRun) { TextRuns.Add(textRun); }

        internal override DrawInfo PrepareToDraw(float maxX, W.ResourceDictionary resources)
        {
            var lines = new List<Line>();
            var curLine = new Line();
            var bounds = Bounds ?? new Bounds();
            bounds.Top ??= 0;
            bounds.Left ??= 0;
            bounds.Right ??= maxX;

            var totalWidth = bounds.Right.Value - bounds.Left.Value;

            foreach (var textRun in TextRuns)
            {
                var fontName = (textRun.Font, textRun.Bold, textRun.Italic) switch
                {
                    (StandardFont.Times, false, false) => "Times-Roman",
                    (StandardFont.Helvetica, false, false) => "Helvetica",
                    (StandardFont.Courier, false, false) => "Courier",
                    (StandardFont.Symbol, false, false) => "Symbol",
                    (StandardFont.Times, true, false) => "Times-Bold",
                    (StandardFont.Helvetica, true, false) => "Helvetica-Bold",
                    (StandardFont.Courier, true, false) => "Courier-Bold",
                    (StandardFont.Times, false, true) => "Times-Italic",
                    (StandardFont.Helvetica, false, true) => "Helvetica-Oblique",
                    (StandardFont.Courier, false, true) => "Courier-Oblique",
                    (StandardFont.Times, true, true) => "Times-BoldItalic",
                    (StandardFont.Helvetica, true, true) => "Helvetica-BoldOblique",
                    (StandardFont.Courier, true, true) => "Courier-BoldOblique",
                    _ => throw new Exception("Font not supported.")
                };
                resources.Font ??= new PdfDict();
                if (resources.Font[fontName] == null)
                {
                    var dict = new PdfDict { Type = "Font" };
                    dict.Add("Subtype", (PdfName)"Type1");
                    dict.Add("BaseFont", (PdfName)fontName);
                    resources.Font[fontName] = dict;
                }
                var paint = new SKPaint { Typeface = Font.FromStandard14(fontName), TextSize = textRun.FontSize * 64 };
                float lineHeight = paint.GetFontMetrics(out var fm) / 64, shiftUp = (lineHeight - fm.CapHeight / 64) / 2;
                int drawn = 0;
                while (drawn < textRun.Text.Length)
                {
                    string remainingText = textRun.Text.Substring(drawn);
                    int newLineIX = remainingText.IndexOf('\n');
                    if (newLineIX == 0)
                    {
                        if (curLine.Height < lineHeight)
                            curLine.Height = lineHeight;
                        if (curLine.ShiftUp < shiftUp)
                            curLine.ShiftUp = shiftUp;
                        lines.Add(curLine);
                        curLine = new Line();
                        drawn++;
                        continue;
                    }
                    if (newLineIX > 0)
                    {
                        remainingText = remainingText.Substring(0, newLineIX);
                    }
                    var glyphWidths = paint.GetGlyphWidths(remainingText).Select(x => x / 64).ToList();

                    float totalGlyphsWidth = default;
                    int drawC = remainingText.Length;
                    for (; drawC > 0; drawC--)
                    {
                        totalGlyphsWidth = glyphWidths.Take(drawC).Sum();
                        if (curLine.Width + totalGlyphsWidth <= totalWidth)
                            break;
                    }
                    if (drawC > 0)
                    {
                        //if in middle of a word, and we shouldn't break words, don't
                        if (!BreakWord && drawC < remainingText.Length && remainingText[drawC - 1] != ' ' && remainingText[drawC] != ' ')
                        {
                            var newDrawC = drawC;
                            do
                            {
                                newDrawC--;
                            } while (newDrawC > 0 && remainingText[newDrawC - 1] != ' ');
                            if (newDrawC > 0 || curLine.Runs.Count > 0)
                                drawC = newDrawC;
                        }
                        //if we are breaking the run, trim end
                        int skipStart = 0, skipEnd = 0;
                        if (drawC < remainingText.Length)
                        {
                            for (var i = drawC - 1; i >= 0 && remainingText[i] == ' '; i--)
                                skipEnd++;
                        }
                        //if we are continuing a broken run, trim start
                        if (drawn > 0)
                        {
                            for (var i = 0; i < drawC && remainingText[i] == ' '; i++)
                                skipStart++;
                        }

                        var toDraw = remainingText.Substring(skipStart, drawC - skipEnd - skipStart);
                        if (toDraw.Length > 0)
                        {
                            curLine.Runs.Add(new LineRun { FontName = fontName, FontSize = textRun.FontSize, Text = toDraw });
                            curLine.Width += glyphWidths.Skip(skipStart).Take(toDraw.Length).Sum();
                            if (curLine.Height < lineHeight)
                                curLine.Height = lineHeight;
                            if (curLine.ShiftUp < shiftUp)
                                curLine.ShiftUp = shiftUp;
                        }
                        drawn += drawC;
                    }
                    if (drawC < remainingText.Length)
                    {
                        if (curLine.Runs.Count == 0)
                            break;
                        lines.Add(curLine);
                        curLine = new Line();
                    }
                }
            }
            if (curLine.Runs.Count > 0)
                lines.Add(curLine);
            var drawInfo = new DrawInfo();
            if (lines.Count == 0)
                return drawInfo;

            drawInfo.Instructions.Add(new q());
            drawInfo.Instructions.Add(new cm(1, 0, 0, 1, bounds.Left.Value, bounds.Top.Value));
            drawInfo.Instructions.Add(new cm(1, 0, 0, -1, 0, 0));
            drawInfo.Instructions.Add(new BT());
            float lineStart = 0, maxWidth = 0, height = 0, prevShiftUp = 0;
            foreach (var line in lines)
            {
                float x = Alignment == Alignment.Left ? 0 : (Alignment == Alignment.Right ? (totalWidth - line.Width) : ((totalWidth - line.Width) / 2));
                drawInfo.Instructions.Add(new Td(x - lineStart, -(prevShiftUp + line.Height - line.ShiftUp)));
                lineStart = x;
                foreach (var run in line.Runs)
                {
                    drawInfo.Instructions.Add(new Tf(run.FontName, run.FontSize));
                    drawInfo.Instructions.Add(new Tj((PdfString)run.Text));
                }
                if (line.Width > maxWidth) maxWidth = line.Width;
                height += line.Height;
                prevShiftUp = line.ShiftUp;
            }
            drawInfo.Instructions.Add(new ET());
            drawInfo.Instructions.Add(new Q());
            drawInfo.Right = bounds.Right.Value;
            drawInfo.Bottom = height + bounds.Top.Value;
            return drawInfo;
        }

        class Line
        {
            public float Width, Height, ShiftUp;
            public List<LineRun> Runs = new List<LineRun>();
        }

        class LineRun
        {
            public string FontName;
            public float FontSize;
            public string Text;
        }

    }
}
