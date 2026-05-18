using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Rendering;

namespace PlSqlMergeTool.UI.Helpers;

public class DiffBackgroundRenderer : IBackgroundRenderer
{
    private readonly TextView _textView;
    private readonly List<HighlightRegion> _regions;
    private readonly HashSet<int> _phantomLines;

    public DiffBackgroundRenderer(TextView textView, List<HighlightRegion> regions, IEnumerable<int>? phantomLines = null)
    {
        _textView = textView;
        _regions = regions;
        _phantomLines = phantomLines == null ? new HashSet<int>() : new HashSet<int>(phantomLines);
    }

    public KnownLayer Layer => KnownLayer.Background;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (_textView != textView) return;
        if (_regions == null || _regions.Count == 0) return;

        double verticalOffset = textView.ScrollOffset.Y;

        foreach (var visualLine in textView.VisualLines)
        {
            var firstLine = visualLine.FirstDocumentLine;
            if (firstLine == null) continue;

            if (_phantomLines.Contains(firstLine.LineNumber)) continue;

            var region = _regions.FirstOrDefault(r => firstLine.LineNumber >= r.StartLine && firstLine.LineNumber <= r.EndLine);
            
            if (region != null)
            {
                var brush = GetBrush(region.Type);
                if (brush != null)
                {
                    double y = visualLine.VisualTop - verticalOffset;
                    
                    var rect = new Rect(0, y, textView.Bounds.Width, visualLine.Height);
                    drawingContext.DrawRectangle(brush, null, rect);
                }
            }
        }
    }

    private ISolidColorBrush? GetBrush(HighlightType type)
    {
        return type switch
        {
            HighlightType.Added => new SolidColorBrush(Color.FromArgb(50, 100, 255, 100)), // Светло-зеленый
            HighlightType.Deleted => new SolidColorBrush(Color.FromArgb(50, 255, 100, 100)), // Светло-красный
            HighlightType.Conflict => new SolidColorBrush(Color.FromArgb(50, 255, 180, 100)), // Оранжевый
            HighlightType.ResolvedFromLocal => new SolidColorBrush(Color.FromArgb(50, 100, 150, 255)), // Синий
            HighlightType.ResolvedFromTarget => new SolidColorBrush(Color.FromArgb(50, 200, 100, 255)), // Фиолетовый
            _ => null
        };
    }
}