using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using System;
using System.IO;
using System.Reflection;
using System.Xml;

namespace PlSqlMergeTool.UI.Helpers;

public static class PlSqlSyntaxHighlighting
{
    public static IHighlightingDefinition LoadPlSqlHighlighting()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "PlSqlMergeTool.UI.Resources.PlSql-Dark.xshd";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            return CreateFallbackHighlighting();
        }

        using var reader = new XmlTextReader(stream);
        return HighlightingLoader.Load(reader, HighlightingManager.Instance);
    }

    private static IHighlightingDefinition CreateFallbackHighlighting()
    {
        return HighlightingManager.Instance.GetDefinition("C#")
               ?? HighlightingManager.Instance.GetDefinition("XML")
               ?? throw new InvalidOperationException("No fallback highlighting available");
    }
}
