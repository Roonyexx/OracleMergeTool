using PlSqlMergeTool.BLL.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlSqlMergeTool.BLL.Services;

public class DdlAnalysisService
{
    // ТЕПЕРЬ ПРИНИМАЕМ 3 ИСТОЧНИКА!
    public DdlAnalysisReport AnalyzeDdlChanges(
        IEnumerable<TableColumnMetadata> baselineMetadata,
        IEnumerable<TableColumnMetadata> localMetadata, 
        IEnumerable<TableColumnMetadata> targetMetadata)
    {
        var report = new DdlAnalysisReport();

        var baseDict = baselineMetadata.GroupBy(m => m.ObjectName).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
        var localDict = localMetadata.GroupBy(m => m.ObjectName).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
        var targetDict = targetMetadata.GroupBy(m => m.ObjectName).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var allObjectNames = baseDict.Keys.Union(localDict.Keys).Union(targetDict.Keys).Distinct();

        foreach (var tableName in allObjectNames)
        {
            var inBase = baseDict.TryGetValue(tableName, out var baseCols);
            var inLocal = localDict.TryGetValue(tableName, out var localCols);
            var inTarget = targetDict.TryGetValue(tableName, out var targetCols);

            if (!inBase)
            {
                if (inLocal && !inTarget)
                {
                    report.ObjectDifferences.Add(CreateObjectDiff(tableName, localCols!, DdlChangeType.Added, DdlChangeSource.Local));
                }
                else if (!inLocal && inTarget)
                {
                    report.ObjectDifferences.Add(CreateObjectDiff(tableName, targetCols!, DdlChangeType.Added, DdlChangeSource.Target));
                }
                else if (inLocal && inTarget)
                {
                    var colDiffs = GetColumnDifferences(new List<TableColumnMetadata>(), localCols!, targetCols!);
                    if (colDiffs.Count > 0)
                        report.ObjectDifferences.Add(new ObjectDiff { ObjectName = tableName, ObjectType = targetCols!.First().ObjectType, ChangeType = DdlChangeType.Added, Source = DdlChangeSource.Both, ColumnChanges = colDiffs });
                }
                continue;
            }

            // 2. Таблица была удалена
            if (!inLocal || !inTarget)
            {
                if (!inLocal && inTarget) 
                    report.ObjectDifferences.Add(CreateObjectDiff(tableName, baseCols!, DdlChangeType.Deleted, DdlChangeSource.Local));
                else if (inLocal && !inTarget) 
                    report.ObjectDifferences.Add(CreateObjectDiff(tableName, baseCols!, DdlChangeType.Deleted, DdlChangeSource.Target));
                else 
                    report.ObjectDifferences.Add(CreateObjectDiff(tableName, baseCols!, DdlChangeType.Deleted, DdlChangeSource.Both));
                continue;
            }

            var columnChanges = GetColumnDifferences(baseCols!, localCols!, targetCols!);
            if (columnChanges.Count > 0)
            {
                report.ObjectDifferences.Add(new ObjectDiff
                {
                    ObjectName = tableName,
                    ObjectType = targetCols!.First().ObjectType,
                    ChangeType = DdlChangeType.Modified,
                    Source = null,
                    ColumnChanges = columnChanges
                });
            }
        }

        return report;
    }

