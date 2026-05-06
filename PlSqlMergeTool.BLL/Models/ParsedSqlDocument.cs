namespace PlSqlMergeTool.BLL.Models;

public class ParsedSqlDocument
{
    public string FileName { get; set; } = string.Empty;
    public string OriginalText { get; set; } = string.Empty;
    public List<PlSqlToken> RawTokens { get; set; } = [];
    public List<PlSqlToken> CleanTokens { get; set; } = [];
}