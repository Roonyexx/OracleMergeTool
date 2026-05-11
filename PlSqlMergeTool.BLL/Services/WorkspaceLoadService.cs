using PlSqlMergeTool.BLL.Interfaces;
using PlSqlMergeTool.BLL.MergeLogic;
using PlSqlMergeTool.BLL.Models;

namespace PlSqlMergeTool.BLL.Services;

public class WorkspaceLoadService(IOracleRepository repository, SqlDifferService differService)
{
    private readonly IOracleRepository _repository = repository;
    private readonly SqlDifferService _differService = differService;

    public async Task<List<MergeContext>> LoadAsync(WorkspaceConnectionConfig config)
    {
        var contexts = new List<MergeContext>();

        var packageNames = await Task.Run(() => _repository.GetPackageNames(config.LocalConnection));

        foreach (var name in packageNames)
        {
            var baselineSql = await Task.Run(() => _repository.GetPackageSource(config.BaselineConnection, name));
            var localSql = await Task.Run(() => _repository.GetPackageSource(config.LocalConnection, name));
            var targetSql = await Task.Run(() => _repository.GetPackageSource(config.TargetConnection, name));

            var context = _differService.Diff(
                filename: name,
                baselineSql: baselineSql,
                localSql: localSql,
                targetSql: targetSql
            );

            contexts.Add(context);
        }

        return contexts;
    }

}