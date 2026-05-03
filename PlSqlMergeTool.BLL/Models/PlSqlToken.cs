
namespace PlSqlMergeTool.DAL.Models;

public class PlSqlToken : IEquatable<PlSqlToken>
{
    public string Text { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }

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