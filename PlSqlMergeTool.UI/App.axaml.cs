using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using PlSqlMergeTool.BLL.LexicalAnalysis;
using PlSqlMergeTool.BLL.Models;
using PlSqlMergeTool.BLL.Interfaces;
using PlSqlMergeTool.BLL.MergeLogic;
using PlSqlMergeTool.BLL.Services;
using PlSqlMergeTool.DAL.Repositories;
using PlSqlMergeTool.UI.ViewModels;
using PlSqlMergeTool.UI.Views;
using DiffPlex;
using DiffPlex.Chunkers;

namespace PlSqlMergeTool.UI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        var serviceProvider = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.DataContext = serviceProvider.GetRequiredService<MainWindowViewModel>();
            
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<MainWindow>();
        services.AddTransient<MainWindowViewModel>();

        //DAL
        services.AddTransient<IOracleRepository, OracleRepository>(); 

        //BLL
        services.AddTransient<WorkspaceLoadService>();
        services.AddTransient<PackagesMergeService>();
        services.AddTransient<SqlDifferService>();

        services.AddTransient<Scanner>();
        services.AddTransient<TokenFilter>(provider => 
        {
            var excludedTypes = new[] 
            { 
                TokenType.Whitespace, 
                TokenType.SingleLineComment, 
                TokenType.MultiLineComment 
            };
            return new TokenFilter(excludedTypes);
        });

        services.AddTransient<SqlAnalyserService>();
        services.AddTransient<DdlAnalysisService>();
        services.AddTransient<MergeResolverService>();
        services.AddTransient<SqlBuilderService>();
        services.AddTransient<MergeRuleBuilder>();
        
        services.AddTransient<ITokenMergeAlgorithm, TokenMergeAlgorithm>();
        services.AddTransient<IDiffer, Differ>();
        services.AddTransient<IChunker, TokenChunker>();
    }
}