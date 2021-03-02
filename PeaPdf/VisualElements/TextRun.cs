/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace SeaPeaYou.PeaPdf.VisualElements
{
    public class TextRun
    {
        public string Text;
        public StandardFont Font;
        public float FontSize;
        public bool Bold;
        public bool Italic;

        public TextRun(string text, StandardFont font, float fontSize, bool bold = false, bool italic = false)
        {
            Text = text; Font = font; FontSize = fontSize; Bold = bold; Italic = italic;
        }
    }
}
