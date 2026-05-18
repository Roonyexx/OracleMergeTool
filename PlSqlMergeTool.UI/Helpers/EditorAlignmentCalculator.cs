using System;
using System.Collections.Generic;
using System.Linq;

namespace PlSqlMergeTool.UI.Helpers;

public class AlignmentGaps
{
    public Dictionary<int, int> LocalGaps { get; } = new();
    public Dictionary<int, int> TargetGaps { get; } = new();
    public Dictionary<int, int> ResolvedGaps { get; } = new();
}

public class SyncBlock
{
    public int LocalInsertAfterLine { get; set; }
    public int TargetInsertAfterLine { get; set; }
    public int ResolvedInsertAfterLine { get; set; }

    public int LocalLineCount { get; set; }
    public int TargetLineCount { get; set; }
    public int ResolvedLineCount { get; set; }
}

public class EditorAlignmentCalculator
{
    public AlignmentGaps CalculateGaps(IEnumerable<SyncBlock> blocks)
    {
        var gaps = new AlignmentGaps();
        
        int totalLocalGaps = 0;
        int totalTargetGaps = 0;
        int totalResolvedGaps = 0;

        foreach (var block in blocks)
        {
            // 1. Align the START of the block
            int localStartHeight = block.LocalInsertAfterLine + totalLocalGaps;
            int targetStartHeight = block.TargetInsertAfterLine + totalTargetGaps;
            int resolvedStartHeight = block.ResolvedInsertAfterLine + totalResolvedGaps;

            int maxStartHeight = Math.Max(localStartHeight, Math.Max(targetStartHeight, resolvedStartHeight));

            if (localStartHeight < maxStartHeight)
            {
                int diff = maxStartHeight - localStartHeight;
                AddGap(gaps.LocalGaps, block.LocalInsertAfterLine, diff);
                totalLocalGaps += diff;
            }
            if (targetStartHeight < maxStartHeight)
            {
                int diff = maxStartHeight - targetStartHeight;
                AddGap(gaps.TargetGaps, block.TargetInsertAfterLine, diff);
                totalTargetGaps += diff;
            }
            if (resolvedStartHeight < maxStartHeight)
            {
                int diff = maxStartHeight - resolvedStartHeight;
                AddGap(gaps.ResolvedGaps, block.ResolvedInsertAfterLine, diff);
                totalResolvedGaps += diff;
            }

            // 2. Align the END of the block (handle block size differences)
            int localEndHeight = maxStartHeight + block.LocalLineCount;
            int targetEndHeight = maxStartHeight + block.TargetLineCount;
            int resolvedEndHeight = maxStartHeight + block.ResolvedLineCount;

            int maxEndHeight = Math.Max(localEndHeight, Math.Max(targetEndHeight, resolvedEndHeight));

            if (localEndHeight < maxEndHeight)
            {
                int diff = maxEndHeight - localEndHeight;
                AddGap(gaps.LocalGaps, block.LocalInsertAfterLine + block.LocalLineCount, diff);
                totalLocalGaps += diff;
            }
            if (targetEndHeight < maxEndHeight)
            {
                int diff = maxEndHeight - targetEndHeight;
                AddGap(gaps.TargetGaps, block.TargetInsertAfterLine + block.TargetLineCount, diff);
                totalTargetGaps += diff;
            }
            if (resolvedEndHeight < maxEndHeight)
            {
                int diff = maxEndHeight - resolvedEndHeight;
                AddGap(gaps.ResolvedGaps, block.ResolvedInsertAfterLine + block.ResolvedLineCount, diff);
                totalResolvedGaps += diff;
            }
        }

        return gaps;
    }

    private void AddGap(Dictionary<int, int> gapsDict, int line, int count)
    {
        if (count <= 0) return;
        if (gapsDict.ContainsKey(line))
        {
            gapsDict[line] += count;
        }
        else
        {
            gapsDict[line] = count;
        }
    }
}