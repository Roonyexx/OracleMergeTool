using DiffPlex.Model;
using PlSqlMergeTool.BLL.Models;

namespace PlSqlMergeTool.BLL.MergeLogic;

public enum MergeStatus
{
    Pending,            // еще не обработан
    AutoTarget,         // автоматически взят Target
    AutoLocal,          // Автоматически взят Local
    AutoMerged,
    NoChanges,          // Файлы не менялись
    ManualConflict      // Kонфликт, требующий ручного разрешения
}

public enum MergeSource
{
    None,
    Local,
    Target
}

public class ResolvedRegion
{
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public MergeSource Source { get; set; }
}

public class MergeContext
{
    public required string FileName { get; init; }
    public required ParsedSqlDocument Baseline { get; init; }
    public required ParsedSqlDocument Local { get; init; }
    public required ParsedSqlDocument Target { get; init; }

    public required DiffResult BaseVsLocalDiff { get; init; }
    public required DiffResult BaseVsTargetDiff { get; init; }

    public bool HasLocalChanges => BaseVsLocalDiff.DiffBlocks.Any();
    public bool HasTargetChanges => BaseVsTargetDiff.DiffBlocks.Any();

    public MergeStatus Status { get; private set; } = MergeStatus.Pending;
    public string? ResolvedCode { get; private set; } 
    public List<ResolvedRegion> ResolvedRegions { get; private set; } = new();

    public void ResolveWith(MergeStatus status, string? resolvedText, List<ResolvedRegion>? resolvedRegions = null)
    {
        if (Status != MergeStatus.Pending)
        {
            throw new InvalidOperationException($"Файл {FileName} уже обработан со статусом {Status}");
        }

        Status = status;
        ResolvedCode = resolvedText;
        if (resolvedRegions != null)
        {
            ResolvedRegions = resolvedRegions;
        }
    }

    public void MarkAsConflict()
    {
        if (Status != MergeStatus.Pending) return;
        Status = MergeStatus.ManualConflict;
    }
}