using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PlSqlMergeTool.BLL.Interfaces;
using PlSqlMergeTool.BLL.Models;

namespace PlSqlMergeTool.BLL.Services;

public class ConfigService : IConfigService
{
    private readonly string _filePath;
    
    private static readonly byte[] Salt = [0x4F, 0x72, 0x61, 0x63, 0x6C, 0x65, 0x4D, 0x65];

    public ConfigService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appData, "PlSqlMergeTool");
        Directory.CreateDirectory(appFolder);
        _filePath = Path.Combine(appFolder, "settings.json");
    }

    public WorkspaceConnectionConfig LoadConfig()
    {
        if (!File.Exists(_filePath))
            return CreateEmptyConfig();

        try
        {
            var json = File.ReadAllText(_filePath);
            var encryptedConfig = JsonSerializer.Deserialize<WorkspaceConnectionConfig>(json);
            
            if (encryptedConfig == null) return CreateEmptyConfig();

            return new WorkspaceConnectionConfig
            {
                BaselineConnection = Decrypt(encryptedConfig.BaselineConnection),
                LocalConnection = Decrypt(encryptedConfig.LocalConnection),
                TargetConnection = Decrypt(encryptedConfig.TargetConnection)
            };
        }
        catch
        {
            return CreateEmptyConfig();
        }
    }

    public void SaveConfig(WorkspaceConnectionConfig config)
    {
        var encryptedConfig = new WorkspaceConnectionConfig
        {
            BaselineConnection = Encrypt(config.BaselineConnection),
            LocalConnection = Encrypt(config.LocalConnection),
            TargetConnection = Encrypt(config.TargetConnection)
        };

        var json = JsonSerializer.Serialize(encryptedConfig, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }

    private static WorkspaceConnectionConfig CreateEmptyConfig() => 
        new() { BaselineConnection = "", LocalConnection = "", TargetConnection = "" };


    private static byte[] GetMachineSpecificKey()
    {
        string seed = Environment.MachineName + Environment.UserName;
        using var rfc2898 = new Rfc2898DeriveBytes(seed, Salt, 10000, HashAlgorithmName.SHA256);
        return rfc2898.GetBytes(32); // 32 байта = 256 бит для AES
    }

    private string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return "";

        using var aes = Aes.Create();
        aes.Key = GetMachineSpecificKey();
        aes.GenerateIV();
        
        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        
        ms.Write(aes.IV, 0, aes.IV.Length);
        
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }
        
        return Convert.ToBase64String(ms.ToArray());
    }

    private string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return "";
        
        try
        {
            var fullCipher = Convert.FromBase64String(cipherText);
            using var aes = Aes.Create();
            aes.Key = GetMachineSpecificKey();
            
            byte[] iv = new byte[aes.BlockSize / 8];
            Array.Copy(fullCipher, 0, iv, 0, iv.Length);
            aes.IV = iv;
            
            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using var ms = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            
            return sr.ReadToEnd();
        }
        catch
        {
            return "";
        }
    }
}