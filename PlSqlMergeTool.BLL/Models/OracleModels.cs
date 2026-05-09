namespace PlSqlMergeTool.BLL.Models;

public class OracleCompileError
{
    public int Line { get; init; }
    public int Position { get; init; }
    public required string ErrorText { get; init; }
}

public class TableColumnMetadata
{
    public required string TableName { get; init; }
    public required string ColumnName { get; init; }
    public required string DataType { get; init; }
}