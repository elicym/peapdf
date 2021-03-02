/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace SeaPeaYou.PeaPdf.VisualElements
{
    public abstract class VisualElement
    {
        public Bounds Bounds;
        internal abstract DrawInfo PrepareToDraw(float maxX, W.ResourceDictionary resources);
    }

    class DrawInfo
    {
        public List<CS.Instruction> Instructions = new List<CS.Instruction>();
        public float Right, Bottom;
    }

    public enum Alignment { Left, Center, Right }
    public enum VAlignment { Top, Center, Bottom }

    public class Bounds
    {
        public float? Left, Top, Right, Bottom;

        public Bounds() { }

        public Bounds(float left, float top, float right, float bottom)
        {
            Left = left; Top = top; Right = right; Bottom = bottom;
        }
    }

}
