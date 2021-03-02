/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SKColor = SkiaSharp.SKColor;

namespace SeaPeaYou.PeaPdf.VisualElements
{
    public class Table : VisualElement
    {
        public (float width, SKColor color) TopBorder, RightBorder, BottomBorder, LeftBorder, InnerVerticalLines, InnerHorizontalLines;
        public readonly List<TableColumn> Columns = new List<TableColumn>();
        public readonly List<TableCell> Cells = new List<TableCell>();

        internal override DrawInfo PrepareToDraw(float maxX, W.ResourceDictionary resources)
        {
            if (Columns.Count == 0) throw new Exception("Missing columns.");
            if (Cells.Count == 0) throw new Exception("Missing cells.");
            if (Cells.Any(x => x.RowSpan == 0 || x.ColSpan == 0)) throw new Exception("Cannot have a span of 0.");
            if (Cells.Any(x => x.Col + (x.RowSpan ?? 1) - 1 >= Columns.Count)) throw new Exception("Cell Col doesn't exist.");

            var colWidths = new float[Columns.Count];
            var bounds = Bounds ?? new Bounds();
            bounds.Top ??= 0;
            bounds.Left ??= 0;
            bounds.Right ??= maxX;

            //col widths
            var widthRoom = Bounds.Right.Value - Bounds.Left.Value - LeftBorder.width - RightBorder.width
                - InnerVerticalLines.width * (Columns.Count - 1);
            var fixedColWidth = Columns.Where(x => !x.RelativeWidths).Sum(x => x.WidthAmount);
            if (fixedColWidth > widthRoom)
                throw new Exception("Not enough width for columns.");
            var flexWidthRoom = widthRoom - fixedColWidth;
            var totalFlex = Columns.Where(x => x.RelativeWidths).Sum(x => x.WidthAmount);
            for (int i = 0; i < Columns.Count; i++)
            {
                var col = Columns[i];
                colWidths[i] = col.RelativeWidths ? col.WidthAmount / totalFlex * flexWidthRoom : col.WidthAmount;
            }

            //CellDrawInfos
            var maxRow = Cells.Max(x => x.Row + (x.RowSpan ?? 1) - 1);
            var verticalBorders = new (bool exists, SKColor? color)[maxRow + 1, Columns.Count - 1];
            var horizontalBorders = new (bool exists, SKColor? color)[maxRow, Columns.Count];
            var cellDrawInfos = new List<CellDrawInfo>();
            foreach (var cell in Cells)
            {
                var cdi = new CellDrawInfo
                {
                    Cell = cell,
                    DrawInfo = new DrawInfo(),
                    FromRow = cell.Row,
                    ToRow = cell.Row + (cell.RowSpan ?? 1) - 1,
                    FromCol = cell.Col,
                    ToCol = cell.Col + (cell.ColSpan ?? 1) - 1,
                };
                cdi.Width = (cdi.ToCol - cdi.FromCol) * InnerVerticalLines.width; //the borders
                for (int i = cdi.FromCol; i <= cdi.ToCol; i++)
                {
                    cdi.Width += colWidths[i];
                }
                float availWidth = cdi.Width - cell.LeftPadding - cell.RightPadding;
                foreach (var c in cell.Contents)
                {
                    var cDrawInfo = c.PrepareToDraw(availWidth, resources);
                    cdi.DrawInfo.Instructions.AddRange(cDrawInfo.Instructions);
                    if (cDrawInfo.Right > cdi.DrawInfo.Right)
                        cdi.DrawInfo.Right = cDrawInfo.Right;
                    if (cDrawInfo.Bottom > cdi.DrawInfo.Bottom)
                        cdi.DrawInfo.Bottom = cDrawInfo.Bottom;
                }
                cellDrawInfos.Add(cdi);
                //top/bottom borders
                for (int i = cdi.FromCol; i <= cdi.ToCol; i++)
                {
                    if (cdi.FromRow > 0)
                    {
                        ref var t = ref horizontalBorders[cdi.FromRow - 1, i];
                        t.exists = true;
                        if (cell.TopBorderColor != null)
                            t.color = cell.TopBorderColor.Value;
                    }
                    if (cdi.ToRow < maxRow)
                    {
                        ref var b = ref horizontalBorders[cdi.ToRow, i];
                        b.exists = true;
                        if (cell.BottomBorderColor != null)
                            b.color = cell.BottomBorderColor.Value;
                    }
                }
                //left/right borders
                for (int i = cdi.FromRow; i <= cdi.ToRow; i++)
                {
                    if (cdi.FromCol > 0)
                    {
                        ref var l = ref verticalBorders[i, cdi.FromCol - 1];
                        l.exists = true;
                        if (cell.LeftBorderColor != null)
                            l.color = cell.LeftBorderColor.Value;
                    }
                    if (cdi.ToCol < Columns.Count - 1)
                    {
                        ref var r = ref verticalBorders[i, cdi.ToCol];
                        r.exists = true;
                        if (cell.RightBorderColor != null)
                            r.color = cell.RightBorderColor.Value;
                    }
                }
            }

            //draw cells
            var drawInfo = new DrawInfo();
            drawInfo.Instructions.Add(new CS.q());
            drawInfo.Instructions.Add(new CS.cm(1, 0, 0, 1, bounds.Left.Value, bounds.Top.Value));
            var rowHeights = new float[maxRow + 1];
            for (int i = 0; i <= maxRow; i++)
            {
                var cdis = cellDrawInfos.Where(x => x.ToRow == i).ToList();
                //get row height
                float rowHeight = 0;
                foreach (var cdi in cdis)
                {
                    var cell = cdi.Cell;
                    float soFar = Enumerable.Range(cdi.FromRow, cdi.ToRow - cdi.FromRow).Sum(x => rowHeights[x]) + (cdi.ToRow - cdi.FromRow) * InnerHorizontalLines.width;
                    cdi.LastRowHeight = cdi.DrawInfo.Bottom + cell.TopPadding + cell.BottomPadding - soFar;
                    if (cdi.LastRowHeight > rowHeight)
                        rowHeight = cdi.LastRowHeight;
                }
                rowHeights[i] = rowHeight;
                //draw
                foreach (var cdi in cdis)
                {
                    var cell = cdi.Cell;
                    float left = LeftBorder.width + colWidths.Take(cdi.FromCol).Sum() + InnerVerticalLines.width * cdi.FromCol + cell.LeftPadding,
                        top = TopBorder.width + rowHeights.Take(cell.Row).Sum() + InnerHorizontalLines.width * cell.Row + cell.TopPadding;
                    if (cell.HorizontalAlignment == Alignment.Center)
                        left += (cdi.Width - cdi.DrawInfo.Right) / 2;
                    else if (cell.HorizontalAlignment == Alignment.Right)
                        left += (cdi.Width - cdi.DrawInfo.Right);
                    if (cell.VerticalAlignment == VAlignment.Center)
                        top += (rowHeight - cdi.LastRowHeight) / 2;
                    else if (cell.VerticalAlignment == VAlignment.Bottom)
                        top += rowHeight - cdi.LastRowHeight;
                    drawInfo.Instructions.Add(new CS.q());
                    drawInfo.Instructions.Add(new CS.cm(1, 0, 0, 1, left, top));
                    drawInfo.Instructions.AddRange(cdi.DrawInfo.Instructions);
                    drawInfo.Instructions.Add(new CS.Q());
                }

            }

            //inner borders
            //  vertical
            for (int r = 0; r <= maxRow; r++)
            {
                for (int c = 0; c < Columns.Count - 1; c++)
                {
                    var b = verticalBorders[r, c];
                    if (!b.exists)
                        continue;
                    float x = LeftBorder.width + colWidths.Take(c + 1).Sum() + c * InnerVerticalLines.width + InnerVerticalLines.width / 2,
                        y = TopBorder.width + rowHeights.Take(r).Sum() + r * InnerHorizontalLines.width;
                    drawInfo.Instructions.Add(new CS.m(x, y));
                    drawInfo.Instructions.Add(new CS.l(x, y + rowHeights[r]));
                    drawInfo.Instructions.Add(new CS.RG(b.color ?? InnerVerticalLines.color));
                    drawInfo.Instructions.Add(new CS.w(InnerVerticalLines.width));
                    drawInfo.Instructions.Add(new CS.S());
                }
            }
            //  horizontal
            for (int r = 0; r < maxRow; r++)
            {
                for (int c = 0; c < Columns.Count; c++)
                {
                    var b = horizontalBorders[r, c];
                    if (!b.exists)
                        continue;
                    float x = LeftBorder.width + colWidths.Take(c).Sum() + (c == 0 ? 0 : c - 1) * InnerVerticalLines.width,
                        y = TopBorder.width + rowHeights.Take(r + 1).Sum() + r * InnerHorizontalLines.width + InnerVerticalLines.width / 2;
                    drawInfo.Instructions.Add(new CS.m(x, y));
                    drawInfo.Instructions.Add(new CS.l(x + colWidths[c] + (2 - (c == 0 ? 1 : 0) - (c == Columns.Count - 1 ? 1 : 0)) * InnerVerticalLines.width, y));
                    drawInfo.Instructions.Add(new CS.RG(b.color ?? InnerHorizontalLines.color));
                    drawInfo.Instructions.Add(new CS.w(InnerHorizontalLines.width));
                    drawInfo.Instructions.Add(new CS.S());
                }
            }

            //outer borders
            drawInfo.Right = LeftBorder.width + colWidths.Sum() + InnerVerticalLines.width * (Columns.Count - 1) + RightBorder.width;
            drawInfo.Bottom = TopBorder.width + rowHeights.Sum() + InnerHorizontalLines.width * maxRow + BottomBorder.width;
            Bounds.Bottom = drawInfo.Bottom;
            //  top
            drawInfo.Instructions.Add(new CS.m(0, 0));
            drawInfo.Instructions.Add(new CS.l(LeftBorder.width, TopBorder.width));
            drawInfo.Instructions.Add(new CS.l(drawInfo.Right - RightBorder.width, TopBorder.width));
            drawInfo.Instructions.Add(new CS.l(drawInfo.Right, 0));
            drawInfo.Instructions.Add(new CS.rg(TopBorder.color));
            drawInfo.Instructions.Add(new CS.f());
            //  right
            drawInfo.Instructions.Add(new CS.m(drawInfo.Right, 0));
            drawInfo.Instructions.Add(new CS.l(drawInfo.Right - RightBorder.width, TopBorder.width));
            drawInfo.Instructions.Add(new CS.l(drawInfo.Right - RightBorder.width, drawInfo.Bottom - BottomBorder.width));
            drawInfo.Instructions.Add(new CS.l(drawInfo.Right, drawInfo.Bottom));
            drawInfo.Instructions.Add(new CS.rg(RightBorder.color));
            drawInfo.Instructions.Add(new CS.f());
            //  bottom
            drawInfo.Instructions.Add(new CS.m(drawInfo.Right, drawInfo.Bottom));
            drawInfo.Instructions.Add(new CS.l(drawInfo.Right - RightBorder.width, drawInfo.Bottom - BottomBorder.width));
            drawInfo.Instructions.Add(new CS.l(LeftBorder.width, drawInfo.Bottom - BottomBorder.width));
            drawInfo.Instructions.Add(new CS.l(0, drawInfo.Bottom));
            drawInfo.Instructions.Add(new CS.rg(BottomBorder.color));
            drawInfo.Instructions.Add(new CS.f());
            //  left
            drawInfo.Instructions.Add(new CS.m(LeftBorder.width, drawInfo.Bottom - BottomBorder.width));
            drawInfo.Instructions.Add(new CS.l(0, drawInfo.Bottom));
            drawInfo.Instructions.Add(new CS.l(0, 0));
            drawInfo.Instructions.Add(new CS.l(LeftBorder.width, TopBorder.width));
            drawInfo.Instructions.Add(new CS.l(LeftBorder.width, drawInfo.Bottom - BottomBorder.width));
            drawInfo.Instructions.Add(new CS.rg(LeftBorder.color));
            drawInfo.Instructions.Add(new CS.f());

            drawInfo.Instructions.Add(new CS.Q());

            //var rowCells = Cells.GroupBy

            //outer borders
            //var innerBounds=new ve.Bounds(bounds.UpperLeftX.Value + LeftBorder.width, bounds.UpperLeftY+TopBorder.width,)
            //Instructions.Add(new m(bounds.UpperLeftX.Value, bounds.UpperLeftY.Value));
            //Instructions.Add(new l(bounds.UpperLeftX.Value + VerticalBorders.width * (Columns.Count - 1) + colWidths.Sum(), bounds.UpperLeftY.Value));
            return drawInfo;
        }

        class CellDrawInfo
        {
            public TableCell Cell;
            public DrawInfo DrawInfo;
            public float Width, LastRowHeight;
            public int FromRow, ToRow, FromCol, ToCol;
        }
    }

    public class TableColumn
    {
        public bool RelativeWidths;
        public float WidthAmount;

        public TableColumn(bool relativeWidths, float widthAmount)
        {
            RelativeWidths = relativeWidths;
            WidthAmount = widthAmount;
        }
    }

    public class TableCell
    {
        public int Row, Col;
        public int? RowSpan, ColSpan;
        public Alignment HorizontalAlignment;
        public VAlignment VerticalAlignment;
        public float TopPadding, RightPadding, LeftPadding, BottomPadding;
        public SKColor? TopBorderColor, RightBorderColor, BottomBorderColor, LeftBorderColor;
        public readonly List<VisualElement> Contents = new List<VisualElement>();

        public TableCell(int row, int col) { Row = row; Col = col; }
        public TableCell(int row, int col, VisualElement content) : this(row, col) { Contents.Add(content); }
    }
}
