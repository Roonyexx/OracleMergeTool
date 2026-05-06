using System;
using DiffPlex;
using DiffPlex.Chunkers;
using PlSqlMergeTool.BLL.Services;

namespace PlSqlMergeTool.BLL.MergeLogic;

public class TokenChunker(SqlAnalyserService sqlAnalyserService) : IChunker
{
    private readonly SqlAnalyserService _sqlAnalyserService = sqlAnalyserService;
    public string[] Chunk(string text)
    {
        var parsedDoc = _sqlAnalyserService.Tokenize(text);
        return [.. parsedDoc.CleanTokens.Select(t => t.Text)];
    }

    IReadOnlyList<string> IChunker.Chunk(string text)
    {
        return Chunk(text);
    }
}