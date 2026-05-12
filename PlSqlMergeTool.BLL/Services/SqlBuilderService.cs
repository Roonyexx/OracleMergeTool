using PlSqlMergeTool.BLL.Models;
using System.Collections.Generic;
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
                    trivia.Offset = sb.Length;
                    sb.Append(trivia.Text);
                }
            }

            token.Offset = sb.Length;
            sb.Append(token.Text);

            if (token.TrailingTrivia != null)
            {
                foreach (var trivia in token.TrailingTrivia)
                {
                    trivia.Offset = sb.Length;
                    sb.Append(trivia.Text);
                }
            }
        }

        return sb.ToString();
    }
}