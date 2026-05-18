using PlSqlMergeTool.BLL.Models;
using PlSqlMergeTool.BLL.MergeLogic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlSqlMergeTool.UI.Helpers;

public static class DiffMapper
{
    public static List<HighlightRegion> GetLocalRegions(MergeContext context)
    {
        var regions = new List<HighlightRegion>();
        if (context.BaseVsLocalDiff?.DiffBlocks == null || context.Local?.CleanTokens == null) return regions;

        foreach (var block in context.BaseVsLocalDiff.DiffBlocks)
        {
            if (block.InsertCountB > 0)
            {
                var tokens = context.Local.CleanTokens.GetRange(block.InsertStartB, block.InsertCountB);
                
                int firstTokenLine = tokens.First().Line;
                int physicalStart = firstTokenLine;

                if (block.InsertStartB > 0)
                {
                    int prevTokenLine = context.Local.CleanTokens[block.InsertStartB - 1].Line;
                    if (prevTokenLine < firstTokenLine)
                    {
                        physicalStart = prevTokenLine + 1;
                    }
                }

                int exactEnd = tokens.Last().Line;
                regions.Add(new HighlightRegion { StartLine = physicalStart, EndLine = exactEnd, Type = HighlightType.Added });
            }
        }
        return MergeRegions(regions); 
    }

    public static List<HighlightRegion> GetTargetRegions(MergeContext context)
    {
        var regions = new List<HighlightRegion>();
        if (context.BaseVsTargetDiff?.DiffBlocks == null || context.Target?.CleanTokens == null) return regions;

        foreach (var block in context.BaseVsTargetDiff.DiffBlocks)
        {
            if (block.InsertCountB > 0)
            {
                var tokens = context.Target.CleanTokens.GetRange(block.InsertStartB, block.InsertCountB);
                
                int firstTokenLine = tokens.First().Line;
                int physicalStart = firstTokenLine;

                if (block.InsertStartB > 0)
                {
                    int prevTokenLine = context.Target.CleanTokens[block.InsertStartB - 1].Line;
                    if (prevTokenLine < firstTokenLine)
                    {
                        physicalStart = prevTokenLine + 1;
                    }
                }

                int exactEnd = tokens.Last().Line;
                regions.Add(new HighlightRegion { StartLine = physicalStart, EndLine = exactEnd, Type = HighlightType.Added });
            }
        }
        return MergeRegions(regions);
    }

    public static List<HighlightRegion> GetResolvedRegions(MergeContext context)
    {   
        var regions = new List<HighlightRegion>();
        if (context.ResolvedRegions == null || context.ResolvedRegions.Count == 0) return regions;

        foreach (var r in context.ResolvedRegions)
        {
            var type = r.Source switch
            {
                MergeSource.Local => HighlightType.ResolvedFromLocal,
                MergeSource.Target => HighlightType.ResolvedFromTarget,
                _ => HighlightType.None
            };
            
            if (type != HighlightType.None)
            {
                regions.Add(new HighlightRegion
                {
                    StartLine = r.StartLine,
                    EndLine = r.EndLine,
                    Type = type
                });
            }
        }
        
        return MergeRegions(regions);
    }

