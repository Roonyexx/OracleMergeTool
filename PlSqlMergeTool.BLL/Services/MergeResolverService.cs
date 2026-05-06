using PlSqlMergeTool.BLL.MergeLogic;

namespace PlSqlMergeTool.BLL.Services;

public class MergeResolverService
{
    private MergeRuleHandler? _ruleChain;

    public MergeResolverService ConfigureRules(Action<MergeRuleBuilder> configure)
    {
        var builder = new MergeRuleBuilder();
        configure(builder);
        _ruleChain = builder.Build();
        return this;
    }

    public void Resolve(MergeContext context)
    {
        if (_ruleChain == null)
        {
            throw new InvalidOperationException(
                "Rules not configured. Call ConfigureRules() before Resolve()");
        }

        if (context.Status != MergeStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Context already resolved with status {context.Status}");
        }

        _ruleChain.Handle(context);

        if (context.Status == MergeStatus.Pending)
        {
            throw new InvalidOperationException(
                "No rule resolved the merge context. Ensure the chain includes a terminal rule.");
        }
    }
}
