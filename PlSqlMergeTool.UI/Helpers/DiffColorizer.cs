using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using System.Collections.Generic;
using System.Linq;

namespace PlSqlMergeTool.UI.Helpers;
// больше не используется, пока как Legacy висит 
public class DiffColorizer : DocumentColorizingTransformer
{
    private readonly List<HighlightRegion> _regions;

    private static readonly SolidColorBrush AddedLineBrush = new(Color.FromArgb(40, 76, 175, 80));
    private static readonly SolidColorBrush DeletedLineBrush = new(Color.FromArgb(40, 244, 67, 54));
    private static readonly SolidColorBrush ConflictLineBrush = new(Color.FromArgb(50, 255, 152, 0));

    public DiffColorizer(List<HighlightRegion> regions)
    {
        _regions = regions ?? new List<HighlightRegion>();
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        if (_regions.Count == 0 || line.Length == 0) return;

        int currentLineNumber = line.LineNumber;

        var region = _regions.FirstOrDefault(r => currentLineNumber >= r.StartLine && currentLineNumber <= r.EndLine);

        if (region != null)
        {
            var lineBrush = GetLineBrush(region.Type);

            if (lineBrush != null)
            {
                // Закрашиваем всю строку (от начала и до самого конца)
                ChangeLinePart(line.Offset, line.Offset + line.Length, element =>
                {
                    element.TextRunProperties.SetBackgroundBrush(lineBrush);
                });
            }
        }
    }

    private static SolidColorBrush? GetLineBrush(HighlightType type)
    {
        return type switch
        {
            HighlightType.Added => AddedLineBrush,
            HighlightType.Deleted => DeletedLineBrush,
            HighlightType.Conflict => ConflictLineBrush,
            _ => null
        };
    }
}