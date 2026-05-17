using System.Collections.Generic;
using System.Linq;

namespace PlSqlMergeTool.UI.Helpers;

public static class RegionMapper
{
    public static void ShiftRegions(List<HighlightRegion> regions, HashSet<int> phantomLines)
    {
        if (phantomLines == null || phantomLines.Count == 0) return;

        // Сортируем фантомные строки по возрастанию для правильного алгоритма сдвига
        var sortedPhantoms = phantomLines.OrderBy(p => p).ToList();

        foreach (var region in regions)
        {
            region.StartLine = GetPhysicalLine(region.StartLine, sortedPhantoms);
            region.EndLine = GetPhysicalLine(region.EndLine, sortedPhantoms);
        }
    }

    private static int GetPhysicalLine(int logicalLine, List<int> sortedPhantoms)
    {
        int physicalLine = logicalLine;
        
        foreach (var phantom in sortedPhantoms)
        {
            if (phantom <= physicalLine)
            {
                physicalLine++;
            }
        }
        
        return physicalLine;
    }
}