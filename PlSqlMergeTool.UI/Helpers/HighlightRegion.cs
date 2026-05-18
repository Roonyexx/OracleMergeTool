namespace PlSqlMergeTool.UI.Helpers;

public enum HighlightType
{
    None,
    Added,
    Deleted,
    Conflict,
    ResolvedFromLocal,
    ResolvedFromTarget
}

public class HighlightRegion
{
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public HighlightType Type { get; set; }
}