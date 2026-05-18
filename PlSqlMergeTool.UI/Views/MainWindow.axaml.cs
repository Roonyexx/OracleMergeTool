using Avalonia.Controls;
using AvaloniaEdit;
using PlSqlMergeTool.UI.Helpers;
using PlSqlMergeTool.UI.ViewModels;
using PlSqlMergeTool.UI.ViewModels.Items;
using PlSqlMergeTool.BLL.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PlSqlMergeTool.UI.Views;

public partial class MainWindow : Window
{
    private readonly SynchronizedEditorManager _syncManager;
    private MainWindowViewModel? _currentViewModel;
    private CancellationTokenSource? _alignmentCts;
    private bool _isUpdatingAlignment;

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

        ResolvedEditor.TextChanged += OnResolvedEditorTextChanged;
        ResolvedEditor.TextArea.AddHandler(Avalonia.Input.InputElement.KeyDownEvent, OnResolvedEditorPreviewKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        ResolvedEditor.TextArea.AddHandler(Avalonia.Input.InputElement.TextInputEvent, OnResolvedEditorTextInput, Avalonia.Interactivity.RoutingStrategies.Tunnel);
    }

    private void OnResolvedEditorTextInput(object? sender, Avalonia.Input.TextInputEventArgs e)
    {
        if (ResolvedEditor.Document == null || string.IsNullOrEmpty(e.Text)) return;

        var margin = ResolvedEditor.TextArea.LeftMargins.OfType<DiffLineNumberMargin>().FirstOrDefault();
        if (margin == null) return;

        var phantomLines = margin.GetPhantomLineNumbers();
        if (phantomLines.Count == 0) return;

        int caret = ResolvedEditor.CaretOffset;
        var currentLine = ResolvedEditor.Document.GetLineByOffset(caret);

        if (phantomLines.Contains(currentLine.LineNumber))
        {
            // Remove from phantom anchors before text is inserted
            var anchorToRemove = margin.PhantomAnchors.FirstOrDefault(a => !a.IsDeleted && a.Line == currentLine.LineNumber);
            if (anchorToRemove != null)
            {
                margin.PhantomAnchors.Remove(anchorToRemove);
                margin.InvalidateVisual();
            }
        }
    }

    private void OnResolvedEditorPreviewKeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (ResolvedEditor.Document == null) return;

        var margin = ResolvedEditor.TextArea.LeftMargins.OfType<DiffLineNumberMargin>().FirstOrDefault();
        if (margin == null) return;

        var phantomLines = margin.GetPhantomLineNumbers();
        if (phantomLines.Count == 0) return;

        int caret = ResolvedEditor.CaretOffset;
        int selectionLength = ResolvedEditor.SelectionLength;

        var currentLine = ResolvedEditor.Document.GetLineByOffset(caret);
        bool isOnPhantomLine = phantomLines.Contains(currentLine.LineNumber);

        // Handle Enter on phantom lines - replace phantom line with text, don't create new line
        if (isOnPhantomLine && selectionLength == 0 && e.Key == Avalonia.Input.Key.Enter)
        {
            e.Handled = true;

            // Remove from phantom anchors
            var anchorToRemove = margin.PhantomAnchors.FirstOrDefault(a => !a.IsDeleted && a.Line == currentLine.LineNumber);
            if (anchorToRemove != null)
            {
                margin.PhantomAnchors.Remove(anchorToRemove);
            }

            // If we're at the start of the phantom line, just convert it to real line
            // If we're in the middle/end, we need to split properly
            if (caret == currentLine.Offset)
            {
                // At start - just convert phantom to real by doing nothing, line stays empty
                margin.InvalidateVisual();
            }
            else
            {
                // Insert newline normally
                ResolvedEditor.Document.Insert(caret, "\n");
                ResolvedEditor.CaretOffset = caret + 1;
                margin.InvalidateVisual();
            }
            return;
        }

        // Handle Tab and Space on phantom lines - just remove phantom status
        if (isOnPhantomLine && selectionLength == 0 && (e.Key == Avalonia.Input.Key.Tab || e.Key == Avalonia.Input.Key.Space))
        {
            var anchorToRemove = margin.PhantomAnchors.FirstOrDefault(a => !a.IsDeleted && a.Line == currentLine.LineNumber);
            if (anchorToRemove != null)
            {
                margin.PhantomAnchors.Remove(anchorToRemove);
                margin.InvalidateVisual();
            }
            // Let the key proceed normally
            return;
        }

        // Handle Backspace and Delete
        if (e.Key != Avalonia.Input.Key.Back && e.Key != Avalonia.Input.Key.Delete) return;

