using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using System.Collections.Generic;
using System.Globalization;

namespace PlSqlMergeTool.UI.Helpers;

public class DiffLineNumberMargin : AbstractMargin
{
    public HashSet<int> PhantomLines { get; set; } = new();

    private static readonly Typeface Typeface = new("Consolas", FontStyle.Normal, FontWeight.Normal);

    public override void Render(DrawingContext context)
    {
        if (TextView == null || Document == null) return;

        var brush = new SolidColorBrush(Color.Parse("#888888"));
        
        int realLineNumber = 1;

        foreach (var visualLine in TextView.VisualLines)
        {
            int documentLineNumber = visualLine.FirstDocumentLine.LineNumber;

            realLineNumber = CalculateRealLineNumber(documentLineNumber);

            double y = visualLine.GetTextLineVisualYPosition(visualLine.TextLines[0], VisualYPosition.TextTop);

            string textToDraw = PhantomLines.Contains(documentLineNumber) 
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

    private int CalculateRealLineNumber(int currentDocumentLine)
    {
        int realCount = 0;
        for (int i = 1; i <= currentDocumentLine; i++)
        {
            if (!PhantomLines.Contains(i))
                realCount++;
        }
        return realCount;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(40, 0); // Ширина колонки
    }
}