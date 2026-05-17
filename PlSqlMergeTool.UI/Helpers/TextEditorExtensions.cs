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

            // Защита от выхода за границы
            insertAfterLine = Math.Clamp(insertAfterLine, 0, editor.Document.LineCount);

            // 1. Ищем нашу кастомную колонку
            var margin = editor.TextArea.LeftMargins.OfType<DiffLineNumberMargin>().FirstOrDefault();
            
            // 2. ОБНОВЛЯЕМ СОСТОЯНИЕ ДО ВСТАВКИ ТЕКСТА
            if (margin != null)
            {
                // Сдвигаем старые фантомы, которые находятся ниже места вставки
                var oldPhantomsToShift = margin.PhantomLines.Where(line => line > insertAfterLine).ToList();
                foreach (var oldPhantom in oldPhantomsToShift)
                {
                    margin.PhantomLines.Remove(oldPhantom);
                }
                foreach (var oldPhantom in oldPhantomsToShift)
                {
                    margin.PhantomLines.Add(oldPhantom + emptyLinesCount);
                }

                // Добавляем новые фантомы
                for (int i = 1; i <= emptyLinesCount; i++)
                {
                    margin.PhantomLines.Add(insertAfterLine + i);
                }
            }

            // 3. ПРАВИЛЬНОЕ ВЫЧИСЛЕНИЕ СМЕЩЕНИЯ (Начало следующей строки)
            int offset = 0;
            if (insertAfterLine > 0)
            {
                var line = editor.Document.GetLineByNumber(insertAfterLine);
                offset = line.Offset + line.TotalLength; // Включает \r\n
            }

            // 4. ФИЗИЧЕСКАЯ ВСТАВКА В ДОКУМЕНТ
            string newLines = new string('\n', emptyLinesCount);
            
            // Если вставляем в самый конец файла, а там нет пустого переноса, добавляем его
            if (insertAfterLine == editor.Document.LineCount && 
                editor.Document.TextLength > 0 && 
                !editor.Document.Text.EndsWith("\n"))
            {
                newLines = "\n" + newLines;
            }

            // Эта строка автоматически вызовет перерисовку редактора и нашей колонки
            editor.Document.Insert(offset, newLines);

            // 5. Принудительный рендер на всякий случай
            margin?.InvalidateVisual();
        }
    }
}