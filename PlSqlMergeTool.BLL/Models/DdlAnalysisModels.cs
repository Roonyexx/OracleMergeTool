using System.Collections.Generic;

namespace PlSqlMergeTool.BLL.Models;

public enum DdlChangeType
{
    Added,      // объект или столбец добавлен вендором
    Deleted,    // объект или столбец удален вендором
    Modified    // в объекте изменились столбцы
}

public enum DdlChangeSource
{
    Local,
    Target,
    Both
}

public class ColumnDiff
{
    public required string ColumnName { get; init; }
    public required DdlChangeType ChangeType { get; init; }
    public required DdlChangeSource Source { get; init; }
    
    // Тип данных
    public string? OldDataType { get; init; }
    public string? NewDataType { get; init; }

    // Размер
    public string? OldSize { get; init; }
    public string? NewSize { get; init; }

    // Обязательность (NULL / NOT NULL)
    public bool? OldNullable { get; init; }
    public bool? NewNullable { get; init; }

    // Значение по умолчанию
    public string? OldDefault { get; init; }
    public string? NewDefault { get; init; }

    // Вспомогательные флаги для UI
    public bool IsTypeChanged => OldDataType != NewDataType;
    public bool IsSizeChanged => OldSize != NewSize;
    public bool IsNullableChanged => OldNullable != NewNullable;
    public bool IsDefaultChanged => OldDefault != NewDefault;
}

public class ObjectDiff
{
    public required string ObjectName { get; init; }
    public required string ObjectType { get; init; }
    public required DdlChangeType ChangeType { get; init; }
    public DdlChangeSource? Source { get; init; }
    public List<ColumnDiff> ColumnChanges { get; init; } = [];
}

public class DdlAnalysisReport
{
    public List<ObjectDiff> ObjectDifferences { get; init; } = [];
}