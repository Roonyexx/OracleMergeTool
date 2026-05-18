using AvaloniaEdit;
using System;
using System.Linq;
using AvaloniaEdit.Document;

namespace PlSqlMergeTool.UI.Helpers
{
    public static class TextEditorExtensions
    {
        public static void InsertPhantomSpace(this TextEditor editor, int insertAfterLine, int emptyLinesCount)
        {
            if (editor.Document == null || emptyLinesCount <= 0) 
                return;

            insertAfterLine = Math.Clamp(insertAfterLine, 0, editor.Document.LineCount);

            var margin = editor.TextArea.LeftMargins.OfType<DiffLineNumberMargin>().FirstOrDefault();

            int offset = 0;
            if (insertAfterLine > 0)
            {
                var line = editor.Document.GetLineByNumber(insertAfterLine);
                offset = line.Offset + line.TotalLength; 
            }

            string newLines = new string('\n', emptyLinesCount);
            
            if (insertAfterLine == editor.Document.LineCount && 
                editor.Document.TextLength > 0 && 
                !editor.Document.Text.EndsWith("\n"))
            {
                newLines = "\n" + newLines;
            }

            editor.Document.Insert(offset, newLines);

            if (margin != null)
            {
                for (int i = 1; i <= emptyLinesCount; i++)
                {
                    int lineNum = insertAfterLine + i;
                    if (lineNum <= editor.Document.LineCount)
                    {
                        var lineToMark = editor.Document.GetLineByNumber(lineNum);
                        var anchor = editor.Document.CreateAnchor(lineToMark.Offset);
                        anchor.SurviveDeletion = true;
                        anchor.MovementType = AnchorMovementType.Default;
                        margin.PhantomAnchors.Add(anchor);
                    }
                }
            }

            margin?.InvalidateVisual();
        }

        public static void RemovePhantomSpaces(this TextEditor editor)
        {
            if (editor.Document == null) return;

            var margin = editor.TextArea.LeftMargins.OfType<DiffLineNumberMargin>().FirstOrDefault();
            if (margin == null || margin.PhantomAnchors.Count == 0) return;

            var phantomLineNums = margin.GetPhantomLineNumbers().OrderByDescending(l => l).ToList();
            
            foreach (var phantomLineNum in phantomLineNums)
            {
                if (phantomLineNum <= editor.Document.LineCount)
                {
                    var line = editor.Document.GetLineByNumber(phantomLineNum);
                    
                    int removeOffset = line.Offset;
                    int removeLength = line.TotalLength;

                    if (removeLength == 0 && line.PreviousLine != null)
                    {
                        removeOffset = line.PreviousLine.Offset + line.PreviousLine.Length;
                        removeLength = line.PreviousLine.DelimiterLength;
                    }

                    if (removeLength > 0)
                    {
                        editor.Document.Remove(removeOffset, removeLength);
                    }
                }
            }

            margin.PhantomAnchors.Clear();
            margin?.InvalidateVisual();
        }
    }
}