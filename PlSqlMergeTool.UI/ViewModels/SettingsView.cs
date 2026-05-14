using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlSqlMergeTool.BLL.Interfaces;
using PlSqlMergeTool.BLL.Models;

namespace PlSqlMergeTool.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IConfigService _configService;
    private readonly Action? _onClose;

    [ObservableProperty] private string _baselineConnection = "";
    [ObservableProperty] private string _localConnection = "";
    [ObservableProperty] private string _targetConnection = "";

    public SettingsViewModel(IConfigService configService, Action? onClose = null)
    {
        _configService = configService;
        _onClose = onClose;
        var config = _configService.LoadConfig();
        BaselineConnection = config.BaselineConnection;
        LocalConnection = config.LocalConnection;
        TargetConnection = config.TargetConnection;
    }

    [RelayCommand]
    private void Save()
    {
        _configService.SaveConfig(new WorkspaceConnectionConfig
        {
            BaselineConnection = BaselineConnection,
            LocalConnection = LocalConnection,
            TargetConnection = TargetConnection
        });
        _onClose?.Invoke();
    }

    [RelayCommand]
    private void Close()
    {
        _onClose?.Invoke();
    }
}