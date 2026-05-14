using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlSqlMergeTool.BLL.Interfaces;
using PlSqlMergeTool.BLL.Models;

namespace PlSqlMergeTool.UI.ViewModels;

public partial class SettingsView : ObservableObject
{
    private readonly IConfigService _configService;

    [ObservableProperty] private string _baselineConnection = "";
    [ObservableProperty] private string _localConnection = "";
    [ObservableProperty] private string _targetConnection = "";

    public SettingsView(IConfigService configService)
    {
        _configService = configService;
        
        var config = _configService.LoadConfig();
        BaselineConnection = config.BaselineConnection;
        LocalConnection = config.LocalConnection;
        TargetConnection = config.TargetConnection;
    }

    [RelayCommand]
    private void Save()
    {
        var config = new WorkspaceConnectionConfig
        {
            BaselineConnection = BaselineConnection,
            LocalConnection = LocalConnection,
            TargetConnection = TargetConnection
        };
        _configService.SaveConfig(config);
    }
}