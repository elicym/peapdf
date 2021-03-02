/*
 * Copyright 2021 Elliott Cymerman
 * SPDX-License-Identifier: Apache-2.0
 */

using System;
using System.Linq;
using System.Collections.Generic;
using SkiaSharp;
using System.Diagnostics;

namespace SeaPeaYou.PeaPdf
{
    public class PDF
    {

        public string Password;
        public List<Page> Pages = new List<Page>();
        internal readonly W.FileTrailer FileTrailer;
        internal PdfFile PdfFile;


        public PDF(byte[] bytes, string password = null)
        {
            PdfFile = new PdfFile(bytes, password);

            FileTrailer = PdfFile.FileTrailer;

            var treeNodeStack = new Stack<W.PageTreeNode>();
            doTreeNode(FileTrailer.Root.Pages, null);

            void doTreeNode(W.PageTreeNode treeNode, PdfDict resources)
            {
                treeNodeStack.Push(treeNode);
                resources = (PdfDict)treeNode.PdfDict["Resources"] ?? resources;
                foreach (var kid in treeNode.Kids)
                {
                    if (kid.Type == "Pages")
                    {
                        doTreeNode(new W.PageTreeNode(kid), resources);
                    }
                    else
                    {
                        Pages.Add(new Page(kid, treeNodeStack, resources));
                    }
                }
                treeNodeStack.Pop();
            }

        }

        public PDF()
        {
            FileTrailer = new W.FileTrailer();
        }

        public SKImage Render(int pageNum, float scale)
        {
            var page = Pages[pageNum];
            return new Renderer(this, page, scale).SKImage;
        }

        public void SetField(string name, string val)
        {
            var acroForm = FileTrailer.Root.AcroForm;
            if (acroForm == null)
                throw new Exception("No form found.");
            W.Field matchingField = null;
            findField(acroForm.Fields, "");
            if (matchingField == null)
                throw new Exception("Field not found.");

            var field = matchingField;
            field.V = new PdfString(val);
            var kids = field.Kids;
            var widgetDict = ((PdfName)field.PdfDict["Subtype"])?.String == "Widget" ? field.PdfDict
                : ((kids != null && kids.Value.Count > 0) ? kids.Value[0] : null);
            if (widgetDict != null)
            {
                var widget = new W.Annotation(widgetDict);

                var daCS = W.ContentStream.ParseInstructions(field.DA.Value);
                var Tf = daCS.OfType<CS.Tf>().Last();
                if (Tf.size == 0) Tf.size = 11;
                var DR = field.DR ?? acroForm.DR;
                var fontDict = (PdfDict)DR.Font[Tf.font];
                var font = new Font(fontDict);
                var paint = new SKPaint { Typeface = font.Typeface, TextSize = Tf.size * 64 };

                if (widget.AP == null) widget.AP = new W.AppearanceStream(widget.Rect);
                var cs = widget.AP.N.GetFormXObject();
                cs.Resources.Font[Tf.font] = fontDict;
                cs.Instructions.Clear();
                cs.Instructions.Add(new CS.BMC("Tx"));
                cs.Instructions.Add(new CS.BT());
                foreach (var inst in daCS)
                    cs.Instructions.Add(inst);
                float boxWidth = widget.Rect.UpperRightX - widget.Rect.LowerLeftX, boxHeight = widget.Rect.UpperRightY - widget.Rect.LowerLeftY;
                cs.PdfStream.Dict["BBox"] = new W.Rectangle(0, 0, boxWidth, boxHeight).PdfArray;

                if (field.Comb)
                {
                    if (val.Length > field.MaxLen.Value)
                        val = val.Substring(0, field.MaxLen.Value);
                    float cellWidth = boxWidth / field.MaxLen.Value, toCellEnd = 0;
                    int skipChars = field.Q == W.Alignment.Right ? field.MaxLen.Value - val.Length
                        : (field.Q == W.Alignment.Centered ? (field.MaxLen.Value - val.Length) / 2 : 0);
                    cs.Instructions.Add(new CS.Td(cellWidth * skipChars, (boxHeight - Tf.size) / 2 + 1.5f));
                    var arr = new PdfArray();
                    foreach (var c in val)
                    {
                        var str = c.ToString();
                        float glyphWidth = paint.GetGlyphWidths(str)[0] / 64, leftPadding = (cellWidth - glyphWidth) / 2;
                        arr.Add((PdfNumeric)(-(toCellEnd + leftPadding) * 1000 / Tf.size));
                        arr.Add((PdfString)str);
                        toCellEnd = cellWidth - glyphWidth - leftPadding;
                    }
                    cs.Instructions.Add(new CS.TJ(arr));
                }
                else
                {
                    var glyphWidths = paint.GetGlyphWidths(val);
                    float offset = 0;
                    if (field.Q == W.Alignment.Right)
                    {
                        offset = boxWidth - glyphWidths.Sum() - 2;
                    }
                    else if (field.Q == W.Alignment.Centered)
                    {
                        offset = (boxWidth - glyphWidths.Sum()) / 2;
                    }
                    cs.Instructions.Add(new CS.Td(2 + offset, (boxHeight - Tf.size) / 2));
                    cs.Instructions.Add(new CS.Tj((PdfString)val));
                }
                cs.Instructions.Add(new CS.ET());
                cs.Instructions.Add(new CS.EMC());
                widget.UpdateObjects();
            }

            void findField(PdfArray<PdfDict> fieldArr, string runningName)
            {
                foreach (PdfDict fieldDict in fieldArr)
                {
                    var field = new W.Field(fieldDict);
                    if (field.T == null)
                        continue; //is a widget annotation
                    var totalName = (string.IsNullOrEmpty(runningName) ? "" : (runningName + ".")) + field.T;
                    var kids = field.Kids;
                    if (totalName == name)
                    {
                        matchingField = field;
                        return;
                    }
                    if (kids != null)
                    {
                        findField(kids.Value, totalName);
                        if (matchingField != null)
                            return;
                    }
                }
            }
        }