        // If something is selected, let it be deleted, the alignment will fix itself
        if (selectionLength > 0) return;

        if (e.Key == Avalonia.Input.Key.Back && caret > 0)
        {
            if (currentLine.Offset == caret && phantomLines.Contains(currentLine.LineNumber))
            {
                e.Handled = true;
            }
            else if (currentLine.Length == 0 && phantomLines.Contains(currentLine.LineNumber))
            {
                e.Handled = true;
            }
        }
        else if (e.Key == Avalonia.Input.Key.Delete && caret < ResolvedEditor.Document.TextLength)
        {
            if (caret == currentLine.Offset + currentLine.Length && currentLine.NextLine != null && phantomLines.Contains(currentLine.NextLine.LineNumber))
            {
                e.Handled = true;
            }
            else if (currentLine.Length == 0 && phantomLines.Contains(currentLine.LineNumber))
            {
                e.Handled = true;
            }
        }
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

    private async void OnResolvedEditorTextChanged(object? sender, EventArgs e)
    {
        if (_isUpdatingAlignment || _currentViewModel?.SelectedPackage?.Context == null) return;

        PromoteModifiedPhantomLines(ResolvedEditor);

        _alignmentCts?.Cancel();
        _alignmentCts = new CancellationTokenSource();
        var token = _alignmentCts.Token;

        try
        {
            await Task.Delay(300, token);
            if (token.IsCancellationRequested) return;

            await PerformAdaptiveAlignmentAsync(token);
        }
        catch (TaskCanceledException) { }
    }

    private void PromoteModifiedPhantomLines(TextEditor editor)
    {
        var margin = editor.TextArea.LeftMargins.OfType<DiffLineNumberMargin>().FirstOrDefault();
        if (margin == null || margin.PhantomAnchors.Count == 0) return;

        var toRemove = new List<AvaloniaEdit.Document.TextAnchor>();
        foreach (var anchor in margin.PhantomAnchors.ToList())
        {
            if (anchor.IsDeleted)
            {
                toRemove.Add(anchor);
                continue;
            }

            if (anchor.Line > editor.Document.LineCount)
            {
                toRemove.Add(anchor);
                continue;
            }

            var line = editor.Document.GetLineByNumber(anchor.Line);
            if (line.Length > 0)
            {
                toRemove.Add(anchor);
            }
        }

        foreach (var a in toRemove)
        {
            margin.PhantomAnchors.Remove(a);
        }

        if (toRemove.Count > 0)
        {
            margin.InvalidateVisual();
        }
    }

    private string GetCleanText(TextEditor editor)
    {
        var margin = editor.TextArea.LeftMargins.OfType<DiffLineNumberMargin>().FirstOrDefault();
        if (margin == null || margin.PhantomAnchors.Count == 0) return editor.Text;

        var phantomLines = margin.GetPhantomLineNumbers();
        if (phantomLines.Count == 0) return editor.Text;

        var cleanLines = new List<string>();
        for (int i = 1; i <= editor.Document.LineCount; i++)
        {
            if (!phantomLines.Contains(i))
            {
                var line = editor.Document.GetLineByNumber(i);
                cleanLines.Add(editor.Document.GetText(line));
            }
        }
        return string.Join("\n", cleanLines);
    }

