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
    public override void Handle(MergeContext context)
    {
        if (!context.HasLocalChanges && context.HasTargetChanges)
        {
            context.ResolveWith(MergeStatus.AutoTarget, context.Target.OriginalText);
            return;
        }

        base.Handle(context);
    }
}

public class BankModificationRule : MergeRuleHandler
{
    public override void Handle(MergeContext context)
    {
        if (context.HasLocalChanges && !context.HasTargetChanges)
        {
            context.ResolveWith(MergeStatus.AutoLocal, context.Local.OriginalText);
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
            var mergeResult = _mergeAlgorithm.MergeTokens(
                context.Baseline.CleanTokens, 
                context.Local.CleanTokens, 
                context.Target.CleanTokens
            );

            if (mergeResult.HasUnresolvedConflicts)
            {
                context.MarkAsConflict();
                return;
            }

            string resolvedCode = _builderService.BuildSql(mergeResult.ResolvedTokens);
            
            context.ResolveWith(MergeStatus.AutoMerged, resolvedCode);
            return;
        }

        base.Handle(context);
    }
}