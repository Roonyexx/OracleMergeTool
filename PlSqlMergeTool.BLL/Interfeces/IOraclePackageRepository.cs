using System.Collections.Generic;

namespace PlSqlMergeTool.BLL.Interfaces;

public interface IOraclePackageRepository
{
    // получить список всех пакетов в схеме
    IEnumerable<string> GetPackageNames(string connectionString);

    // извлечь исходный код конкретного пакета
    string GetPackageSource(string connectionString, string packageName);
}