    private async Task PerformAdaptiveAlignmentAsync(CancellationToken token)
    {
        var context = _currentViewModel?.SelectedPackage?.Context;
        if (context == null) return;

        string cleanResolvedText = GetCleanText(ResolvedEditor);

        var gaps = await Task.Run(() => 
        {
            var differService = App.ServiceProvider?.GetService<SqlDifferService>();
            if (differService != null)
            {
                differService.UpdateResolvedDiff(context, cleanResolvedText);
            }

            var syncBlocks = DiffMapper.BuildSyncBlocks(context);
            var calculator = new EditorAlignmentCalculator();
            return calculator.CalculateGaps(syncBlocks);
        }, token);

        if (token.IsCancellationRequested) return;

        bool localChanged = !AreGapsSame(LocalEditor, gaps.LocalGaps);
        bool targetChanged = !AreGapsSame(TargetEditor, gaps.TargetGaps);
        bool resolvedChanged = !AreGapsSame(ResolvedEditor, gaps.ResolvedGaps);

        if (!localChanged && !targetChanged && !resolvedChanged) return;

        _isUpdatingAlignment = true;
        try
        {
            var localAnchor = LocalEditor.Document.CreateAnchor(LocalEditor.CaretOffset);
            var targetAnchor = TargetEditor.Document.CreateAnchor(TargetEditor.CaretOffset);
            var resolvedAnchor = ResolvedEditor.Document.CreateAnchor(ResolvedEditor.CaretOffset);

            localAnchor.MovementType = AvaloniaEdit.Document.AnchorMovementType.BeforeInsertion;
            targetAnchor.MovementType = AvaloniaEdit.Document.AnchorMovementType.BeforeInsertion;
            resolvedAnchor.MovementType = AvaloniaEdit.Document.AnchorMovementType.BeforeInsertion;

            double localScroll = LocalEditor.TextArea.TextView.ScrollOffset.Y;
            double targetScroll = TargetEditor.TextArea.TextView.ScrollOffset.Y;
            double resolvedScroll = ResolvedEditor.TextArea.TextView.ScrollOffset.Y;

            LocalEditor.Document.UndoStack.StartUndoGroup();
            TargetEditor.Document.UndoStack.StartUndoGroup();
            ResolvedEditor.Document.UndoStack.StartUndoGroup();

            if (localChanged) ApplyGapsSurgically(LocalEditor, gaps.LocalGaps);
            if (targetChanged) ApplyGapsSurgically(TargetEditor, gaps.TargetGaps);
            if (resolvedChanged) ApplyGapsSurgically(ResolvedEditor, gaps.ResolvedGaps);

            LocalEditor.Document.UndoStack.EndUndoGroup();
            TargetEditor.Document.UndoStack.EndUndoGroup();
            ResolvedEditor.Document.UndoStack.EndUndoGroup();

            LocalEditor.CaretOffset = localAnchor.Offset;
            TargetEditor.CaretOffset = targetAnchor.Offset;
            ResolvedEditor.CaretOffset = resolvedAnchor.Offset;

            LocalEditor.ScrollToVerticalOffset(localScroll);
            TargetEditor.ScrollToVerticalOffset(targetScroll);
            ResolvedEditor.ScrollToVerticalOffset(resolvedScroll);

            var localRegions = DiffMapper.GetLocalRegions(context);
            var targetRegions = DiffMapper.GetTargetRegions(context);
            var resolvedRegions = DiffMapper.GetResolvedRegions(context);

            ShiftRegionsForEditor(LocalEditor, localRegions);
            ShiftRegionsForEditor(TargetEditor, targetRegions);
            ShiftRegionsForEditor(ResolvedEditor, resolvedRegions);

            UpdateBackgroundRenderers(LocalEditor, localRegions);
            UpdateBackgroundRenderers(TargetEditor, targetRegions);
            UpdateBackgroundRenderers(ResolvedEditor, resolvedRegions);
        }
        finally
        {
            _isUpdatingAlignment = false;
        }
    }

    private bool AreGapsSame(TextEditor editor, Dictionary<int, int> desiredGaps)
    {
        var margin = editor.TextArea.LeftMargins.OfType<DiffLineNumberMargin>().FirstOrDefault();
        if (margin == null) return desiredGaps.Count == 0;

        var currentGaps = margin.GetCurrentGapsByCleanLine();
        if (currentGaps.Count != desiredGaps.Count) return false;

        foreach (var kv in desiredGaps)
        {
            if (!currentGaps.TryGetValue(kv.Key, out int count) || count != kv.Value) return false;
        }
        return true;
    }

