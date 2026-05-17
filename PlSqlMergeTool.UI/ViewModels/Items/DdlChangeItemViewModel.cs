using PlSqlMergeTool.BLL.Models;
using System.Collections.Generic;
using System.Linq;

namespace PlSqlMergeTool.UI.ViewModels.Items;

public class DdlChangeItemViewModel
{
    public string ObjectName { get; }
    public string ObjectType { get; }
    public DdlChangeType ChangeType { get; }
    public DdlChangeSource? Source { get; }
    public List<ColumnChangeItemViewModel> ColumnChanges { get; }

    public string Icon => ChangeType switch
    {
        DdlChangeType.Added => "➕",
        DdlChangeType.Deleted => "➖",
        DdlChangeType.Modified => "✏️",
        _ => "❓"
    };

    public string ChangeTypeText => ChangeType switch
    {
        DdlChangeType.Added => "Добавлен",
        DdlChangeType.Deleted => "Удален",
        DdlChangeType.Modified => "Изменен",
        _ => "Неизвестно"
    };

    public string SourceText => Source switch
    {
        DdlChangeSource.Local => "LOCAL",
        DdlChangeSource.Target => "TARGET",
        DdlChangeSource.Both => "BOTH",
        null => "",
        _ => ""
    };

    public string SourceColor => Source switch
    {
        DdlChangeSource.Local => "#569CD6",
        DdlChangeSource.Target => "#CE9178",
        DdlChangeSource.Both => "#D16969",
        _ => "#808080"
    };

    public string DisplayText => $"{ObjectName} ({ObjectType})";

    public string Summary
    {
        get
        {
            if (ChangeType == DdlChangeType.Added || ChangeType == DdlChangeType.Deleted)
                return $"{ChangeTypeText} • {SourceText}";

            var changes = new List<string>();
            if (ColumnChanges.Any(c => c.ChangeType == DdlChangeType.Added))
                changes.Add($"+{ColumnChanges.Count(c => c.ChangeType == DdlChangeType.Added)} колонок");
            if (ColumnChanges.Any(c => c.ChangeType == DdlChangeType.Deleted))
                changes.Add($"-{ColumnChanges.Count(c => c.ChangeType == DdlChangeType.Deleted)} колонок");
            if (ColumnChanges.Any(c => c.ChangeType == DdlChangeType.Modified))
                changes.Add($"~{ColumnChanges.Count(c => c.ChangeType == DdlChangeType.Modified)} изменений");

            return string.Join(", ", changes);
        }
    }

    public DdlChangeItemViewModel(ObjectDiff diff)
    {
        ObjectName = diff.ObjectName;
        ObjectType = diff.ObjectType;
        ChangeType = diff.ChangeType;
        Source = diff.Source;
        ColumnChanges = diff.ColumnChanges.Select(c => new ColumnChangeItemViewModel(c)).ToList();
    }
}

public class ColumnChangeItemViewModel
{
    public string ColumnName { get; }
    public DdlChangeType ChangeType { get; }
    public DdlChangeSource Source { get; }

    public string? OldDataType { get; }
    public string? NewDataType { get; }
    public string? OldSize { get; }
    public string? NewSize { get; }
    public bool? OldNullable { get; }
    public bool? NewNullable { get; }
    public string? OldDefault { get; }
    public string? NewDefault { get; }

    public bool IsTypeChanged { get; }
    public bool IsSizeChanged { get; }
    public bool IsNullableChanged { get; }
    public bool IsDefaultChanged { get; }

    public string Icon => ChangeType switch
    {
        DdlChangeType.Added => "➕",
        DdlChangeType.Deleted => "➖",
        DdlChangeType.Modified => "✏️",
        _ => "•"
    };

    public string ChangeTypeText => ChangeType switch
    {
        DdlChangeType.Added => "Добавлена",
        DdlChangeType.Deleted => "Удалена",
        DdlChangeType.Modified => "Изменена",
        _ => ""
    };

    public string SourceText => Source switch
    {
        DdlChangeSource.Local => "LOCAL",
        DdlChangeSource.Target => "TARGET",
        DdlChangeSource.Both => "BOTH",
        _ => ""
    };

    public string SourceColor => Source switch
    {
        DdlChangeSource.Local => "#569CD6",
        DdlChangeSource.Target => "#CE9178",
        DdlChangeSource.Both => "#D16969",
        _ => "#808080"
    };

    public string TypeInfo
    {
        get
        {
            if (ChangeType == DdlChangeType.Added)
                return FormatType(NewDataType, NewSize);
            if (ChangeType == DdlChangeType.Deleted)
                return FormatType(OldDataType, OldSize);
            if (IsTypeChanged || IsSizeChanged)
                return $"{FormatType(OldDataType, OldSize)} → {FormatType(NewDataType, NewSize)}";
            return FormatType(NewDataType, NewSize);
        }
    }

    public string NullableInfo
    {
        get
        {
            if (ChangeType == DdlChangeType.Added)
                return NewNullable == true ? "NULL" : "NOT NULL";
            if (ChangeType == DdlChangeType.Deleted)
                return OldNullable == true ? "NULL" : "NOT NULL";
            if (IsNullableChanged)
                return $"{(OldNullable == true ? "NULL" : "NOT NULL")} → {(NewNullable == true ? "NULL" : "NOT NULL")}";
            return NewNullable == true ? "NULL" : "NOT NULL";
        }
    }

    public string? DefaultInfo
    {
        get
        {
            if (string.IsNullOrEmpty(OldDefault) && string.IsNullOrEmpty(NewDefault))
                return null;

            if (ChangeType == DdlChangeType.Added && !string.IsNullOrEmpty(NewDefault))
                return $"DEFAULT {NewDefault}";
            if (ChangeType == DdlChangeType.Deleted && !string.IsNullOrEmpty(OldDefault))
                return $"DEFAULT {OldDefault}";
            if (IsDefaultChanged)
            {
                var oldDef = string.IsNullOrEmpty(OldDefault) ? "нет" : OldDefault;
                var newDef = string.IsNullOrEmpty(NewDefault) ? "нет" : NewDefault;
                return $"DEFAULT: {oldDef} → {newDef}";
            }
            return !string.IsNullOrEmpty(NewDefault) ? $"DEFAULT {NewDefault}" : null;
        }
    }

    public string DetailedInfo
    {
        get
        {
            var parts = new List<string> { TypeInfo, NullableInfo };
            if (!string.IsNullOrEmpty(DefaultInfo))
                parts.Add(DefaultInfo);
            return string.Join(" • ", parts);
        }
    }

    private string FormatType(string? dataType, string? size)
    {
        if (string.IsNullOrEmpty(dataType))
            return "";
        if (string.IsNullOrEmpty(size))
            return dataType;
        return $"{dataType}({size})";
    }

    public ColumnChangeItemViewModel(ColumnDiff diff)
    {
        ColumnName = diff.ColumnName;
        ChangeType = diff.ChangeType;
        Source = diff.Source;
        OldDataType = diff.OldDataType;
        NewDataType = diff.NewDataType;
        OldSize = diff.OldSize;
        NewSize = diff.NewSize;
        OldNullable = diff.OldNullable;
        NewNullable = diff.NewNullable;
        OldDefault = diff.OldDefault;
        NewDefault = diff.NewDefault;
        IsTypeChanged = diff.IsTypeChanged;
        IsSizeChanged = diff.IsSizeChanged;
        IsNullableChanged = diff.IsNullableChanged;
        IsDefaultChanged = diff.IsDefaultChanged;
    }
}
