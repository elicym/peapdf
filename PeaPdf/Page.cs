/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using SeaPeaYou.PeaPdf.W;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ve = SeaPeaYou.PeaPdf.VisualElements;

namespace SeaPeaYou.PeaPdf
{
    public class Page
    {

        internal readonly PdfDict Dict;

        internal readonly ResourceDictionary Resources;

        static readonly string[] inheritableEntries = { "Resources", "MediaBox", "CropBox", "Rotate" };

        ContentStream contents;
        internal ContentStream GetContents()
        {
            if (contents == null)
            {
                contents = new ContentStream(Dict["Contents"], Resources);
            }
            return contents;
        }

        internal Page(PdfDict dict, Stack<PageTreeNode> treeNodeStack, PdfDict resources)
        {
            Dict = dict;
            foreach (var e in inheritableEntries)
            {
                if (Dict[e] == null)
                {
                    foreach (var t in treeNodeStack)
                    {
                        var item = t.PdfDict[e];
                        if (item != null)
                        {
                            Dict[e] = item;
                            break;
                        }
                    }
                }
            }
            Resources = new ResourceDictionary((PdfDict)dict["Resources"] ?? resources);
        }

        public Page(Dimensions dimensions)
        {
            Dict = new PdfDict { Type = "Page" };
            MediaBox = new Rectangle(0, 0, dimensions.Width, dimensions.Height);
            Resources = new ResourceDictionary(new PdfDict());
        }
         
        internal WrapperArray<Annotation> GetAnnots()
        {
            var annots = (PdfArray)Dict["Annots"];
            if (annots == null)
                Dict["Annots"] = annots = new PdfArray();
            return new WrapperArray<Annotation>(annots, x => new Annotation((PdfDict)x), x => x.PdfDict);
        }

        internal Rectangle MediaBox { get => new Rectangle((PdfArray)Dict["MediaBox"]); set => Dict["MediaBox"] = value.PdfArray; }

        bool preparedForVisualElements;
        public void Add(ve.VisualElement visualElement)
        {
            var contents = GetContents();
            if (!preparedForVisualElements)
            {
                contents.PrepareForVisualElements(this);
                preparedForVisualElements = true;
            }

            var drawInfo = visualElement.PrepareToDraw(MediaBox.UpperRightX, Resources);
            contents.Instructions.AddRange(drawInfo.Instructions);
        }

        internal void UpdateObjects(PageTreeNode parent)
        {
            if (contents != null)
            {
                contents.UpdateObjects();
                Dict["Contents"] = contents.PdfStream;
                Dict["Resources"] = contents.Resources.PdfDict;
            }
            else
            {
                Dict["Resources"] = Resources.PdfDict;
            }
            Dict["Parent"] = parent.PdfDict;
        }

    }
}
