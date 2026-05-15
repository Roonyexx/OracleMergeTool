namespace PlSqlMergeTool.BLL.Models;

public class OracleCompileError
{
    public int Line { get; init; }
    public int Position { get; init; }
    public required string ErrorText { get; init; }
}

public class TableColumnMetadata
{
    public required string ObjectName { get; init; }
    public required string ObjectType { get; init; }
    public required string ColumnName { get; init; }
    public required string DataType { get; init; }

    public int? DataLength { get; init; }
    public int? DataPrecision { get; init; }
    public int? DataScale { get; init; }
    public required string Nullable { get; init; }
    public string? DataDefault { get; init; }
}