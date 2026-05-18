using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using AvaloniaEdit.Document;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PlSqlMergeTool.UI.Helpers;

public class DiffLineNumberMargin : AbstractMargin
{
    public List<TextAnchor> PhantomAnchors { get; } = new();

    private static readonly Typeface Typeface = new("Consolas", FontStyle.Normal, FontWeight.Normal);

    public HashSet<int> GetPhantomLineNumbers()
    {
        var set = new HashSet<int>();
        if (Document == null) return set;
        
        PhantomAnchors.RemoveAll(a => a.IsDeleted);

        foreach (var anchor in PhantomAnchors)
        {
            if (anchor.IsDeleted) continue;
            int lineNum = anchor.Line;
            if (lineNum >= 1 && lineNum <= Document.LineCount)
            {
                var line = Document.GetLineByNumber(lineNum);
                if (line.Length == 0) // only delimiter, perfectly empty
                {
                    set.Add(lineNum);
                }
            }
        }
        return set;
    }

    public Dictionary<int, int> GetCurrentGapsByCleanLine()
    {
        var gaps = new Dictionary<int, int>();
        if (Document == null) return gaps;

        var phantomLines = GetPhantomLineNumbers().OrderBy(l => l).ToList();
        int totalPhantomsAbove = 0;
        
        foreach (var p in phantomLines)
        {
            // A phantom at dirty line 'p' corresponds to clean line (p - phantoms_above - 1)
            int cleanLine = p - totalPhantomsAbove - 1;
            if (!gaps.ContainsKey(cleanLine)) gaps[cleanLine] = 0;
            gaps[cleanLine]++;
            totalPhantomsAbove++;
        }
        return gaps;
    }

    public override void Render(DrawingContext context)
    {
        if (TextView == null || Document == null) return;

        var brush = new SolidColorBrush(Color.Parse("#888888"));
        var phantomLines = GetPhantomLineNumbers();
        
        int realLineNumber = 1;

        foreach (var visualLine in TextView.VisualLines)
        {
            int documentLineNumber = visualLine.FirstDocumentLine.LineNumber;

            realLineNumber = CalculateRealLineNumber(documentLineNumber, phantomLines);

            double y = visualLine.GetTextLineVisualYPosition(visualLine.TextLines[0], VisualYPosition.TextTop);

            string textToDraw = phantomLines.Contains(documentLineNumber) 
                ? "-" 
                : realLineNumber.ToString();

            var formattedText = new FormattedText(
                textToDraw,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                Typeface,
                14,
                brush);

            double x = Bounds.Width - formattedText.Width - 5;
            context.DrawText(formattedText, new Point(x, y - TextView.VerticalOffset));
        }
    }

    private int CalculateRealLineNumber(int currentDocumentLine, HashSet<int> phantomLines)
    {
        int realCount = 0;
        for (int i = 1; i <= currentDocumentLine; i++)
        {
            if (!phantomLines.Contains(i))
                realCount++;
        }
        return realCount;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(40, 0); // Ширина колонки
    }
}