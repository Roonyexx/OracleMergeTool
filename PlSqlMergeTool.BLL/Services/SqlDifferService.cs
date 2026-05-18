using DiffPlex;
using PlSqlMergeTool.BLL.MergeLogic;
using PlSqlMergeTool.BLL.Models;

namespace PlSqlMergeTool.BLL.Services;

public class SqlDifferService
{
    private readonly SqlAnalyserService _sqlAnalyserService;
    private readonly IDiffer _differ;
    private readonly IChunker _chunker;

    public SqlDifferService(SqlAnalyserService sqlAnalyserService, IDiffer differ, IChunker chunker)
    {
        _sqlAnalyserService = sqlAnalyserService;
        _differ = differ;
        _chunker = chunker;
    }

    public MergeContext Diff(string filename, string baselineSql, string localSql, string targetSql)
    {
        var baseline = _sqlAnalyserService.Tokenize(baselineSql);
        var local = _sqlAnalyserService.Tokenize(localSql);
        var target = _sqlAnalyserService.Tokenize(targetSql);

        string baseJoined = string.Join('\uFFFF', baseline.CleanTokens.Select(t => t.Text));
        string localJoined = string.Join('\uFFFF', local.CleanTokens.Select(t => t.Text));
        string targetJoined = string.Join('\uFFFF', target.CleanTokens.Select(t => t.Text));

        var baseVsLocalDiff = _differ.CreateDiffs(baseJoined, localJoined, false, true, _chunker);
        var baseVsTargetDiff = _differ.CreateDiffs(baseJoined, targetJoined, false, true, _chunker);
        return new MergeContext
        {
            FileName = filename,
            Baseline = baseline,
            Local = local,
            Target = target,
            BaseVsLocalDiff = baseVsLocalDiff,
            BaseVsTargetDiff = baseVsTargetDiff
        };
    }

    public void UpdateResolvedDiff(MergeContext context, string resolvedSql)
    {
        var resolved = _sqlAnalyserService.Tokenize(resolvedSql);
        string baseJoined = string.Join('\uFFFF', context.Baseline.CleanTokens.Select(t => t.Text));
        string resolvedJoined = string.Join('\uFFFF', resolved.CleanTokens.Select(t => t.Text));
        
        var baseVsResolvedDiff = _differ.CreateDiffs(baseJoined, resolvedJoined, false, true, _chunker);
        
        context.Resolved = resolved;
        context.BaseVsResolvedDiff = baseVsResolvedDiff;
    }
}