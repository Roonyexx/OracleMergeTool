using PlSqlMergeTool.BLL.Models;

namespace PlSqlMergeTool.BLL.Interfaces;

public interface IConfigService
{
    WorkspaceConnectionConfig LoadConfig();
    
    void SaveConfig(WorkspaceConnectionConfig config);
}