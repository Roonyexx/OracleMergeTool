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

    [ObservableProperty] private bool _isSettingsOpen;
    [ObservableProperty] private SettingsViewModel? _currentSettingsVM;

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

    public IAsyncRelayCommand LoadWorkspaceCommand { get; }
    public IAsyncRelayCommand CompilePackageCommand { get; }
    public IRelayCommand OpenSettingsCommand { get; }
    public IRelayCommand CloseSettingsCommand { get; }

    public MainWindowViewModel(WorkspaceLoadService loadService, PackagesMergeService mergeService, IConfigService configService)
    {
        _loadService = loadService;
        _mergeService = mergeService;
        _configService = configService;

        LoadWorkspaceCommand = new AsyncRelayCommand(LoadWorkspaceAsync);
        CompilePackageCommand = new AsyncRelayCommand(CompilePackageAsync, CanCompile);
        OpenSettingsCommand = new RelayCommand(OpenSettings);
        CloseSettingsCommand = new RelayCommand(CloseSettings);
    }

    private void OpenSettings()
    {
        CurrentSettingsVM = new SettingsViewModel(_configService, CloseSettings);
        IsSettingsOpen = true;
    }

    private void CloseSettings()
    {
        IsSettingsOpen = false;
        CurrentSettingsVM = null;
    }

    private async Task LoadWorkspaceAsync()
    {
        try
        {
            IsLoading = true;
            Packages.Clear();
            SelectedPackage = null;

            var config = _configService.LoadConfig();

            StatusMessage = "Этап 1/2: Выгрузка пакетов из БД...";
            var contexts = await _loadService.LoadPackagesAsync(config);

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