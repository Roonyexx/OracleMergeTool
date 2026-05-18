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

    private static int[] MapBaseToSide(int baseCount, DiffPlex.Model.DiffResult diff)
    {
        int[] map = new int[baseCount];
        for (int i = 0; i < baseCount; i++) map[i] = -1;

        if (diff == null || diff.DiffBlocks == null) 
        {
            for (int i = 0; i < baseCount; i++) map[i] = i;
            return map;
        }

        int baseIdx = 0;
        int sideIdx = 0;

        foreach (var block in diff.DiffBlocks)
        {
            while (baseIdx < block.DeleteStartA)
            {
                if (baseIdx < baseCount) map[baseIdx] = sideIdx;
                baseIdx++;
                sideIdx++;
            }

            for (int i = 0; i < block.DeleteCountA; i++)
            {
                if (baseIdx < baseCount) map[baseIdx] = -1;
                baseIdx++;
            }

            sideIdx += block.InsertCountB;
        }

        while (baseIdx < baseCount)
        {
            map[baseIdx] = sideIdx;
            baseIdx++;
            sideIdx++;
        }

        return map;
    }

    private static (int StartIdx, int EndIdx) GetIndexRange(int[] map, int baseStartIdx, int baseEndIdx, int sideTokenCount)
    {
        if (baseStartIdx >= map.Length) 
            return (sideTokenCount, sideTokenCount - 1);

        if (baseStartIdx > baseEndIdx)
        {
            int idx = map[baseStartIdx];
            if (idx == -1) idx = FindNearestMappedIndex(map, baseStartIdx, sideTokenCount);
            return (idx, idx - 1);
        }

        int start = -1;
        for (int i = baseStartIdx; i <= baseEndIdx; i++)
        {
            if (map[i] != -1) { start = map[i]; break; }
        }
        if (start == -1) start = FindNearestMappedIndex(map, baseStartIdx, sideTokenCount);

        int end = -1;
        for (int i = baseEndIdx; i >= baseStartIdx; i--)
        {
            if (map[i] != -1) { end = map[i]; break; }
        }
        if (end == -1) end = start - 1;

        return (start, end);
    }

    private static int FindNearestMappedIndex(int[] map, int baseIdx, int sideTokenCount)
    {
        for (int i = baseIdx; i < map.Length; i++)
            if (map[i] != -1) return map[i];
        return sideTokenCount;
    }

    private class TokenSpan
    {
        public int BaseStartIdx { get; set; }
        public int BaseEndIdx { get; set; }
        public int LocalStartIdx { get; set; }
        public int LocalEndIdx { get; set; }
        public int TargetStartIdx { get; set; }
        public int TargetEndIdx { get; set; }
        public int ResolvedStartIdx { get; set; }
        public int ResolvedEndIdx { get; set; }
        public bool HasLocal { get; set; }
        public bool HasTarget { get; set; }
        public bool HasResolved { get; set; }
    }

    public static List<SyncBlock> BuildSyncBlocks(MergeContext context)
    {
        var syncBlocks = new List<SyncBlock>();
        
        var baseTokens = context.Baseline?.CleanTokens;
        var localTokens = context.Local?.CleanTokens;
        var targetTokens = context.Target?.CleanTokens;
        var resolvedTokens = context.Resolved?.CleanTokens;

        if (baseTokens == null || localTokens == null || targetTokens == null || resolvedTokens == null) 
            return syncBlocks;

        int baseCount = baseTokens.Count;
        int[] mapLocal = MapBaseToSide(baseCount, context.BaseVsLocalDiff);
        int[] mapTarget = MapBaseToSide(baseCount, context.BaseVsTargetDiff);
        int[] mapResolved = MapBaseToSide(baseCount, context.BaseVsResolvedDiff);

        var spans = new List<TokenSpan>();

        if (context.BaseVsLocalDiff?.DiffBlocks != null)
        {
            foreach (var block in context.BaseVsLocalDiff.DiffBlocks)
            {
                spans.Add(new TokenSpan
                {
                    BaseStartIdx = block.DeleteStartA,
                    BaseEndIdx = block.DeleteStartA + block.DeleteCountA - 1,
                    LocalStartIdx = block.InsertStartB,
                    LocalEndIdx = block.InsertStartB + block.InsertCountB - 1,
                    HasLocal = true
                });
            }
        }

        if (context.BaseVsTargetDiff?.DiffBlocks != null)
        {
            foreach (var block in context.BaseVsTargetDiff.DiffBlocks)
            {
                spans.Add(new TokenSpan
                {
                    BaseStartIdx = block.DeleteStartA,
                    BaseEndIdx = block.DeleteStartA + block.DeleteCountA - 1,
                    TargetStartIdx = block.InsertStartB,
                    TargetEndIdx = block.InsertStartB + block.InsertCountB - 1,
                    HasTarget = true
                });
            }
        }

        if (context.BaseVsResolvedDiff?.DiffBlocks != null)
        {
            foreach (var block in context.BaseVsResolvedDiff.DiffBlocks)
            {
                spans.Add(new TokenSpan
                {
                    BaseStartIdx = block.DeleteStartA,
                    BaseEndIdx = block.DeleteStartA + block.DeleteCountA - 1,
                    ResolvedStartIdx = block.InsertStartB,
                    ResolvedEndIdx = block.InsertStartB + block.InsertCountB - 1,
                    HasResolved = true
                });
            }
        }

        spans.Sort((a, b) => a.BaseStartIdx.CompareTo(b.BaseStartIdx));

        var mergedSpans = new List<TokenSpan>();
        if (spans.Count > 0)
        {
            var current = spans[0];
            for (int i = 1; i < spans.Count; i++)
            {
                var next = spans[i];
                if (next.BaseStartIdx <= current.BaseEndIdx + 1)
                {
                    current.BaseEndIdx = Math.Max(current.BaseEndIdx, next.BaseEndIdx);
                    if (next.HasLocal)
                    {
                        if (current.HasLocal) { current.LocalStartIdx = Math.Min(current.LocalStartIdx, next.LocalStartIdx); current.LocalEndIdx = Math.Max(current.LocalEndIdx, next.LocalEndIdx); }
                        else { current.LocalStartIdx = next.LocalStartIdx; current.LocalEndIdx = next.LocalEndIdx; current.HasLocal = true; }
                    }
                    if (next.HasTarget)
                    {
                        if (current.HasTarget) { current.TargetStartIdx = Math.Min(current.TargetStartIdx, next.TargetStartIdx); current.TargetEndIdx = Math.Max(current.TargetEndIdx, next.TargetEndIdx); }
                        else { current.TargetStartIdx = next.TargetStartIdx; current.TargetEndIdx = next.TargetEndIdx; current.HasTarget = true; }
                    }
                    if (next.HasResolved)
                    {
                        if (current.HasResolved) { current.ResolvedStartIdx = Math.Min(current.ResolvedStartIdx, next.ResolvedStartIdx); current.ResolvedEndIdx = Math.Max(current.ResolvedEndIdx, next.ResolvedEndIdx); }
                        else { current.ResolvedStartIdx = next.ResolvedStartIdx; current.ResolvedEndIdx = next.ResolvedEndIdx; current.HasResolved = true; }
                    }
                }
                else
                {
                    mergedSpans.Add(current);
                    current = next;
                }
            }
            mergedSpans.Add(current);
        }

        int lastBaseIdx = 0;

        foreach (var span in mergedSpans)
        {
            // Process unchanged tokens BEFORE this span to find whitespace gaps
            for (int i = lastBaseIdx; i < span.BaseStartIdx; i++)
            {
                int lIdx = mapLocal[i];
                int tIdx = mapTarget[i];
                int rIdx = mapResolved[i];

                if (lIdx != -1 && tIdx != -1 && rIdx != -1)
                {
                    int lLine = localTokens[lIdx].Line;
                    int tLine = targetTokens[tIdx].Line;
                    int rLine = resolvedTokens[rIdx].Line;

                    // We create a zero-length sync block at every unchanged token to act as an alignment anchor!
                    syncBlocks.Add(new SyncBlock
                    {
                        LocalInsertAfterLine = lLine - 1,
                        TargetInsertAfterLine = tLine - 1,
                        ResolvedInsertAfterLine = rLine - 1,
                        LocalLineCount = 0,
                        TargetLineCount = 0,
                        ResolvedLineCount = 0
                    });
                }
            }

            if (!span.HasLocal) { var r = GetIndexRange(mapLocal, span.BaseStartIdx, span.BaseEndIdx, localTokens.Count); span.LocalStartIdx = r.StartIdx; span.LocalEndIdx = r.EndIdx; }
            if (!span.HasTarget) { var r = GetIndexRange(mapTarget, span.BaseStartIdx, span.BaseEndIdx, targetTokens.Count); span.TargetStartIdx = r.StartIdx; span.TargetEndIdx = r.EndIdx; }
            if (!span.HasResolved) { var r = GetIndexRange(mapResolved, span.BaseStartIdx, span.BaseEndIdx, resolvedTokens.Count); span.ResolvedStartIdx = r.StartIdx; span.ResolvedEndIdx = r.EndIdx; }

            var localPhys = GetPhysicalRange(localTokens, span.LocalStartIdx, span.LocalEndIdx - span.LocalStartIdx + 1);
            var targetPhys = GetPhysicalRange(targetTokens, span.TargetStartIdx, span.TargetEndIdx - span.TargetStartIdx + 1);
            var resolvedPhys = GetPhysicalRange(resolvedTokens, span.ResolvedStartIdx, span.ResolvedEndIdx - span.ResolvedStartIdx + 1);

            syncBlocks.Add(new SyncBlock
            {
                LocalInsertAfterLine = localPhys.StartLine - 1,
                TargetInsertAfterLine = targetPhys.StartLine - 1,
                ResolvedInsertAfterLine = resolvedPhys.StartLine - 1,
                LocalLineCount = Math.Max(0, localPhys.EndLine - localPhys.StartLine + 1),
                TargetLineCount = Math.Max(0, targetPhys.EndLine - targetPhys.StartLine + 1),
                ResolvedLineCount = Math.Max(0, resolvedPhys.EndLine - resolvedPhys.StartLine + 1)
            });

            lastBaseIdx = span.BaseEndIdx + 1;
        }

        // Process remaining unchanged tokens at the end of the file
        for (int i = lastBaseIdx; i < baseCount; i++)
        {
            int lIdx = mapLocal[i];
            int tIdx = mapTarget[i];
            int rIdx = mapResolved[i];

            if (lIdx != -1 && tIdx != -1 && rIdx != -1)
            {
                int lLine = localTokens[lIdx].Line;
                int tLine = targetTokens[tIdx].Line;
                int rLine = resolvedTokens[rIdx].Line;

                syncBlocks.Add(new SyncBlock
                {
                    LocalInsertAfterLine = lLine - 1,
                    TargetInsertAfterLine = tLine - 1,
                    ResolvedInsertAfterLine = rLine - 1,
                    LocalLineCount = 0,
                    TargetLineCount = 0,
                    ResolvedLineCount = 0
                });
            }
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