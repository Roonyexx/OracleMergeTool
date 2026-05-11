using PlSqlMergeTool.BLL.Models;
using PlSqlMergeTool.BLL.MergeLogic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlSqlMergeTool.BLL.Services;

public class DdlAnalysisService
{
    public DdlAnalysisReport AnalyzeDdlChanges(
        IEnumerable<TableColumnMetadata> localMetadata, 
        IEnumerable<TableColumnMetadata> targetMetadata)
    {
        var report = new DdlAnalysisReport();

        var localDict = localMetadata.GroupBy(m => m.ObjectName).ToDictionary(g => g.Key, g => g.ToList());
        var targetDict = targetMetadata.GroupBy(m => m.ObjectName).ToDictionary(g => g.Key, g => g.ToList());

        var allObjectNames = localDict.Keys.Union(targetDict.Keys).Distinct();

        foreach (var tableName in allObjectNames)
        {
            var inLocal = localDict.TryGetValue(tableName, out var localCols);
            var inTarget = targetDict.TryGetValue(tableName, out var targetCols);

            if (!inLocal && inTarget)
            {
                // таблица добавлена вендором
                report.ObjectDifferences.Add(new ObjectDiff
                {
                    ObjectName = tableName,
                    ObjectType = targetCols!.First().ObjectType,
                    ChangeType = DdlChangeType.Added,
                    ColumnChanges = targetCols!.Select(c => new ColumnDiff 
                        { ColumnName = c.ColumnName, ChangeType = DdlChangeType.Added, NewDataType = c.DataType }).ToList()
                });
            }
            else if (inLocal && !inTarget)
            {
                // таблица удалена вендором
                report.ObjectDifferences.Add(new ObjectDiff
                {
                    ObjectName = tableName,
                    ObjectType = localCols!.First().ObjectType,
                    ChangeType = DdlChangeType.Deleted,
                    ColumnChanges = localCols!.Select(c => new ColumnDiff 
                        { ColumnName = c.ColumnName, ChangeType = DdlChangeType.Deleted, OldDataType = c.DataType }).ToList()
                });
            }
            else if (inLocal && inTarget)
            {
                // ищем изменения столбцов
                var columnChanges = GetColumnDifferences(localCols!, targetCols!);
                
                if (columnChanges.Count != 0)
                {
                    report.ObjectDifferences.Add(new ObjectDiff
                    {
                        ObjectName = tableName,
                        ObjectType = targetCols!.First().ObjectType,
                        ChangeType = DdlChangeType.Modified,
                        ColumnChanges = columnChanges
                    });
                }
            }
        }

        return report;
    }

    private List<ColumnDiff> GetColumnDifferences(List<TableColumnMetadata> local, List<TableColumnMetadata> target)
    {
        var diffs = new List<ColumnDiff>();
        var localCols = local.ToDictionary(c => c.ColumnName);
        var targetCols = target.ToDictionary(c => c.ColumnName);

        foreach (var lCol in localCols.Values)
        {
            if (!targetCols.TryGetValue(lCol.ColumnName, out var tCol))
            {
                diffs.Add(new ColumnDiff { ColumnName = lCol.ColumnName, ChangeType = DdlChangeType.Deleted, OldDataType = lCol.DataType });
            }
            else if (lCol.DataType != tCol.DataType)
            {
                diffs.Add(new ColumnDiff { ColumnName = lCol.ColumnName, ChangeType = DdlChangeType.Modified, OldDataType = lCol.DataType, NewDataType = tCol.DataType });
            }
        }

        foreach (var tCol in targetCols.Values)
        {
            if (!localCols.ContainsKey(tCol.ColumnName))
            {
                diffs.Add(new ColumnDiff { ColumnName = tCol.ColumnName, ChangeType = DdlChangeType.Added, NewDataType = tCol.DataType });
            }
        }

        return diffs;
    }
}