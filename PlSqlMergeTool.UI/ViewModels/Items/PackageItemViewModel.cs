using CommunityToolkit.Mvvm.ComponentModel;
using PlSqlMergeTool.BLL.MergeLogic;
using PlSqlMergeTool.BLL.Models;

namespace PlSqlMergeTool.UI.ViewModels.Items;

// ui-обертка над бизнес-моделью MergeContext. 
public partial class PackageItemViewModel : ObservableObject
{
    public MergeContext Context { get; }

    public string Name => Context.FileName;

    public MergeStatus Status => Context.Status;

    public string LocalText => Context.Local.OriginalText;
    public string TargetText => Context.Target.OriginalText;

    public string ResolvedText => Context.ResolvedCode ?? Context.Baseline.OriginalText;
    public PackageItemViewModel(MergeContext context)
    {
        Context = context;
    }

    public void MarkAsResolved(string resolvedCode)
    {
        Context.ResolveWith(MergeStatus.AutoMerged, resolvedCode);
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(StatusIcon));
    }

    public string StatusIcon => Status switch
    {
        MergeStatus.AutoMerged => "🟢",
        MergeStatus.AutoTarget => "🔵",
        MergeStatus.AutoLocal => "⚪",
        MergeStatus.ManualConflict => "🔴",
        MergeStatus.NoChanges => "⚪",
        _ => "❓"
    };
}