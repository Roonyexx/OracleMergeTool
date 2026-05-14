using Avalonia.Controls;
using AvaloniaEdit;
using PlSqlMergeTool.UI.Helpers;
using PlSqlMergeTool.UI.ViewModels;
using PlSqlMergeTool.UI.ViewModels.Items;
using System;

namespace PlSqlMergeTool.UI.Views;

public partial class MainWindow : Window
{
    private readonly SynchronizedEditorManager _syncManager;
    private MainWindowViewModel? _currentViewModel;

    public MainWindow()
    {
        InitializeComponent();

        _syncManager = new SynchronizedEditorManager();

        SetupEditors();
        DataContextChanged += OnDataContextChanged;
    }

    private void SetupEditors()
    {
        // Apply syntax highlighting
        var highlighting = PlSqlSyntaxHighlighting.LoadPlSqlHighlighting();
        LocalEditor.SyntaxHighlighting = highlighting;
        ResolvedEditor.SyntaxHighlighting = highlighting;
        TargetEditor.SyntaxHighlighting = highlighting;

        // Configure editor options
        ConfigureEditor(LocalEditor);
        ConfigureEditor(ResolvedEditor);
        ConfigureEditor(TargetEditor);

        // Register for synchronized scrolling
        _syncManager.RegisterEditor(LocalEditor);
        _syncManager.RegisterEditor(ResolvedEditor);
        _syncManager.RegisterEditor(TargetEditor);
    }

    private void ConfigureEditor(TextEditor editor)
    {
        editor.Options.ShowSpaces = false;
        editor.Options.ShowTabs = false;
        editor.Options.ShowEndOfLine = false;
        editor.Options.ShowBoxForControlCharacters = true;
        editor.Options.EnableHyperlinks = false;
        editor.Options.EnableEmailHyperlinks = false;
        editor.Options.HighlightCurrentLine = true;
        editor.Options.EnableRectangularSelection = true;
        editor.Options.EnableTextDragDrop = false;
        editor.Options.CutCopyWholeLine = true;
        editor.Options.AllowScrollBelowDocument = true;
        editor.Options.IndentationSize = 4;
        editor.Options.ConvertTabsToSpaces = true;

        // Set editor appearance
        editor.TextArea.TextView.CurrentLineBackground = new Avalonia.Media.SolidColorBrush(
            Avalonia.Media.Color.FromArgb(25, 255, 255, 255));
        editor.TextArea.TextView.CurrentLineBorder = new Avalonia.Media.Pen(
            new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(40, 255, 255, 255)), 1);
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_currentViewModel != null)
        {
            _currentViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _currentViewModel = DataContext as MainWindowViewModel;

        if (_currentViewModel != null)
        {
            _currentViewModel.PropertyChanged += OnViewModelPropertyChanged;
            LoadPackageIntoEditors(_currentViewModel.SelectedPackage);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.SelectedPackage) && _currentViewModel != null)
        {
            LoadPackageIntoEditors(_currentViewModel.SelectedPackage);
        }
    }

    private void LoadPackageIntoEditors(PackageItemViewModel? package)
    {
        ClearEditorDecorations(LocalEditor);
        ClearEditorDecorations(TargetEditor);
        ClearEditorDecorations(ResolvedEditor);

        if (package?.Name == "TEST_CALCULATOR")
        {
            Console.WriteLine(package.Context.Local.OriginalText);
        }

        if (package == null)
        {
            LocalEditor.Text = string.Empty;
            TargetEditor.Text = string.Empty;
            ResolvedEditor.Text = string.Empty;
            return;
        }

        LocalEditor.Text = package.LocalText;
        TargetEditor.Text = package.TargetText;
        ResolvedEditor.Text = package.ResolvedText;

        LocalEditor.ScrollToHome();
        ResolvedEditor.ScrollToHome();
        TargetEditor.ScrollToHome();

        // Synchronize line heights with phantom lines
        _syncManager.SynchronizeLineHeights();

        // Apply diff highlighting
        var localRegions = DiffMapper.GetLocalRegions(package.Context);
        var targetRegions = DiffMapper.GetTargetRegions(package.Context);
        var resolvedRegions = DiffMapper.GetResolvedRegions(package.Context);

        if (localRegions.Count > 0)
        {
            LocalEditor.TextArea.TextView.LineTransformers.Add(new DiffColorizer(localRegions));
        }

        if (targetRegions.Count > 0)
        {
            TargetEditor.TextArea.TextView.LineTransformers.Add(new DiffColorizer(targetRegions));
        }

        if (resolvedRegions.Count > 0)
        {
            ResolvedEditor.TextArea.TextView.LineTransformers.Add(new DiffColorizer(resolvedRegions));
        }
    }

    private void ClearEditorDecorations(TextEditor editor)
    {
        var transformers = editor.TextArea.TextView.LineTransformers;

        for (int i = transformers.Count - 1; i >= 0; i--)
        {
            if (transformers[i] is DiffColorizer)
            {
                transformers.RemoveAt(i);
            }
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _syncManager.Dispose();
        base.OnClosed(e);
    }
}
