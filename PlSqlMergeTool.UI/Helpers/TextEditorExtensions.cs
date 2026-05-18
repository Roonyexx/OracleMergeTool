using AvaloniaEdit;
using System;
using System.Linq;

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
            
            if (margin != null)
            {
                var oldPhantomsToShift = margin.PhantomLines.Where(line => line > insertAfterLine).ToList();
                foreach (var oldPhantom in oldPhantomsToShift)
                {
                    margin.PhantomLines.Remove(oldPhantom);
                }
                foreach (var oldPhantom in oldPhantomsToShift)
                {
                    margin.PhantomLines.Add(oldPhantom + emptyLinesCount);
                }

                for (int i = 1; i <= emptyLinesCount; i++)
                {
                    margin.PhantomLines.Add(insertAfterLine + i);
                }
            }

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

            margin?.InvalidateVisual();
        }
    }
}