namespace PlSqlMergeTool.BLL.Models;

public class WorkspaceConnectionConfig
{
    public required string BaselineConnection { get; init; }
    public required string LocalConnection { get; init; }
    public required string TargetConnection { get; init; }
}