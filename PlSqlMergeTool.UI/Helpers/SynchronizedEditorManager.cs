using Avalonia;
using AvaloniaEdit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlSqlMergeTool.UI.Helpers;

public sealed class SynchronizedEditorManager : IDisposable
{
    private readonly List<TextEditor> _editors = new();
    
    private bool _isSyncing;

    public void RegisterEditor(TextEditor editor)
    {
        if (_editors.Contains(editor)) return;

        _editors.Add(editor);
        if (editor.TextArea?.TextView != null)
        {
            editor.TextArea.TextView.ScrollOffsetChanged += OnScrollOffsetChanged;
        }
    }

    public void UnregisterEditor(TextEditor editor)
    {
        if (!_editors.Contains(editor)) return;

        if (editor.TextArea?.TextView != null)
        {
            editor.TextArea.TextView.ScrollOffsetChanged -= OnScrollOffsetChanged;
        }

        _editors.Remove(editor);
    }

    private void OnScrollOffsetChanged(object? sender, EventArgs e)
    {
        if (_isSyncing) return;

        var sourceView = sender as AvaloniaEdit.Rendering.TextView;
        if (sourceView == null) return;

        var sourceEditor = _editors.FirstOrDefault(ed => ed.TextArea?.TextView == sourceView);
        if (sourceEditor == null) return;

        _isSyncing = true;
        try
        {
            var offset = sourceView.ScrollOffset;

            foreach (var editor in _editors)
            {
                if (editor != sourceEditor && editor.TextArea?.TextView != null)
                {
                    editor.ScrollToHorizontalOffset(offset.X);
                    editor.ScrollToVerticalOffset(offset.Y);
                }
            }
        }
        finally
        {
            _isSyncing = false;
        }
    }

    public void Dispose()
    {
        foreach (var editor in _editors.ToList())
        {
            UnregisterEditor(editor);
        }
        _editors.Clear();
    }
}