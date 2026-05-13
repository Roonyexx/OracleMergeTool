using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlSqlMergeTool.BLL.Models;
using PlSqlMergeTool.BLL.Services;
using PlSqlMergeTool.BLL.MergeLogic;
using PlSqlMergeTool.UI.ViewModels.Items;

namespace PlSqlMergeTool.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly WorkspaceLoadService _loadService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Готов к работе";

    public ObservableCollection<PackageItemViewModel> Packages { get; } = new();

    [ObservableProperty]
    private PackageItemViewModel? _selectedPackage;

    // todo форма ввода конфига
    private readonly WorkspaceConnectionConfig _config = new()
    {
        BaselineConnection = "Data Source=...;User Id=...;Password=...",
        LocalConnection = "Data Source=...;User Id=...;Password=...",
        TargetConnection = "Data Source=...;User Id=...;Password=..."
    };

    public MainWindowViewModel(WorkspaceLoadService loadService)
    {
        _loadService = loadService;
    }

    /// <summary>
    /// Команда загрузки рабочего пространства (вызывается кнопкой "Анализировать")
    /// </summary>
    [RelayCommand]
    private async Task LoadWorkspaceAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Загрузка и анализ пакетов...";
            Packages.Clear();
            SelectedPackage = null;

            var contexts = await _loadService.LoadPackagesAsync(_config);

            foreach (var ctx in contexts)
            {
                Packages.Add(new PackageItemViewModel(ctx));
            }

            StatusMessage = $"Анализ завершен. Найдено пакетов: {Packages.Count}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка загрузки: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCompile))]
    private async Task CompilePackageAsync()
    {
        if (SelectedPackage == null) return;

        // todo
        StatusMessage = $"Пакет {SelectedPackage.Name} отправлен на компиляцию...";
    }

    private bool CanCompile()
    {
        return SelectedPackage != null && SelectedPackage.Status != MergeStatus.ManualConflict;
    }

    partial void OnSelectedPackageChanged(PackageItemViewModel? value)
    {
        CompilePackageCommand.NotifyCanExecuteChanged();
    }
}