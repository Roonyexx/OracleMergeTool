using System.Collections.Generic;
using PlSqlMergeTool.BLL.Models;

namespace PlSqlMergeTool.BLL.Interfaces;

public interface IOracleRepository
{
    // получить список всех пакетов в схеме
    IEnumerable<string> GetPackageNames(string connectionString);

    // извлечь исходный код конкретного пакета
    string GetPackageSource(string connectionString, string packageName);

    IEnumerable<TableColumnMetadata> GetSchemaTablesMetadata(string connectionString);

    void CompilePackage(string targetConnectionString, string resolvedSqlText);

    IEnumerable<OracleCompileError> GetCompilationErrors(string targetConnectionString, string packageName);
}