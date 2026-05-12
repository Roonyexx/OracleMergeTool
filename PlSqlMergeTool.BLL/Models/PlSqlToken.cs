
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
    public required string Text { get; init; } 
    public int Line { get; init; }
    public required int Offset { get; set; } 
    public required int Length  { get; init; }
    public required TokenType Type { get; init; }

    public List<PlSqlToken>? LeadingTrivia { get; set; }
    public List<PlSqlToken>? TrailingTrivia { get; set; } 

    public bool Equals(PlSqlToken? other)
    {
        if (other is null || Type != other.Type) return false; 

        if (Type == TokenType.String)
            return Text.Equals(other.Text, StringComparison.Ordinal);
            
        return Text.Equals(other.Text, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Type == TokenType.String ? Text : Text.ToLower(), 
            Type);
    }
}