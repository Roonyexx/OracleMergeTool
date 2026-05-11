namespace PlSqlMergeTool.BLL.Models;

public enum DdlChangeType
{
    Added,      // объект или столбец добавлен вендором
    Deleted,    // объект или столбец удален вендором
    Modified    // в объекте изменились столбцы
}

// описывает изменение конкретного столбца
public class ColumnDiff
{
    public required string ColumnName { get; init; }
    public required DdlChangeType ChangeType { get; init; }
    public string? OldDataType { get; init; }
    public string? NewDataType { get; init; }
}

// описывает изменение таблицы/представления в целом
public class ObjectDiff
{
    public required string ObjectName { get; init; }
    public required string ObjectType { get; init; }
    public required DdlChangeType ChangeType { get; init; }
    public List<ColumnDiff> ColumnChanges { get; init; } = [];
}

public class DdlAnalysisReport
{
    public List<ObjectDiff> ObjectDifferences { get; init; } = [];
}