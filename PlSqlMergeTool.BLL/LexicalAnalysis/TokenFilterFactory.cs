using PlSqlMergeTool.BLL.Models;

namespace PlSqlMergeTool.BLL.LexicalAnalysis;

public static class TokenFilterFactory
{
    public static TokenFilter CreateDefault()
    {
        return new TokenFilter([
            TokenType.Whitespace,
            TokenType.SingleLineComment,
            TokenType.MultiLineComment
        ]);
    }

    public static TokenFilter CreateStrict()
    {
        return new TokenFilter([
            TokenType.Whitespace
        ]);
    }

    public static TokenFilter CreateCustom(params TokenType[] excludedTypes)
    {
        return new TokenFilter(excludedTypes);
    }
}
