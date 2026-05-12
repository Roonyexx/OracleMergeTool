using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlSqlMergeTool.UI.Helpers;

public class DiffColorizer(List<HighlightRegion> regions) : DocumentColorizingTransformer
{
    private readonly List<HighlightRegion> _regions = regions;

    protected override void ColorizeLine(DocumentLine line)
    {
        if (_regions == null || _regions.Count == 0 || line.Length == 0) return;

        int lineStart = line.Offset;
        int lineEnd = line.Offset + line.Length;

        var intersectingRegions = _regions.Where(r => 
            r.StartOffset < lineEnd && 
            (r.StartOffset + r.Length) > lineStart).ToList();

        if (intersectingRegions.Count == 0) return;


        var lineChangeType = intersectingRegions.First().Type; 

        ChangeLinePart(lineStart, lineEnd, element =>
        {
            if (lineChangeType == HighlightType.Added)
                element.TextRunProperties.SetBackgroundBrush(new SolidColorBrush(Color.FromArgb(30, 0, 255, 0)));
            else if (lineChangeType == HighlightType.Deleted)
                element.TextRunProperties.SetBackgroundBrush(new SolidColorBrush(Color.FromArgb(30, 255, 0, 0)));
            else if (lineChangeType == HighlightType.Conflict)
                element.TextRunProperties.SetBackgroundBrush(new SolidColorBrush(Color.FromArgb(40, 255, 165, 0)));
        });

        foreach (var region in intersectingRegions)
        {
            int start = Math.Max(lineStart, region.StartOffset);
            int end = Math.Min(lineEnd, region.StartOffset + region.Length);

            if (start < end)
            {
                ChangeLinePart(start, end, element =>
                {
                    element.TextRunProperties.SetTextDecorations(TextDecorations.Underline);
                    
                    // if (region.Type == HighlightType.Added)
                    //     element.TextRunProperties.SetBackgroundBrush(new SolidColorBrush(Color.FromArgb(80, 0, 255, 0))); 
                    // else if (region.Type == HighlightType.Deleted)
                    //     element.TextRunProperties.SetBackgroundBrush(new SolidColorBrush(Color.FromArgb(80, 255, 0, 0)));
                    // else if (region.Type == HighlightType.Conflict)
                    //     element.TextRunProperties.SetBackgroundBrush(new SolidColorBrush(Color.FromArgb(100, 255, 165, 0)));
                });
            }
        }
    }
}