    private void ApplyGapsSurgically(TextEditor editor, Dictionary<int, int> desiredGaps)
    {
        editor.RemovePhantomSpaces();
        
        var adjustedGaps = new Dictionary<int, int>();
        
        // Получаем физическую строку, на которой сейчас стоит курсор (если редактор в фокусе)
        int caretLine = editor.IsFocused ? editor.Document.GetLineByOffset(editor.CaretOffset).LineNumber : -1;

        foreach (var kvp in desiredGaps)
        {
            int insertLine = kvp.Key;
            int count = kvp.Value;

            // 1. БЕЗОПАСНЫЙ СДВИГ: Пропускаем все строки, состоящие только из пустоты (Enter, Space, Tab)
            // Это гарантирует, что пустые строки останутся "приклеенными" к блоку кода сверху.
            while (insertLine < editor.Document.LineCount)
            {
                var nextLine = editor.Document.GetLineByNumber(insertLine + 1);
                string text = editor.Document.GetText(nextLine);
                
                if (string.IsNullOrWhiteSpace(text))
                {
                    insertLine++;
                }
                else
                {
                    break;
                }
            }

            // 2. СДВИГ ЗА КУРСОРОМ: Обрабатываем комментарии
            // Если пользователь сфокусирован и пишет комментарий (который BLL тоже игнорирует),
            // мы принудительно спускаем фантомный блок ПОД курсор, чтобы не отталкивать его.
            if (editor.IsFocused && insertLine < caretLine)
            {
                bool onlyTrivia = true;
                for (int i = insertLine + 1; i <= caretLine; i++)
                {
                    string text = editor.Document.GetText(editor.Document.GetLineByNumber(i));
                    // Если между предполагаемым разрывом и курсором есть реальный код - сдвигать нельзя
                    if (!string.IsNullOrWhiteSpace(text) && !text.TrimStart().StartsWith("--"))
                    {
                        onlyTrivia = false;
                        break;
                    }
                }

                if (onlyTrivia)
                {
                    insertLine = caretLine;
                }
            }

            // Группируем отступы, если вдруг два сдвинутых блока наслоились друг на друга
            if (adjustedGaps.ContainsKey(insertLine))
                adjustedGaps[insertLine] += count;
            else
                adjustedGaps[insertLine] = count;
        }

        // 3. Вставляем скорректированные отступы строго снизу вверх
        var sortedGaps = adjustedGaps.OrderByDescending(g => g.Key);
        foreach (var gap in sortedGaps)
        {
            editor.InsertPhantomSpace(gap.Key, gap.Value);
        }
    }

    private void ClearPhantomLines(TextEditor editor)
    {
        var margin = editor.TextArea.LeftMargins.OfType<DiffLineNumberMargin>().FirstOrDefault();
        if (margin != null)
        {
            margin.PhantomAnchors.Clear();
            margin.InvalidateVisual();
        }
    }

    private void LoadPackageIntoEditors(PackageItemViewModel? package)
    {
        _isUpdatingAlignment = true;
        try
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

            if (package.Context == null) return;

            var differService = App.ServiceProvider?.GetService<SqlDifferService>();
            if (differService != null)
            {
                differService.UpdateResolvedDiff(package.Context, package.ResolvedText);
            }

            List<SyncBlock> syncBlocks = DiffMapper.BuildSyncBlocks(package.Context);
            
            var calculator = new EditorAlignmentCalculator();
            var gaps = calculator.CalculateGaps(syncBlocks);

            var localRegions = DiffMapper.GetLocalRegions(package.Context);
            var targetRegions = DiffMapper.GetTargetRegions(package.Context);
            var resolvedRegions = DiffMapper.GetResolvedRegions(package.Context);

            ApplyGapsSurgically(LocalEditor, gaps.LocalGaps);
            ApplyGapsSurgically(TargetEditor, gaps.TargetGaps);
            ApplyGapsSurgically(ResolvedEditor, gaps.ResolvedGaps);

            ShiftRegionsForEditor(LocalEditor, localRegions);
            ShiftRegionsForEditor(TargetEditor, targetRegions);
            ShiftRegionsForEditor(ResolvedEditor, resolvedRegions);

            UpdateBackgroundRenderers(LocalEditor, localRegions);
            UpdateBackgroundRenderers(TargetEditor, targetRegions);
            UpdateBackgroundRenderers(ResolvedEditor, resolvedRegions);
        }
        finally
        {
            _isUpdatingAlignment = false;
        }
    }

    private void UpdateBackgroundRenderers(TextEditor editor, List<HighlightRegion> regions)
    {
        var renderers = editor.TextArea.TextView.BackgroundRenderers;
        for (int i = renderers.Count - 1; i >= 0; i--)
        {
            if (renderers[i] is DiffBackgroundRenderer) renderers.RemoveAt(i);
        }

        if (regions != null && regions.Count > 0)
        {
            var margin = editor.TextArea.LeftMargins.OfType<DiffLineNumberMargin>().FirstOrDefault();
            var phantomLines = margin?.GetPhantomLineNumbers().ToList() ?? new List<int>();
            renderers.Add(new DiffBackgroundRenderer(editor.TextArea.TextView, regions, phantomLines));
        }
        
        editor.TextArea.TextView.InvalidateLayer(AvaloniaEdit.Rendering.KnownLayer.Background);
    }

    private void ShiftRegionsForEditor(TextEditor editor, List<HighlightRegion> regions)
    {
        if (regions == null || regions.Count == 0) return;

        var margin = editor.TextArea.LeftMargins.OfType<DiffLineNumberMargin>().FirstOrDefault();
        if (margin != null)
        {
            var phantomLines = margin.GetPhantomLineNumbers();
            if (phantomLines.Count > 0)
            {
                RegionMapper.ShiftRegions(regions, phantomLines);
            }
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