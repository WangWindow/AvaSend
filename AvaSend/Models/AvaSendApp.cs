using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AvaSend.Models;

/// <summary>
/// AvaSend 应用程序。
/// </summary>
public class AvaSendApp
{
    private const string ConfigFileName = "AppSettings.json";

    public string AppName { get; set; }

    public string Version { get; set; }

    public Dictionary<string, string> Configurations { get; set; }

    public string UserName { get; set; }

    public string LogFilePath { get; set; }

    public bool IsConnected { get; set; }

    public AvaSendApp()
    {
        Configurations = new Dictionary<string, string>();
    }

    // 添加或更新配置
    public void AddOrUpdateConfiguration(string key, string value)
    {
        if (Configurations.ContainsKey(key))
        {
            Configurations[key] = value;
        }
        else
        {
            Configurations.Add(key, value);
        }
    }

    // 获取配置
    public string GetConfiguration(string key)
    {
        return Configurations.TryGetValue(key, out string? value) ? value : null;
    }

    // 加载配置文件
    public bool LoadConfigurations()
    {
        try
        {
            string filePath = Path.Combine(AppContext.BaseDirectory, ConfigFileName);
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                var config = JsonSerializer.Deserialize<
                    Dictionary<string, Dictionary<string, string>>
                >(json);
                if (
                    config != null
                    && config.TryGetValue("AppSettings", out Dictionary<string, string>? value)
                )
                {
                    Configurations = value;
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            // 可以添加日志记录异常信息
            Console.WriteLine($"加载配置时发生错误: {ex.Message}");
            return false;
        }
    }

    // 保存配置文件
    public bool SaveConfigurations()
    {
        try
        {
            var config = new Dictionary<string, Dictionary<string, string>>
            {
                { "AppSettings", Configurations },
            };
            string json = JsonSerializer.Serialize(
                config,
                new JsonSerializerOptions { WriteIndented = true }
            );
            string filePath = Path.Combine(AppContext.BaseDirectory, ConfigFileName);
            File.WriteAllText(filePath, json);
            return true;
        }
        catch (Exception ex)
        {
            // 可以添加日志记录异常信息
            Console.WriteLine($"保存配置时发生错误: {ex.Message}");
            return false;
        }
    }
}
