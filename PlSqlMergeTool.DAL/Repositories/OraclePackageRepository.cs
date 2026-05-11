using System.Text;
using Dapper;
using Oracle.ManagedDataAccess.Client;
using PlSqlMergeTool.BLL.Interfaces;
using PlSqlMergeTool.BLL.Models;

namespace PlSqlMergeTool.DAL.Repositories;

public class OracleRepository : IOracleRepository // todo разделить поведение на разные интерфейсы
{
    public void CompilePackage(string сonnectionString, string resolvedSqlText)
    {
        using var connection = new OracleConnection(сonnectionString);
        connection.Execute(resolvedSqlText);
    }

    public IEnumerable<OracleCompileError> GetCompilationErrors(string сonnectionString, string packageName)
    {
        using var connection = new OracleConnection(сonnectionString);
        
        const string sql = @"
            SELECT LINE as Line, 
                   POSITION as Position, 
                   TEXT as ErrorText 
            FROM ALL_ERRORS 
            WHERE NAME = :PackageName 
              AND TYPE IN ('PACKAGE', 'PACKAGE BODY')
            ORDER BY TYPE, SEQUENCE";

        return connection.Query<OracleCompileError>(sql, new { PackageName = packageName });
    }

    public IEnumerable<string> GetPackageNames(string connectionString)
    {
        using var connection = new OracleConnection(connectionString);
        
        const string sql = @"
            SELECT DISTINCT NAME 
            FROM ALL_SOURCE 
            WHERE TYPE IN ('PACKAGE', 'PACKAGE BODY')
            ORDER BY NAME";
            
        return connection.Query<string>(sql);
    }

    public string GetPackageSource(string connectionString, string packageName)
    {
        using var connection = new OracleConnection(connectionString);
        
        const string sql = @"
            SELECT TEXT 
            FROM ALL_SOURCE 
            WHERE NAME = :PackageName 
              AND TYPE IN ('PACKAGE', 'PACKAGE BODY')
            ORDER BY TYPE, LINE"; 

        var lines = connection.Query<string>(sql, new { PackageName = packageName });
        
        var sb = new StringBuilder();
        foreach (var line in lines)
        {
            sb.Append(line);
        }
        
        return sb.ToString();
    }

    public IEnumerable<TableColumnMetadata> GetSchemaTablesMetadata(string connectionString)
    {
        using var connection = new OracleConnection(connectionString);
        
        const string sql = @"
            SELECT c.TABLE_NAME as ObjectName, 
                o.OBJECT_TYPE as ObjectType,
                c.COLUMN_NAME as ColumnName, 
                c.DATA_TYPE as DataType 
            FROM USER_TAB_COLUMNS c
            JOIN USER_OBJECTS o ON c.TABLE_NAME = o.OBJECT_NAME
            WHERE o.OBJECT_TYPE IN ('TABLE', 'VIEW')
            ORDER BY c.TABLE_NAME, c.COLUMN_ID";

        return connection.Query<TableColumnMetadata>(sql);
    }
}