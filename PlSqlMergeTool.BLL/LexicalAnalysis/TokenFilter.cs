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
        var currentLeading = new List<PlSqlToken>();
        PlSqlToken? lastCleanToken = null;
        bool newlineSeen = false;

        foreach (var token in tokens)
        {
            if (_excludedTypes.Contains(token.Type))
            {
                if (token.Text.Contains('\n')) newlineSeen = true;

                if (!newlineSeen && lastCleanToken != null)
                {
                    lastCleanToken.TrailingTrivia ??= [];
                    lastCleanToken.TrailingTrivia.Add(token);
                }
                else
                {
                    currentLeading.Add(token);
                }
            }
            else
            {
                token.LeadingTrivia = currentLeading.Count > 0 ? [.. currentLeading] : null;
                currentLeading.Clear();
                cleanTokens.Add(token);
                lastCleanToken = token;
                newlineSeen = false;
            }
        }

        if (currentLeading.Count > 0)
        {
            cleanTokens.Add(new PlSqlToken
            {
                Text = "",
                Line = lastCleanToken?.Line ?? 0,
                Offset = 0,
                Length = 0,
                Type = TokenType.Eof,
                LeadingTrivia = currentLeading
            });
        }

        return cleanTokens;
    }
}