using PlSqlMergeTool.BLL.Models;
using System.Collections.Generic;
using PlSqlMergeTool.BLL.MergeLogic;
using System.Linq;

namespace PlSqlMergeTool.UI.Helpers;

public static class DiffMapper
{
    public static List<HighlightRegion> GetRegionsFromTokens(IEnumerable<PlSqlToken> tokens, HighlightType targetType)
    {
        if (!tokens.Any()) return new List<HighlightRegion>();

        var changedLines = tokens
            .Select(t => t.Line)
            .Distinct()
            .OrderBy(l => l)
            .ToList();

        var regions = new List<HighlightRegion>();
        int start = changedLines[0];
        int end = changedLines[0];

        for (int i = 1; i < changedLines.Count; i++)
        {
            if (changedLines[i] == end + 1)
            {
                end = changedLines[i];
            }
            else
            {
                regions.Add(new HighlightRegion { StartLine = start, EndLine = 1000, Type = targetType });
                start = changedLines[i];
                end = changedLines[i];
            }
        }
        regions.Add(new HighlightRegion { StartLine = start, EndLine = end, Type = targetType });

        return regions;
    }

    public static List<HighlightRegion> GetLocalRegions(MergeContext context)
    {
        var changedTokens = new List<PlSqlToken>();
        foreach (var block in context.BaseVsLocalDiff.DiffBlocks)
        {
            if (block.InsertCountB > 0)
                changedTokens.AddRange(context.Local.CleanTokens.GetRange(block.InsertStartB, block.InsertCountB));
        }
        return GetRegionsFromTokens(changedTokens, HighlightType.Added); 
    }

    public static List<HighlightRegion> GetTargetRegions(MergeContext context)
    {
        var changedTokens = new List<PlSqlToken>();
        foreach (var block in context.BaseVsTargetDiff.DiffBlocks)
        {
            if (block.InsertCountB > 0)
                changedTokens.AddRange(context.Target.CleanTokens.GetRange(block.InsertStartB, block.InsertCountB));
        }
        return GetRegionsFromTokens(changedTokens, HighlightType.Added);
    }

    public static List<HighlightRegion> GetResolvedRegions(MergeContext context)
    {
        return new List<HighlightRegion>(); 
    }
}