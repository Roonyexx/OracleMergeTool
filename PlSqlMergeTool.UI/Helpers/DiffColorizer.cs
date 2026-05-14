using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlSqlMergeTool.UI.Helpers;

public class DiffColorizer : DocumentColorizingTransformer
{
    private readonly List<HighlightRegion> _regions;

    private static readonly SolidColorBrush AddedLineBrush = new(Color.FromArgb(40, 76, 175, 80));
    private static readonly SolidColorBrush DeletedLineBrush = new(Color.FromArgb(40, 244, 67, 54));
    private static readonly SolidColorBrush ConflictLineBrush = new(Color.FromArgb(50, 255, 152, 0));

    private static readonly SolidColorBrush AddedTextBrush = new(Color.FromArgb(80, 76, 175, 80));
    private static readonly SolidColorBrush DeletedTextBrush = new(Color.FromArgb(80, 244, 67, 54));
    private static readonly SolidColorBrush ConflictTextBrush = new(Color.FromArgb(100, 255, 152, 0));

    public DiffColorizer(List<HighlightRegion> regions)
    {
        _regions = regions ?? new List<HighlightRegion>();
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        if (_regions.Count == 0 || line.Length == 0) return;

        int lineStart = line.Offset;
        int lineEnd = line.Offset + line.Length;

        var intersectingRegions = _regions
            .Where(r => r.StartOffset < lineEnd && (r.StartOffset + r.Length) > lineStart)
            .ToList();

        if (intersectingRegions.Count == 0) return;

        // Apply line background
        var lineChangeType = intersectingRegions.First().Type;
        var lineBrush = GetLineBrush(lineChangeType);

        if (lineBrush != null)
        {
            ChangeLinePart(lineStart, lineEnd, element =>
            {
                element.TextRunProperties.SetBackgroundBrush(lineBrush);
            });
        }

        // Apply text highlighting for specific regions
        foreach (var region in intersectingRegions)
        {
            int start = Math.Max(lineStart, region.StartOffset);
            int end = Math.Min(lineEnd, region.StartOffset + region.Length);

            if (start < end)
            {
                var textBrush = GetTextBrush(region.Type);
                if (textBrush != null)
                {
                    ChangeLinePart(start, end, element =>
                    {
                        element.TextRunProperties.SetBackgroundBrush(textBrush);
                    });
                }
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

    private static SolidColorBrush? GetTextBrush(HighlightType type)
    {
        return type switch
        {
            HighlightType.Added => AddedTextBrush,
            HighlightType.Deleted => DeletedTextBrush,
            HighlightType.Conflict => ConflictTextBrush,
            _ => null
        };
    }
}
