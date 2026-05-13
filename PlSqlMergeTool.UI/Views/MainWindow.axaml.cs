using Avalonia.Controls;
using AvaloniaEdit;
using PlSqlMergeTool.UI.Helpers;
using PlSqlMergeTool.UI.ViewModels;
using PlSqlMergeTool.UI.ViewModels.Items;
using System;

namespace PlSqlMergeTool.UI.Views;

public partial class MainWindow : Window
{
    private bool _isSyncingScroll = false;
    
    private MainWindowViewModel? _currentViewModel;

    public MainWindow()
    {
        InitializeComponent();
        
        SetupSynchronizedScrolling();

        DataContextChanged += OnDataContextChanged;
    }

    private void SetupSynchronizedScrolling()
    {
        LocalEditor.TextArea.TextView.ScrollOffsetChanged += (s, e) => SyncScroll(LocalEditor);
        ResolvedEditor.TextArea.TextView.ScrollOffsetChanged += (s, e) => SyncScroll(ResolvedEditor);
        TargetEditor.TextArea.TextView.ScrollOffsetChanged += (s, e) => SyncScroll(TargetEditor);
    }

    private void SyncScroll(TextEditor source)
    {
        if (_isSyncingScroll) return;
        _isSyncingScroll = true;

        double offset = source.VerticalOffset;

        if (source != LocalEditor) LocalEditor.ScrollToVerticalOffset(offset);
        if (source != ResolvedEditor) ResolvedEditor.ScrollToVerticalOffset(offset);
        if (source != TargetEditor) TargetEditor.ScrollToVerticalOffset(offset);

        _isSyncingScroll = false;
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
        ClearDiffColorizers(LocalEditor);
        ClearDiffColorizers(TargetEditor);
        ClearDiffColorizers(ResolvedEditor);

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

        var localRegions = DiffMapper.GetLocalRegions(package.Context);
        var targetRegions = DiffMapper.GetTargetRegions(package.Context);
        var resolvedRegions = DiffMapper.GetResolvedRegions(package.Context);

        LocalEditor.TextArea.TextView.LineTransformers.Add(new DiffColorizer(localRegions));
        TargetEditor.TextArea.TextView.LineTransformers.Add(new DiffColorizer(targetRegions));
        ResolvedEditor.TextArea.TextView.LineTransformers.Add(new DiffColorizer(resolvedRegions));
    }

    private void ClearDiffColorizers(TextEditor editor)
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
}