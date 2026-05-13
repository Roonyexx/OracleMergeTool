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

    public PackageItemViewModel(MergeContext context)
    {
        Context = context;
    }

    public void MarkAsResolved(string resolvedCode)
    {
        Context.ResolveWith(MergeStatus.AutoMerged, resolvedCode);
        OnPropertyChanged(nameof(Status));
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