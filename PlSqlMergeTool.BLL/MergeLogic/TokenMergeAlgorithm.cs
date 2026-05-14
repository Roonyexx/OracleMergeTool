using PlSqlMergeTool.BLL.Models;
using System.Collections.Generic;
using System.Linq;

namespace PlSqlMergeTool.BLL.MergeLogic;

public class TokenMergeResult
{
    public List<PlSqlToken> ResolvedTokens { get; set; } = [];
    public bool HasUnresolvedConflicts { get; set; }    
}

public class MergeEdit
{
    public int BaseStart { get; init; }
    public int BaseCount { get; init; }
    public required List<PlSqlToken> InsertTokens { get; init; }
}

public interface ITokenMergeAlgorithm
{
    TokenMergeResult MergeTokens(MergeContext context);
}

public class TokenMergeAlgorithm : ITokenMergeAlgorithm
{
    public TokenMergeResult MergeTokens(MergeContext context)
    {
        var localEdits = GetEdits(context.BaseVsLocalDiff.DiffBlocks, context.Local.CleanTokens);
        var targetEdits = GetEdits(context.BaseVsTargetDiff.DiffBlocks, context.Target.CleanTokens);

        foreach (var l in localEdits)
        {
            foreach (var t in targetEdits)
            {
                bool overlap = l.BaseStart <= (t.BaseStart + t.BaseCount) && 
                               t.BaseStart <= (l.BaseStart + l.BaseCount);

                if (overlap)
                {
                    if (AreEditsIdentical(l, t)) continue; 
                    
                    return new TokenMergeResult { HasUnresolvedConflicts = true };
                }
            }
        }

        var allEdits = new List<MergeEdit>(localEdits);
        foreach (var t in targetEdits)
        {
            if (!localEdits.Any(l => AreEditsIdentical(l, t)))
                allEdits.Add(t);
        }

        allEdits.Sort((a, b) => b.BaseStart.CompareTo(a.BaseStart));

        var resolved = new List<PlSqlToken>(context.Baseline.CleanTokens);
        foreach (var edit in allEdits)
        {
            resolved.RemoveRange(edit.BaseStart, edit.BaseCount);
            resolved.InsertRange(edit.BaseStart, edit.InsertTokens);
        }

        return new TokenMergeResult { ResolvedTokens = resolved };
    }

    private List<MergeEdit> GetEdits(IList<DiffPlex.Model.DiffBlock> blocks, List<PlSqlToken> sourceTokens)
    {
        return blocks.Select(b => new MergeEdit
        {
            BaseStart = b.DeleteStartA,
            BaseCount = b.DeleteCountA,
            InsertTokens = sourceTokens.GetRange(b.InsertStartB, b.InsertCountB)
        }).ToList();
    }

    private bool AreEditsIdentical(MergeEdit a, MergeEdit b)
    {
        if (a.BaseStart != b.BaseStart || a.BaseCount != b.BaseCount || a.InsertTokens.Count != b.InsertTokens.Count) 
            return false;
            
        for (int i = 0; i < a.InsertTokens.Count; i++)
        {
            if (!a.InsertTokens[i].Equals(b.InsertTokens[i])) return false;
        }
        return true;
    }
}