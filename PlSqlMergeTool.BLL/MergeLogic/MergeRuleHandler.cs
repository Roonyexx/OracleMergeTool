using PlSqlMergeTool.BLL.Services;
namespace PlSqlMergeTool.BLL.MergeLogic;

public abstract class MergeRuleHandler
{
    protected MergeRuleHandler? Next { get; private set; }

    public MergeRuleHandler SetNext(MergeRuleHandler next)
    {
        Next = next;
        return next;
    }

    public virtual void Handle(MergeContext context)
    {
        Next?.Handle(context);
    }
}

public class NoChangesRule : MergeRuleHandler
{
    public override void Handle(MergeContext context)
    {
        if (!context.HasLocalChanges && !context.HasTargetChanges)
        {
            context.ResolveWith(MergeStatus.NoChanges, context.Baseline.OriginalText);
            return;
        }

        base.Handle(context);
    }
}

public class VendorModificationRule : MergeRuleHandler
{
    private readonly SqlBuilderService _builderService;

    public VendorModificationRule(SqlBuilderService builderService)
    {
        _builderService = builderService;
    }

    public override void Handle(MergeContext context)
    {
        if (!context.HasLocalChanges && context.HasTargetChanges)
        {
            var edits = context.BaseVsTargetDiff.DiffBlocks
                .Where(b => b.InsertCountB > 0)
                .Select(b => new AppliedTokenEdit 
                { 
                    StartTokenIndex = b.InsertStartB, 
                    TokenCount = b.InsertCountB, 
                    Source = MergeSource.Target 
                }).ToList();

            var (resolvedCode, regions) = _builderService.BuildSql(context.Target.CleanTokens, edits);
            context.ResolveWith(MergeStatus.AutoTarget, resolvedCode, regions);
            return;
        }

        base.Handle(context);
    }
}

public class BankModificationRule : MergeRuleHandler
{
    private readonly SqlBuilderService _builderService;

    public BankModificationRule(SqlBuilderService builderService)
    {
        _builderService = builderService;
    }

    public override void Handle(MergeContext context)
    {
        if (context.HasLocalChanges && !context.HasTargetChanges)
        {
            var edits = context.BaseVsLocalDiff.DiffBlocks
                .Where(b => b.InsertCountB > 0)
                .Select(b => new AppliedTokenEdit 
                { 
                    StartTokenIndex = b.InsertStartB, 
                    TokenCount = b.InsertCountB, 
                    Source = MergeSource.Local 
                }).ToList();

            var (resolvedCode, regions) = _builderService.BuildSql(context.Local.CleanTokens, edits);
            context.ResolveWith(MergeStatus.AutoLocal, resolvedCode, regions);
            return;
        }

        base.Handle(context);
    }
}

public class ThreeWayMergeRule : MergeRuleHandler
{
    private readonly ITokenMergeAlgorithm _mergeAlgorithm;
    private readonly SqlBuilderService _builderService;

    public ThreeWayMergeRule(ITokenMergeAlgorithm mergeAlgorithm, SqlBuilderService builderService)
    {
        _mergeAlgorithm = mergeAlgorithm;
        _builderService = builderService;
    }

    public override void Handle(MergeContext context)
    {
        if (context.HasLocalChanges && context.HasTargetChanges)
        {
            var mergeResult = _mergeAlgorithm.MergeTokens(context);

            if (mergeResult.HasUnresolvedConflicts)
            {
                context.MarkAsConflict();
                return;
            }

            var (resolvedCode, regions) = _builderService.BuildSql(mergeResult.ResolvedTokens, mergeResult.AppliedEdits);
            
            context.ResolveWith(MergeStatus.AutoMerged, resolvedCode, regions);
            return;
        }

        base.Handle(context);
    }
}