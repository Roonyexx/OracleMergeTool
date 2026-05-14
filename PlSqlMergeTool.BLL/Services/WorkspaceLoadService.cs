using PlSqlMergeTool.BLL.Interfaces;
using PlSqlMergeTool.BLL.MergeLogic;
using PlSqlMergeTool.BLL.Models;

namespace PlSqlMergeTool.BLL.Services;

public class WorkspaceLoadService(IOracleRepository repository, SqlDifferService differService, DdlAnalysisService ddlAnalysisService)
{
    private readonly IOracleRepository _repository = repository;
    private readonly SqlDifferService _differService = differService;
    private readonly DdlAnalysisService _ddlAnalysisService = ddlAnalysisService;

    public async Task<DdlAnalysisReport> LoadDdlReportAsync(WorkspaceConnectionConfig config)
    {
        var localDdlTask = Task.Run(() => _repository.GetSchemaTablesMetadata(config.LocalConnection));
        var targetDdlTask = Task.Run(() => _repository.GetSchemaTablesMetadata(config.TargetConnection));

        await Task.WhenAll(localDdlTask, targetDdlTask);

        return _ddlAnalysisService.AnalyzeDdlChanges(localDdlTask.Result, targetDdlTask.Result);
    }

    public async Task<List<MergeContext>> LoadPackagesAsync(WorkspaceConnectionConfig config)
    {
        var contexts = new List<MergeContext>();
        var packageNames = await Task.Run(() => _repository.GetPackageNames(config.LocalConnection));

        var options = new ParallelOptions { MaxDegreeOfParallelism = 10 };

        await Parallel.ForEachAsync(packageNames, options, async (name, ct) =>
        {
            var baselineSqlTask = Task.Run(() => _repository.GetPackageSource(config.BaselineConnection, name));
            var localSqlTask = Task.Run(() => _repository.GetPackageSource(config.LocalConnection, name));
            var targetSqlTask = Task.Run(() => _repository.GetPackageSource(config.TargetConnection, name));

            await Task.WhenAll(baselineSqlTask, localSqlTask, targetSqlTask);

            var context = _differService.Diff(
                filename: name,
                baselineSql: baselineSqlTask.Result,
                localSql: localSqlTask.Result,
                targetSql: targetSqlTask.Result
            );

            lock (contexts)
            {
                // if(name == "TEST_CALCULATOR")
                // {
                //     Console.WriteLine($"Loaded package: {context.Baseline.OriginalText}");
                //     Console.WriteLine($"Loaded package: {context.Local.OriginalText}");
                //     Console.WriteLine($"Loaded package: {context.Target.OriginalText}");
                // }

                contexts.Add(context);
            }
        });

        return contexts;
    }

}