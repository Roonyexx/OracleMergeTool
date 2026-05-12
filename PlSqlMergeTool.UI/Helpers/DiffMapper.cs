using PlSqlMergeTool.BLL.Models;
using System.Collections.Generic;
using System.Linq;

namespace PlSqlMergeTool.UI.Helpers;

public static class DiffMapper
{
    public static List<HighlightRegion> GetRegionsFromTokens(
        IEnumerable<PlSqlToken> tokens, 
        HighlightType targetType)
    {
        var regions = tokens
            .OrderBy(t => t.Offset)
            .Select(t => new HighlightRegion
            {
                StartOffset = t.Offset,
                Length = t.Length,
                Type = targetType
            })
            .ToList();

        return MergeAdjacentRegions(regions);
    }

    private static List<HighlightRegion> MergeAdjacentRegions(List<HighlightRegion> regions)
    {
        if (regions.Count == 0) return regions;

        var merged = new List<HighlightRegion>();
        var current = regions[0];

        for (int i = 1; i < regions.Count; i++)
        {
            var next = regions[i];
            
            if (current.StartOffset + current.Length >= next.StartOffset - 5) 
            {
                current = new HighlightRegion
                {
                    StartOffset = current.StartOffset,
                    Length = (next.StartOffset + next.Length) - current.StartOffset,
                    Type = current.Type
                };
            }
            else
            {
                merged.Add(current);
                current = next;
            }
        }
        merged.Add(current);

        return merged;
    }
}