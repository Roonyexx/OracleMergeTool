using PlSqlMergeTool.BLL.Models;
using System.Collections.Generic;
using PlSqlMergeTool.BLL.MergeLogic;
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
                    Length = next.StartOffset + next.Length - current.StartOffset,
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

    public static List<HighlightRegion> GetLocalRegions(MergeContext context)
    {
        var changedTokens = new List<PlSqlToken>();
        
        foreach (var block in context.BaseVsLocalDiff.DiffBlocks)
        {
            if (block.InsertCountB > 0)
            {
                var tokens = context.Local.CleanTokens.GetRange(block.InsertStartB, block.InsertCountB);
                changedTokens.AddRange(tokens);
            }
        }

        return GetRegionsFromTokens(changedTokens, HighlightType.Added); 
    }


    public static List<HighlightRegion> GetTargetRegions(MergeContext context)
    {
        var changedTokens = new List<PlSqlToken>();

        // Перебираем блоки отличий между Baseline и Target
        foreach (var block in context.BaseVsTargetDiff.DiffBlocks)
        {
            if (block.InsertCountB > 0)
            {
                var tokens = context.Target.CleanTokens.GetRange(block.InsertStartB, block.InsertCountB);
                changedTokens.AddRange(tokens);
            }
        }

        return GetRegionsFromTokens(changedTokens, HighlightType.Added);
    }


    public static List<HighlightRegion> GetResolvedRegions(MergeContext context)
    {
        // Если конфликтов нет, ничего не подсвечиваем в центральном окне
        if (context.Status != MergeStatus.ManualConflict || context.ResolvedCode == null) 
            return new List<HighlightRegion>();

        // todo
        // в TokenMergeResult, можно будет вытягивать пересекующиеся регионы.
        return new List<HighlightRegion>();
    }
}