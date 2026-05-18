using PlSqlMergeTool.BLL.Models;
using PlSqlMergeTool.BLL.MergeLogic;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlSqlMergeTool.BLL.Services;

public class SqlBuilderService
{
    public (string Sql, List<ResolvedRegion> Regions) BuildSql(IList<PlSqlToken> resolvedCleanTokens, List<AppliedTokenEdit>? edits = null)
    {
        var sb = new StringBuilder();
        var regions = new List<ResolvedRegion>();
        
        int currentLine = 1;
        int currentTokenIndex = 0;
        
        var editsQueue = edits != null 
            ? new Queue<AppliedTokenEdit>(edits.OrderBy(e => e.StartTokenIndex)) 
            : new Queue<AppliedTokenEdit>();
            
        AppliedTokenEdit? currentEdit = editsQueue.Count > 0 ? editsQueue.Dequeue() : null;
        int editStartLine = -1;

        foreach (var token in resolvedCleanTokens)
        {
            if (token.LeadingTrivia != null)
            {
                foreach (var trivia in token.LeadingTrivia)
                {
                    trivia.Offset = sb.Length;
                    sb.Append(trivia.Text);
                    currentLine += CountNewlines(trivia.Text);
                }
            }

            if (currentEdit != null && currentTokenIndex == currentEdit.StartTokenIndex)
            {
                editStartLine = currentLine;
            }

            token.Offset = sb.Length;
            sb.Append(token.Text);
            currentLine += CountNewlines(token.Text);

            if (token.TrailingTrivia != null)
            {
                foreach (var trivia in token.TrailingTrivia)
                {
                    trivia.Offset = sb.Length;
                    sb.Append(trivia.Text);
                    currentLine += CountNewlines(trivia.Text);
                }
            }

            currentTokenIndex++;

            if (currentEdit != null && currentTokenIndex == currentEdit.StartTokenIndex + currentEdit.TokenCount)
            {
                regions.Add(new ResolvedRegion
                {
                    StartLine = editStartLine,
                    EndLine = currentLine,
                    Source = currentEdit.Source
                });
                
                currentEdit = editsQueue.Count > 0 ? editsQueue.Dequeue() : null;
            }
        }

        return (sb.ToString(), regions);
    }

    private static int CountNewlines(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        int count = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n') count++;
        }
        return count;
    }
}