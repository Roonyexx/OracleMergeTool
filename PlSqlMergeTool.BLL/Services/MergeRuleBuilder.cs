using PlSqlMergeTool.BLL.MergeLogic;
using PlSqlMergeTool.BLL.Services;
namespace PlSqlMergeTool.BLL.Services;

public class MergeRuleBuilder
{
    private readonly List<MergeRuleHandler> _rules = [];

    public MergeRuleBuilder AddNoChangesRule()
    {
        _rules.Add(new NoChangesRule());
        return this;
    }

    public MergeRuleBuilder AddVendorModificationRule()
    {
        _rules.Add(new VendorModificationRule());
        return this;
    }

    public MergeRuleBuilder AddBankModificationRule()
    {
        _rules.Add(new BankModificationRule());
        return this;
    }

    public MergeRuleBuilder AddConflictRule()
    {
        _rules.Add(new ThreeWayMergeRule(new TokenMergeAlgorithm(), new SqlBuilderService()));
        return this;
    }

    public MergeRuleBuilder AddCustomRule(MergeRuleHandler customRule)
    {
        _rules.Add(customRule);
        return this;
    }

    public MergeRuleHandler Build()
    {
        if (_rules.Count == 0)
        {
            throw new InvalidOperationException(
                "No rules configured. Add at least one rule before building the chain.");
        }

        for (int i = 0; i < _rules.Count - 1; i++)
        {
            _rules[i].SetNext(_rules[i + 1]);
        }

        return _rules[0];
    }
}