    private List<ColumnDiff> GetColumnDifferences(
        List<TableColumnMetadata> baseline, 
        List<TableColumnMetadata> local, 
        List<TableColumnMetadata> target)
    {
        var diffs = new List<ColumnDiff>();
        var baseCols = baseline.ToDictionary(c => c.ColumnName, StringComparer.OrdinalIgnoreCase);
        var localCols = local.ToDictionary(c => c.ColumnName, StringComparer.OrdinalIgnoreCase);
        var targetCols = target.ToDictionary(c => c.ColumnName, StringComparer.OrdinalIgnoreCase);

        var allColNames = baseCols.Keys.Union(localCols.Keys).Union(targetCols.Keys).Distinct();

        foreach (var colName in allColNames)
        {
            bool inB = baseCols.TryGetValue(colName, out var bCol);
            bool inL = localCols.TryGetValue(colName, out var lCol);
            bool inT = targetCols.TryGetValue(colName, out var tCol);

            if (!inB)
            {
                if (inL && !inT) diffs.Add(CreateDiff(lCol!, DdlChangeType.Added, DdlChangeSource.Local));
                if (!inL && inT) diffs.Add(CreateDiff(tCol!, DdlChangeType.Added, DdlChangeSource.Target));
                if (inL && inT) diffs.Add(CreateConflictDiff(lCol!, tCol!, DdlChangeType.Added)); // Добавили оба
                continue;
            }

            if (!inL || !inT)
            {
                if (!inL && inT) diffs.Add(CreateDiff(bCol!, DdlChangeType.Deleted, DdlChangeSource.Local));
                if (inL && !inT) diffs.Add(CreateDiff(bCol!, DdlChangeType.Deleted, DdlChangeSource.Target));
                if (!inL && !inT) diffs.Add(CreateDiff(bCol!, DdlChangeType.Deleted, DdlChangeSource.Both));
                continue;
            }

            bool localChanged = IsColumnModified(bCol!, lCol!);
            bool targetChanged = IsColumnModified(bCol!, tCol!);

            if (localChanged && !targetChanged)
            {
                diffs.Add(CreateModifiedDiff(bCol!, lCol!, DdlChangeSource.Local));
            }
            else if (!localChanged && targetChanged)
            {
                diffs.Add(CreateModifiedDiff(bCol!, tCol!, DdlChangeSource.Target));
            }
            else if (localChanged && targetChanged)
            {
                diffs.Add(CreateModifiedDiff(lCol!, tCol!, DdlChangeSource.Both));
            }
        }

        return diffs;
    }

    private bool IsColumnModified(TableColumnMetadata oldCol, TableColumnMetadata newCol)
    {
        return oldCol.DataType != newCol.DataType ||
               FormatSize(oldCol) != FormatSize(newCol) ||
               oldCol.Nullable != newCol.Nullable ||
               oldCol.DataDefault?.Trim() != newCol.DataDefault?.Trim();
    }

    private string FormatSize(TableColumnMetadata col)
    {
        if (col.DataType.Contains("VARCHAR") || col.DataType.Contains("CHAR") || col.DataType.Contains("RAW"))
            return col.DataLength?.ToString() ?? "";
            
        if (col.DataType == "NUMBER")
        {
            if (col.DataPrecision.HasValue && col.DataScale.HasValue && col.DataScale > 0)
                return $"{col.DataPrecision},{col.DataScale}";
            if (col.DataPrecision.HasValue)
                return col.DataPrecision.ToString()!;
        }
        return "";
    }

    private ObjectDiff CreateObjectDiff(string name, List<TableColumnMetadata> cols, DdlChangeType type, DdlChangeSource source)
    {
        return new ObjectDiff
        {
            ObjectName = name,
            ObjectType = cols.First().ObjectType,
            ChangeType = type,
            Source = source,
            ColumnChanges = cols.Select(c => CreateDiff(c, type, source)).ToList()
        };
    }

    private ColumnDiff CreateDiff(TableColumnMetadata col, DdlChangeType type, DdlChangeSource source) => new()
    {
        ColumnName = col.ColumnName,
        ChangeType = type,
        Source = source,
        NewDataType = type == DdlChangeType.Added ? col.DataType : null,
        OldDataType = type == DdlChangeType.Deleted ? col.DataType : null,
        NewSize = type == DdlChangeType.Added ? FormatSize(col) : null,
        OldSize = type == DdlChangeType.Deleted ? FormatSize(col) : null,
    };

    private ColumnDiff CreateModifiedDiff(TableColumnMetadata oldCol, TableColumnMetadata newCol, DdlChangeSource source) => new()
    {
        ColumnName = oldCol.ColumnName,
        ChangeType = DdlChangeType.Modified,
        Source = source,
        OldDataType = oldCol.DataType,
        NewDataType = newCol.DataType,
        OldSize = FormatSize(oldCol),
        NewSize = FormatSize(newCol),
        OldNullable = oldCol.Nullable == "Y",
        NewNullable = newCol.Nullable == "Y",
        OldDefault = oldCol.DataDefault?.Trim(),
        NewDefault = newCol.DataDefault?.Trim()
    };

    private ColumnDiff CreateConflictDiff(TableColumnMetadata local, TableColumnMetadata target, DdlChangeType type) => new()
    {
        ColumnName = local.ColumnName,
        ChangeType = type,
        Source = DdlChangeSource.Both,
        OldDataType = local.DataType,
        NewDataType = target.DataType,
        OldSize = FormatSize(local),
        NewSize = FormatSize(target)
    };
}