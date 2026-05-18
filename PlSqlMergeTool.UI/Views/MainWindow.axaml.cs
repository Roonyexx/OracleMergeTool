using Avalonia.Controls;
using AvaloniaEdit;
using PlSqlMergeTool.UI.Helpers;
using PlSqlMergeTool.UI.ViewModels;
using PlSqlMergeTool.UI.ViewModels.Items;
using System;
using System.Collections.Generic;
using System.Linq;

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
        var highlighting = PlSqlSyntaxHighlighting.LoadPlSqlHighlighting();
        LocalEditor.SyntaxHighlighting = highlighting;
        ResolvedEditor.SyntaxHighlighting = highlighting;
        TargetEditor.SyntaxHighlighting = highlighting;

        ConfigureEditor(LocalEditor);
        ConfigureEditor(ResolvedEditor);
        ConfigureEditor(TargetEditor);

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

        editor.ShowLineNumbers = false;

        var diffMargin = new DiffLineNumberMargin();
        editor.TextArea.LeftMargins.Insert(0, diffMargin);

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

    private void ClearPhantomLines(TextEditor editor)
    {
        var margin = editor.TextArea.LeftMargins.OfType<DiffLineNumberMargin>().FirstOrDefault();
        if (margin != null)
        {
            margin.PhantomLines.Clear();
            margin.InvalidateVisual();
        }
    }

    private void LoadPackageIntoEditors(PackageItemViewModel? package)
    {
        ClearEditorDecorations(LocalEditor);
        ClearEditorDecorations(TargetEditor);
        ClearEditorDecorations(ResolvedEditor);

        ClearPhantomLines(LocalEditor);
        ClearPhantomLines(TargetEditor);
        ClearPhantomLines(ResolvedEditor);

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

        List<SyncBlock> syncBlocks = DiffMapper.BuildSyncBlocks(package.Context);
        
        var calculator = new EditorAlignmentCalculator();
        var gaps = calculator.CalculateGaps(syncBlocks);

        var localRegions = DiffMapper.GetLocalRegions(package.Context);
        var targetRegions = DiffMapper.GetTargetRegions(package.Context);
        var resolvedRegions = DiffMapper.GetResolvedRegions(package.Context);

        ApplyGapsToEditor(LocalEditor, gaps.LocalGaps);
        ApplyGapsToEditor(TargetEditor, gaps.TargetGaps);
        ApplyGapsToEditor(ResolvedEditor, gaps.ResolvedGaps);

        ShiftRegionsForEditor(LocalEditor, localRegions);
        ShiftRegionsForEditor(TargetEditor, targetRegions);
        ShiftRegionsForEditor(ResolvedEditor, resolvedRegions);

        if (localRegions.Count > 0)
            LocalEditor.TextArea.TextView.BackgroundRenderers.Add(new DiffBackgroundRenderer(LocalEditor.TextArea.TextView, localRegions));

        if (targetRegions.Count > 0)
            TargetEditor.TextArea.TextView.BackgroundRenderers.Add(new DiffBackgroundRenderer(TargetEditor.TextArea.TextView, targetRegions));

        if (resolvedRegions.Count > 0)
            ResolvedEditor.TextArea.TextView.BackgroundRenderers.Add(new DiffBackgroundRenderer(ResolvedEditor.TextArea.TextView, resolvedRegions));

    }

    private void ApplyGapsToEditor(TextEditor editor, Dictionary<int, int> gaps)
    {
        if (gaps == null || gaps.Count == 0) return;

        var sortedGaps = gaps.OrderByDescending(g => g.Key);
        
        foreach (var gap in sortedGaps)
        {
            editor.InsertPhantomSpace(gap.Key, gap.Value);
        }
    }

    private void ShiftRegionsForEditor(TextEditor editor, List<HighlightRegion> regions)
    {
        if (regions == null || regions.Count == 0) return;

        var margin = editor.TextArea.LeftMargins.OfType<DiffLineNumberMargin>().FirstOrDefault();
        if (margin != null && margin.PhantomLines.Count > 0)
        {
            RegionMapper.ShiftRegions(regions, margin.PhantomLines);
        }
    }

    private void ClearEditorDecorations(TextEditor editor)
    {
        var transformers = editor.TextArea.TextView.LineTransformers;
        for (int i = transformers.Count - 1; i >= 0; i--)
        {
            if (transformers[i] is DiffColorizer) transformers.RemoveAt(i);
        }

        var renderers = editor.TextArea.TextView.BackgroundRenderers;
        for (int i = renderers.Count - 1; i >= 0; i--)
        {
            if (renderers[i] is DiffBackgroundRenderer) renderers.RemoveAt(i);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _syncManager.Dispose();
        base.OnClosed(e);
    }
}