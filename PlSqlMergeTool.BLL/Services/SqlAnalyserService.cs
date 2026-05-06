using System;
using PlSqlMergeTool.BLL.Models;
using PlSqlMergeTool.BLL.LexicalAnalysis;
using System.Runtime.CompilerServices;

namespace PlSqlMergeTool.BLL.Services;

public class SqlAnalyserService(TokenFilter tokenFilter)
{
    private TokenFilter _tokenFilter = tokenFilter;

    public ParsedSqlDocument Tokenize(string sqlText)
    {
        var scanner = new Scanner(sqlText);
        var rawTokens = new List<PlSqlToken>();
        PlSqlToken? token;

        while ((token = scanner.GetNextToken()) != null)
        {
            rawTokens.Add(token);
        }

        var cleanTokens = _tokenFilter.FilterTokens(rawTokens).ToList();
        return new ParsedSqlDocument
        {
            OriginalText = sqlText,
            RawTokens = rawTokens,
            CleanTokens = cleanTokens
        };
    }

    public (ParsedSqlDocument Baseline, 
            ParsedSqlDocument Local, 
            ParsedSqlDocument Target) 
    TokenizeSchemas(string baselineSql, string localSql, string targetSql)
    {
        var baselineDoc = Tokenize(baselineSql);
        var localDoc = Tokenize(localSql);
        var targetDoc = Tokenize(targetSql);
        
        return (baselineDoc, localDoc, targetDoc);
    }
}



