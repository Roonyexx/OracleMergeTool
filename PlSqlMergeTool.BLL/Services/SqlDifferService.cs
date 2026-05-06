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

        var baseVsLocalDiff = _differ.CreateDiffs(baseline.OriginalText, local.OriginalText, false, true, _chunker);
        var baseVsTargetDiff = _differ.CreateDiffs(baseline.OriginalText, target.OriginalText, false, true, _chunker);
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
}