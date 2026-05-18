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

        foreach (var block in blocks)
        {
            int maxLines = Math.Max(block.LocalLineCount, Math.Max(block.TargetLineCount, block.ResolvedLineCount));

            int localGap = maxLines - block.LocalLineCount;
            if (localGap > 0)
            {
                AddGap(gaps.LocalGaps, block.LocalInsertAfterLine, localGap);
            }

            int targetGap = maxLines - block.TargetLineCount;
            if (targetGap > 0)
            {
                AddGap(gaps.TargetGaps, block.TargetInsertAfterLine, targetGap);
            }

            int resolvedGap = maxLines - block.ResolvedLineCount;
            if (resolvedGap > 0)
            {
                AddGap(gaps.ResolvedGaps, block.ResolvedInsertAfterLine, resolvedGap);
            }
        }

        return gaps;
    }

    private void AddGap(Dictionary<int, int> gapsDict, int line, int count)
    {
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