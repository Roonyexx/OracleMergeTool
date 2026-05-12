using System.Collections.Generic;
using PlSqlMergeTool.BLL.Models;

namespace PlSqlMergeTool.BLL.LexicalAnalysis;

public class TokenFilter(IEnumerable<TokenType> excludedTypes)
{
    private readonly HashSet<TokenType> _excludedTypes = [.. excludedTypes];
    
    public void ExcludeTokenType(TokenType type)
    {
        _excludedTypes.Add(type);
    }

    public void IncludeTokenType(TokenType type)
    {
        _excludedTypes.Remove(type);
    }
    
    public List<PlSqlToken> FilterTokens(IEnumerable<PlSqlToken> tokens)
    {
        var cleanTokens = new List<PlSqlToken>();
        var currentTrivia = new List<PlSqlToken>();

        foreach (var token in tokens)
        {
            if (_excludedTypes.Contains(token.Type))
            {
                currentTrivia.Add(token);
            }
            else
            {
                token.LeadingTrivia = [.. currentTrivia];
                currentTrivia.Clear();
                cleanTokens.Add(token);
            }
        }

        if (currentTrivia.Count > 0)
        {
            var eofToken = new PlSqlToken
            {
                Text = "",
                Type = TokenType.Eof,
                LeadingTrivia = currentTrivia
            };
            cleanTokens.Add(eofToken);
        }

        return cleanTokens;
    }
}