        public void FlattenFields()
        {
            FileTrailer.Root.Dict["AcroForm"] = null;
            foreach (var page in Pages)
            {
                foreach (var annot in page.GetAnnots())
                {
                    annot.PdfDict["FT"] = null;
                    annot.PdfDict["Subtype"] = (PdfName)"FreeText";
                    annot.PdfDict["Border"] = new PdfArray((PdfNumeric)0, (PdfNumeric)0, (PdfNumeric)0);
                    annot.PdfDict["F"] = (PdfNumeric)(1 << 2 | 1 << 6 | 1 << 7 | 1 << 9);
                }
            }
        }

        public byte[] Save(SaveOptions opts = null)
        {
            if (opts == null) opts = new SaveOptions();

            var fileID = (part1: opts.FileID ?? (FileTrailer.ID == null ? Guid.NewGuid().ToByteArray() : FileTrailer.ID.Value.Item1), part2: Guid.NewGuid().ToByteArray());

            byte[] encryptionKey = null;
            if (opts.Encryption != null)
            {
                if ((opts.Encryption.OwnerPwd != null && opts.Encryption.OwnerPwd.Any(x => x >= 128))
                    || (opts.Encryption.OwnerPwd != null && opts.Encryption.OwnerPwd.Any(x => x >= 128)))
                    throw new NotImplementedException("Only ASCII characters allowed in the password.");
                var O = Encryption.ComputeO(opts.Encryption.UserPwd, opts.Encryption.OwnerPwd, 128);
                var P = -3392 | (int)opts.Encryption.UserAccessPermissions;
                encryptionKey = Encryption.ComputeEncryptionKey(Encryption.PadBytes(opts.Encryption.UserPwd), O, P, fileID.part1, 128);
                var U = Encryption.ComputeU(encryptionKey, fileID.part1);
                FileTrailer.Encrypt = new PdfDict
                {
                    {"Filter", (PdfName)"Standard" },
                    {"V", (PdfNumeric)4 },
                    {"R", (PdfNumeric)4 },
                    {"O", (PdfString)O },
                    {"U", (PdfString)U },
                    {"P", (PdfNumeric)P },
                    {"Length", (PdfNumeric)128 },
                    {"StmF",(PdfName)"StdCF" },
                    {"StrF",(PdfName)"StdCF" },
                    {"CF", new PdfDict { {"StdCF", new PdfDict { {"AuthEvent",(PdfName)"DocOpen" }, {"CFM",(PdfName)(opts.Encryption.UseRC4? "V2":"AESV2") } } } } }
                };
            }
            else
            {
                FileTrailer.Encrypt = null;
            }

            const int objectStreamLength = 20;

            //prepare objects
            var root = FileTrailer.Root;
            root.Pages = new W.PageTreeNode(Pages);
            Pages.ForEach(x => x.UpdateObjects(root.Pages));
            FileTrailer.Prev = null;

            //pdf writer
            var w = new PdfWriter(encryptionKey, opts.Encryption?.UseRC4 ?? false);
            w.WriteString("%PDF-1.7");
            w.WriteNewLine();
            w.WriteByte('%');
            for (byte i = 170; i < 174; i++)
                w.WriteByte(i);
            w.WriteNewLine();

            var objNum = 0;
            var indirectObjs = new HashSet<PdfObject>();
            var objsSeen = new HashSet<PdfObject>();
            var objPath = new List<string>();
            examineObj(FileTrailer.Dict);

            if (!opts.NoObjectStreams && opts.Encryption == null)
            {
                //separate objects between those that can be in object streams and those not
                List<PdfObject> objsInObjStream = new List<PdfObject>(), objsNotInObjStream = new List<PdfObject>();
                foreach (var obj in indirectObjs)
                {
                    bool notInObjStream = obj is PdfStream || obj == FileTrailer.Encrypt;
                    (notInObjStream ? objsNotInObjStream : objsInObjStream).Add(obj);
                }
                foreach (var obj in objsInObjStream)
                    w.IndirectObjs.Add(obj, ++objNum);
                foreach (var obj in objsNotInObjStream)
                    w.IndirectObjs.Add(obj, ++objNum);
                //write
                var xRefEntries = new List<XRefEntry>();
                var streamObjOffsets = new List<int>();
                foreach (var chunk in objsInObjStream.Chunk(objectStreamLength))
                {
                    var streamObjNum = ++objNum;
                    var objStreamOffsets = new PdfWriter();
                    var objStreamW = new PdfWriter
                    {
                        IndirectObjs = w.IndirectObjs
                    };
                    for (int i = 0; i < chunk.Count; i++)
                    {
                        var obj = chunk[i];
                        var _objNum = w.IndirectObjs[obj];
                        xRefEntries.Add(new XRefEntry(XRefEntryType.Compressed, streamObjNum, i));
                        objStreamOffsets.WriteObj((PdfNumeric)_objNum, null, true);
                        objStreamOffsets.WriteObj((PdfNumeric)objStreamW.Count, null, true);
                        objStreamW.WriteObj(obj, null, true);
                    }
                    objStreamOffsets.WriteByte(' ');

                    var streamBytes = new byte[objStreamOffsets.Count + objStreamW.Count];
                    Array.Copy(objStreamOffsets.ToArray(), streamBytes, objStreamOffsets.Count);
                    Array.Copy(objStreamW.ToArray(), 0, streamBytes, objStreamOffsets.Count, objStreamW.Count);
                    streamObjOffsets.Add(w.Count);
                    var streamObj = new PdfStream(streamBytes, "FlateDecode", new PdfDict
                    {
                        { "Type", (PdfName)"ObjStm" },
                        { "N", (PdfNumeric)chunk.Count },
                        { "First", (PdfNumeric)objStreamOffsets.Count }
                    });
                    writeBaseObj(streamObj, new ObjID(streamObjNum, 0));
                }
                foreach (var obj in objsNotInObjStream)
                {
                    var _objNum = w.IndirectObjs[obj];
                    xRefEntries.Add(new XRefEntry(XRefEntryType.InUse, w.Count, 0));
                    writeBaseObj(obj, new ObjID(_objNum, 0));
                }
                foreach (var streamObjOffset in streamObjOffsets)
                    xRefEntries.Add(new XRefEntry(XRefEntryType.InUse, streamObjOffset, 0));

                var xRefObjNum = ++objNum;
                xRefEntries.Add(new XRefEntry(XRefEntryType.InUse, w.Count, 0));
                var fileTrailerStream = new W.FileTrailerStream(FileTrailer);
                fileTrailerStream.FileTrailer.Root = FileTrailer.Root;
                fileTrailerStream.FileTrailer.ID = fileID;
                var xRef = new XRef.XRefStream(xRefEntries, w.Count, fileTrailerStream);
                writeBaseObj(xRef.PdfStream, new ObjID(xRefObjNum, 0), true);

                w.WriteString("startxref");
                w.WriteNewLine();
                w.WriteString(xRef.Offset.ToString());
                w.WriteNewLine();
                w.WriteString("%%EOF");
            }
            else
            {
                var xRefEntries = new List<XRefInUseEntry>();
                //assign object numbers before writing
                foreach (var obj in indirectObjs)
                {
                    w.IndirectObjs.Add(obj, ++objNum);
                }
                //write
                foreach (var obj in w.IndirectObjs)
                {
                    var objID = new ObjID(obj.Value, 0);
                    xRefEntries.Add(new XRefInUseEntry(w.Count, objID));
                    //will be on a new line here
                    writeBaseObj(obj.Key, objID);
                }

                FileTrailer.Size = objNum + 1;
                FileTrailer.ID = fileID;
                new XRef.XRefTable(xRefEntries, w.Count, FileTrailer).Write(w);
            }

            return w.ToArray();

            bool getIsIndirect(PdfDict dict)
            {
                if (objPath.Count == 1 && objPath[0] == "Info")
                    return true;
                var type = dict.Type;
                if (type == "Catalog" || type == "Pages" || type == "Page" || type == "Font")
                    return true;
                if (objPath.Count >= 6 && objPath[0] == "Root" && objPath[1] == "AcroForm" && objPath[2] == "Fields" && objPath[3] == null && objPath.Count % 2 == 0)
                {
                    bool match = true;
                    for (int i = 4; i < objPath.Count; i++)
                    {
                        if (objPath[i] != "Kids" || objPath[i + 1] != null)
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match)
                        return true;
                }
                return false;
            }

            void examineObj(PdfObject obj)
            {

                if (obj == null) return;

                bool isSimpleObj = obj is PdfName || obj is PdfNumeric,
                    seen = !isSimpleObj && !objsSeen.Add(obj),
                    isIndirect = seen || obj is PdfStream;
                var dict = obj as PdfDict;
                if (!isIndirect && dict != null && getIsIndirect(dict))
                    isIndirect = true;


                if (isIndirect)
                {
                    indirectObjs.Add(obj);
                    if (seen)
                        return;
                }

                if (dict != null)
                {
                    recurseDict(dict);
                }
                else if (obj is PdfArray arr)
                {
                    foreach (var subObj in arr)
                    {
                        objPath.Add(null);
                        examineObj(subObj);
                        objPath.RemoveAt(objPath.Count - 1);
                    }
                }
                else if (obj is PdfStream stream)
                {
                    recurseDict(stream.Dict);
                }

                void recurseDict(PdfDict dict)
                {
                    foreach (var (key, value) in dict)
                    {
                        objPath.Add(key);
                        if (value is PdfIndirectReference) Debugger.Break();
                        examineObj(value);
                        objPath.RemoveAt(objPath.Count - 1);
                    }
                }
            }

            void writeBaseObj(PdfObject obj, ObjID objID, bool noEncrypt = false)
            {
                new TwoNums(objID, "obj").Write(w);
                w.WriteNewLine();
                w.WriteObj(obj, noEncrypt ? (ObjID?)null : objID, true);
                w.WriteNewLine();
                w.WriteString("endobj");
                w.WriteNewLine();
            }
        }

        internal PdfObject GetPageObj(PdfDict page, string key)
        {
            var _page = page;
            do
            {
                var obj = _page[key];
                if (obj != null)
                    return obj;
                _page = (PdfDict)_page["Parent"];
            } while (_page != null);
            return null;
        }

    }

}
