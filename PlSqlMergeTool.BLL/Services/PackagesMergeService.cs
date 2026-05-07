using PlSqlMergeTool.BLL.MergeLogic;
namespace PlSqlMergeTool.BLL.Services;

class PackagesMergeService(MergeResolverService mergeResolverService)
{
    private readonly MergeResolverService _resolver = mergeResolverService;
    // ссылка на бд должна быть

    public void ProcessPackages(List<MergeContext> packages)
    {
        
        foreach (var context in packages)
        {
            _resolver.Resolve(context);
        }
        // todo сохранить результаты в бд, отобразить пользователю, дать возможность разрешить конфликты вручную и т.д.
        
    }
}