using PlSqlMergeTool.BLL.Models;
using System.Text;

namespace PlSqlMergeTool.BLL.Services;

public class SqlBuilderService
{
    public string BuildSql(IEnumerable<PlSqlToken> resolvedCleanTokens)
    {
        var sb = new StringBuilder();

        foreach (var token in resolvedCleanTokens)
        {
            if (token.LeadingTrivia != null)
            {
                foreach (var trivia in token.LeadingTrivia)
                {
                    sb.Append(trivia.Text);
                }
            }

            sb.Append(token.Text);
        }

        return sb.ToString();
    }
}