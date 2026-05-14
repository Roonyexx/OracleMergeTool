using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlSqlMergeTool.BLL.Models;
using PlSqlMergeTool.BLL.Services;
using PlSqlMergeTool.BLL.MergeLogic;
using PlSqlMergeTool.UI.ViewModels.Items;
using PlSqlMergeTool.BLL.Interfaces;

namespace PlSqlMergeTool.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly WorkspaceLoadService _loadService;
    private readonly PackagesMergeService _mergeService;
    private readonly IConfigService _configService;

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private string _statusMessage = "Готов к работе";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ObservableCollection<PackageItemViewModel> Packages { get; } = new();

    private PackageItemViewModel? _selectedPackage;
    public PackageItemViewModel? SelectedPackage
    {
        get => _selectedPackage;
        set
        {
            if (SetProperty(ref _selectedPackage, value))
            {
                CompilePackageCommand.NotifyCanExecuteChanged();
            }
        }
    }

    // todo форма ввода конфига
    private readonly WorkspaceConnectionConfig _config = new()
    {
        BaselineConnection = "Data Source=...;User Id=...;Password=...",
        LocalConnection = "Data Source=...;User Id=...;Password=...",
        TargetConnection = "Data Source=...;User Id=...;Password=..."
    };

    public IAsyncRelayCommand LoadWorkspaceCommand { get; }
    public IAsyncRelayCommand CompilePackageCommand { get; }

    public MainWindowViewModel(WorkspaceLoadService loadService, PackagesMergeService mergeService, IConfigService configService)
    {
        _loadService = loadService;
        _mergeService = mergeService;
        _configService = configService;

        LoadWorkspaceCommand = new AsyncRelayCommand(LoadWorkspaceAsync);
        CompilePackageCommand = new AsyncRelayCommand(CompilePackageAsync, CanCompile);
    }

    private async Task LoadWorkspaceAsync()
    {
        try
        {
            IsLoading = true;
            Packages.Clear();
            SelectedPackage = null;

            StatusMessage = "Этап 1/2: Выгрузка пакетов из БД...";
            var contexts = await _loadService.LoadPackagesAsync(_config);

            StatusMessage = "Этап 2/2: Применение правил слияния...";

            await Task.Run(() => _mergeService.ProcessPackages(contexts));

            foreach (var ctx in contexts)
            {
                Packages.Add(new PackageItemViewModel(ctx));
            }

            StatusMessage = $"Готово! Загружено пакетов: {Packages.Count}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

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
}