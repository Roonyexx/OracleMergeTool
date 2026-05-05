using System;
using System.Diagnostics.CodeAnalysis;
using PlSqlMergeTool.BLL.LexicalAnalysis;
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
    
    public IEnumerable<PlSqlToken> FilterTokens(IEnumerable<PlSqlToken> tokens)
    {
        return tokens.Where(t => !_excludedTypes.Contains(t.Type));
    }
}