    private static List<HighlightRegion> MergeRegions(List<HighlightRegion> regions)
    {
        if (regions.Count == 0) return regions;
        
        var merged = new List<HighlightRegion>();
        var current = regions.OrderBy(r => r.StartLine).First();

        foreach (var next in regions.OrderBy(r => r.StartLine).Skip(1))
        {
            if (next.StartLine <= current.EndLine + 1 && current.Type == next.Type)
            {
                current.EndLine = Math.Max(current.EndLine, next.EndLine);
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

    private class SyncSpan
    {
        public int BaseStartLine { get; set; }
        public int BaseEndLine { get; set; }
        public int LocalStartLine { get; set; }
        public int LocalEndLine { get; set; }
        public int TargetStartLine { get; set; }
        public int TargetEndLine { get; set; }
        public bool HasLocal { get; set; }
        public bool HasTarget { get; set; }
    }

    public static List<SyncBlock> BuildSyncBlocks(MergeContext context)
    {
        var syncBlocks = new List<SyncBlock>();
        var spans = new List<SyncSpan>();

        var baseTokens = context.Baseline?.CleanTokens;
        var localTokens = context.Local?.CleanTokens;
        var targetTokens = context.Target?.CleanTokens;

        if (baseTokens == null || localTokens == null || targetTokens == null) 
            return syncBlocks;

        if (context.BaseVsLocalDiff?.DiffBlocks != null)
        {
            foreach (var block in context.BaseVsLocalDiff.DiffBlocks)
            {
                var baseRange = GetPhysicalRange(baseTokens, block.DeleteStartA, block.DeleteCountA);
                var localRange = GetPhysicalRange(localTokens, block.InsertStartB, block.InsertCountB);

                spans.Add(new SyncSpan
                {
                    BaseStartLine = baseRange.StartLine,
                    BaseEndLine = baseRange.EndLine,
                    LocalStartLine = localRange.StartLine,
                    LocalEndLine = localRange.EndLine,
                    HasLocal = true
                });
            }
        }

        if (context.BaseVsTargetDiff?.DiffBlocks != null)
        {
            foreach (var block in context.BaseVsTargetDiff.DiffBlocks)
            {
                var baseRange = GetPhysicalRange(baseTokens, block.DeleteStartA, block.DeleteCountA);
                var targetRange = GetPhysicalRange(targetTokens, block.InsertStartB, block.InsertCountB);

                spans.Add(new SyncSpan
                {
                    BaseStartLine = baseRange.StartLine,
                    BaseEndLine = baseRange.EndLine,
                    TargetStartLine = targetRange.StartLine,
                    TargetEndLine = targetRange.EndLine,
                    HasTarget = true
                });
            }
        }

        if (spans.Count == 0) return syncBlocks;

        spans.Sort((a, b) => a.BaseStartLine.CompareTo(b.BaseStartLine));

        var mergedSpans = new List<SyncSpan>();
        var current = spans[0];

        for (int i = 1; i < spans.Count; i++)
        {
            var next = spans[i];

            if (next.BaseStartLine <= Math.Max(current.BaseEndLine, current.BaseStartLine))
            {
                current.BaseEndLine = Math.Max(current.BaseEndLine, next.BaseEndLine);

                if (next.HasLocal)
                {
                    if (current.HasLocal)
                    {
                        current.LocalStartLine = Math.Min(current.LocalStartLine, next.LocalStartLine);
                        current.LocalEndLine = Math.Max(current.LocalEndLine, next.LocalEndLine);
                    }
                    else
                    {
                        current.LocalStartLine = next.LocalStartLine;
                        current.LocalEndLine = next.LocalEndLine;
                        current.HasLocal = true;
                    }
                }

                if (next.HasTarget)
                {
                    if (current.HasTarget)
                    {
                        current.TargetStartLine = Math.Min(current.TargetStartLine, next.TargetStartLine);
                        current.TargetEndLine = Math.Max(current.TargetEndLine, next.TargetEndLine);
                    }
                    else
                    {
                        current.TargetStartLine = next.TargetStartLine;
                        current.TargetEndLine = next.TargetEndLine;
                        current.HasTarget = true;
                    }
                }
            }
            else
            {
                mergedSpans.Add(current);
                current = next;
            }
        }
        mergedSpans.Add(current);

        int localDelta = 0;
        int targetDelta = 0;

        foreach (var span in mergedSpans)
        {
            int baseCount = Math.Max(0, span.BaseEndLine - span.BaseStartLine + 1);

            int localInsertAfter, localCount;
            if (span.HasLocal)
            {
                localInsertAfter = span.LocalStartLine - 1;
                localCount = Math.Max(0, span.LocalEndLine - span.LocalStartLine + 1);
                localDelta += localCount - baseCount; // Обновляем смещение Банка
            }
            else
            {
                localInsertAfter = span.BaseStartLine - 1 + localDelta;
                localCount = baseCount;
            }

            int targetInsertAfter, targetCount;
            if (span.HasTarget)
            {
                targetInsertAfter = span.TargetStartLine - 1;
                targetCount = Math.Max(0, span.TargetEndLine - span.TargetStartLine + 1);
                targetDelta += targetCount - baseCount; // Обновляем смещение Вендора
            }
            else
            {
                targetInsertAfter = span.BaseStartLine - 1 + targetDelta;
                targetCount = baseCount;
            }

            int resolvedCount = Math.Max(localCount, targetCount);
            int resolvedInsertAfter = Math.Max(localInsertAfter, targetInsertAfter); 

            syncBlocks.Add(new SyncBlock
            {
                LocalInsertAfterLine = localInsertAfter,
                TargetInsertAfterLine = targetInsertAfter,
                ResolvedInsertAfterLine = resolvedInsertAfter,
                LocalLineCount = localCount,
                TargetLineCount = targetCount,
                ResolvedLineCount = resolvedCount
            });
        }

        return syncBlocks;
    }

    private static (int StartLine, int EndLine) GetPhysicalRange(List<PlSqlToken> cleanTokens, int startIndex, int count)
    {
        if (cleanTokens == null || cleanTokens.Count == 0) return (1, 0);

        if (count == 0)
        {
            int line = startIndex > 0 && startIndex <= cleanTokens.Count 
                ? cleanTokens[startIndex - 1].Line + 1 
                : 1;
            return (line, line - 1);
        }

        int firstTokenLine = cleanTokens[startIndex].Line;
        int startLine = firstTokenLine;

        if (startIndex > 0 && startIndex < cleanTokens.Count)
        {
            int prevTokenLine = cleanTokens[startIndex - 1].Line;
            if (prevTokenLine < firstTokenLine)
            {
                startLine = prevTokenLine + 1;
            }
        }

        int lastIndex = startIndex + count - 1;
        int endLine = 1;
        
        if (lastIndex >= 0 && lastIndex < cleanTokens.Count)
        {
            endLine = cleanTokens[lastIndex].Line;
        }
        else if (lastIndex >= cleanTokens.Count)
        {
            endLine = cleanTokens.Last().Line;
        }

        if (endLine < startLine) endLine = startLine;

        return (startLine, endLine);
    }
}