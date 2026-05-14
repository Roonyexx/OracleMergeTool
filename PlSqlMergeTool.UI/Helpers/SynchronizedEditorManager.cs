using Avalonia;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlSqlMergeTool.UI.Helpers;

public sealed class SynchronizedEditorManager : IDisposable
{
    private readonly List<TextEditor> _editors = new();
    private readonly Dictionary<TextEditor, PhantomLineTransformer> _transformers = new();
    private bool _isSyncing;

    public void RegisterEditor(TextEditor editor)
    {
        if (_editors.Contains(editor)) return;

        _editors.Add(editor);
        editor.TextArea.TextView.ScrollOffsetChanged += OnScrollOffsetChanged;

        var transformer = new PhantomLineTransformer();
        _transformers[editor] = transformer;
        editor.TextArea.TextView.BackgroundRenderers.Add(transformer);
    }

    public void UnregisterEditor(TextEditor editor)
    {
        if (!_editors.Contains(editor)) return;

        editor.TextArea.TextView.ScrollOffsetChanged -= OnScrollOffsetChanged;

        if (_transformers.TryGetValue(editor, out var transformer))
        {
            editor.TextArea.TextView.BackgroundRenderers.Remove(transformer);
            _transformers.Remove(editor);
        }

        _editors.Remove(editor);
    }

    public void SynchronizeLineHeights()
    {
        if (_editors.Count == 0) return;

        var maxLines = _editors.Max(e => e.Document.LineCount);

        foreach (var editor in _editors)
        {
            var currentLines = editor.Document.LineCount;
            var phantomLines = maxLines - currentLines;

            if (_transformers.TryGetValue(editor, out var transformer))
            {
                transformer.PhantomLineCount = phantomLines;
                editor.TextArea.TextView.Redraw();
            }
        }
    }

    private void OnScrollOffsetChanged(object? sender, EventArgs e)
    {
        if (_isSyncing) return;

        var sourceEditor = _editors.FirstOrDefault(ed => ed.TextArea.TextView == sender);
        if (sourceEditor == null) return;

        _isSyncing = true;

        var offset = sourceEditor.VerticalOffset;
        foreach (var editor in _editors)
        {
            if (editor != sourceEditor)
            {
                editor.ScrollToVerticalOffset(offset);
            }
        }

        _isSyncing = false;
    }

    public void Dispose()
    {
        foreach (var editor in _editors.ToList())
        {
            UnregisterEditor(editor);
        }
        _editors.Clear();
        _transformers.Clear();
    }
}

public class PhantomLineTransformer : IBackgroundRenderer
{
    public int PhantomLineCount { get; set; }

    public KnownLayer Layer => KnownLayer.Background;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (PhantomLineCount <= 0) return;

        var document = textView.Document;
        if (document == null) return;

        var lastLine = document.GetLineByNumber(document.LineCount);
        var lastLineBottom = textView.GetVisualPosition(new TextViewPosition(document.LineCount, 0), VisualYPosition.LineBottom);

        var lineHeight = textView.DefaultLineHeight;
        var phantomHeight = PhantomLineCount * lineHeight;
        var startY = lastLineBottom.Y;

        var brush = new SolidColorBrush(Color.FromArgb(10, 128, 128, 128));
        var rect = new Rect(0, startY, textView.Bounds.Width, phantomHeight);

        drawingContext.DrawRectangle(brush, null, rect);
    }
}
