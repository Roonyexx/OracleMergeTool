using PlSqlMergeTool.BLL.Models;
using System.Collections.Generic;

namespace PlSqlMergeTool.BLL.MergeLogic;

public class TokenMergeResult
{
    public List<PlSqlToken> ResolvedTokens { get; set; } = [];
    public bool HasUnresolvedConflicts { get; set; }
}

public interface ITokenMergeAlgorithm
{
    TokenMergeResult MergeTokens(List<PlSqlToken> baseline, List<PlSqlToken> local, List<PlSqlToken> target);
}

public class TokenMergeAlgorithm : ITokenMergeAlgorithm
{
    public TokenMergeResult MergeTokens(List<PlSqlToken> baseline, List<PlSqlToken> local, List<PlSqlToken> target)
    {
        var result = new TokenMergeResult();
        int b = 0, l = 0, t = 0;

        while (l < local.Count || t < target.Count)
        {
            bool hasL = l < local.Count;
            bool hasT = t < target.Count;
            bool hasB = b < baseline.Count;

            var tokL = hasL ? local[l] : null;
            var tokT = hasT ? target[t] : null;
            var tokB = hasB ? baseline[b] : null;

            if (hasL && hasT && tokL!.Equals(tokT))
            {
                result.ResolvedTokens.Add(tokL);
                l++; t++;
                if (hasB && tokB!.Equals(tokL)) b++;
                continue;
            }

            if (hasT && hasB && tokT!.Equals(tokB))
            {
                if (hasL) { result.ResolvedTokens.Add(tokL!); l++; }
                else { t++; b++; }
                continue;
            }

            if (hasL && hasB && tokL!.Equals(tokB))
            {
                if (hasT) { result.ResolvedTokens.Add(tokT!); t++; }
                else { l++; b++; }
                continue;
            }

            result.HasUnresolvedConflicts = true;
            break;
        }

        return result;
    }
}