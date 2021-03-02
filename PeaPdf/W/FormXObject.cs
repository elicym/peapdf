/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SeaPeaYou.PeaPdf.W
{

    class FormXObjects
    {
        readonly FormXObject singleState;
        readonly Dictionary<string, FormXObject> states;

        public FormXObjects(PdfObject obj)
        {
            if (obj is PdfStream s)
            {
                singleState = new FormXObject(s);
            }
            else
            {
                states = new Dictionary<string, FormXObject>();
                var d = (PdfDict)obj;
                foreach (var (k, v) in d)
                {
                    states.Add(k, new FormXObject((PdfStream)v));
                }
            }
        }

        public FormXObjects(FormXObject formXObject) => singleState = formXObject;

        public FormXObject GetFormXObject() => singleState;
        public FormXObject GetFormXObjectByState(string state) => states[state];

        public void UpdateObjects()
        {
            singleState?.UpdateObjects();
            if (states != null)
            {
                foreach (var item in states.Values)
                {
                    item.UpdateObjects();
                }
            }
        }

        public PdfObject GetPdfObject()
        {
            if (states != null)
            {
                return new PdfDict(states.ToDictionary(x => x.Key, x => (PdfObject)x.Value.PdfStream));
            }
            return singleState.PdfStream;
        }

    }

    class FormXObject : ContentStream
    {

        public Rectangle BBox { get => new Rectangle((PdfArray)PdfStream.Dict["BBox"]); set => PdfStream.Dict["BBox"] = value.PdfArray; }

        public FormXObject(PdfStream stream) : base(stream, null) { }

        public FormXObject(Rectangle BBox)
        {
            this.BBox = BBox;
            PdfStream.Dict.Type = "XObject";
            PdfStream.Dict["Subtype"] = (PdfName)"Form";
        }

    }
}
