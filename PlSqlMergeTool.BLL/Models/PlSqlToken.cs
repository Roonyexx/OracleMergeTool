
namespace PlSqlMergeTool.BLL.Models;


public enum TokenType
{
    Whitespace,
    SingleLineComment,
    MultiLineComment,
    String,
    Number,
    Word,
    Operator,
    Delimiter,
    Unknown,
    Eof
}
public class PlSqlToken : IEquatable<PlSqlToken>
{
    public string Text { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Offset { get; set; }
    public int Length  { get; set; }
    public TokenType Type { get; set; }

    public List<PlSqlToken> LeadingTrivia { get; set; } = [];

    public bool Equals(PlSqlToken? other)
    {
        if (other is null) return false;
        return Text.Equals(other.Text, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        return Text.ToLower().GetHashCode();
    }
}