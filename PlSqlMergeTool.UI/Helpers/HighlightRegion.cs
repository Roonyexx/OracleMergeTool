namespace PlSqlMergeTool.UI.Helpers;

public enum HighlightType
{
    None,
    Added,
    Deleted,
    Conflict
}

public class HighlightRegion
{
    public int StartOffset { get; init; }
    public int Length { get; init; }
    public HighlightType Type { get; init; }
}