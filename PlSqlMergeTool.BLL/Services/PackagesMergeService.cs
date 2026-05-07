using PlSqlMergeTool.BLL.MergeLogic;
namespace PlSqlMergeTool.BLL.Services;

class PackagesMergeService(SqlDifferService sqlDifferService, MergeResolverService mergeResolverService)
{
    private readonly SqlDifferService _sqlDifferService = sqlDifferService;
    private readonly MergeResolverService _mergeResolverService = mergeResolverService;

    public MergeContext Merge(string filename, string baselineSql, string localSql, string targetSql)
    {
        var context = _sqlDifferService.Diff(filename, baselineSql, localSql, targetSql);
        _mergeResolverService.Resolve(context);
        return context;
    }
}