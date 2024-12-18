using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AvaSend.Models;

/// <summary>
/// AvaSendApp
/// 用于保存应用程序各项的信息的数据结构
/// </summary>
public class AvaSendApp
{
    private const string ConfigFileName = "AppSettings.json";

    public string AppName { get; set; }

    public string Version { get; set; }

    public Dictionary<string, string> Configurations { get; set; }

    public string CurrentUser { get; set; }

    public string LogFilePath { get; set; }

    public bool IsConnected { get; set; }

    public AvaSendApp()
    {
        Configurations = new Dictionary<string, string>();
    }

    /// <summary>
    /// 添加或更新配置项
    /// </summary>
    /// <param name="key">配置项键</param>
    /// <param name="value">配置项值</param>
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

    /// <summary>
    /// 获取配置项
    /// </summary>
    /// <param name="key">配置项键</param>
    /// <returns>配置项值</returns>
    public string GetConfiguration(string key)
    {
        return Configurations.TryGetValue(key, out string? value) ? value : null;
    }

    /// <summary>
    /// 从JSON文件加载配置
    /// </summary>
    public void LoadConfigurations()
    {
        string filePath = Path.Combine(AppContext.BaseDirectory, ConfigFileName);
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            var config = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);
            if (config != null && config.TryGetValue("AppSettings", out Dictionary<string, string>? value))
            {
                Configurations = value;
            }
        }
    }

    /// <summary>
    /// 保存配置到JSON文件
    /// </summary>
    public void SaveConfigurations()
    {
        var config = new Dictionary<string, Dictionary<string, string>>
        {
            { "AppSettings", Configurations }
        };
        string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        string filePath = Path.Combine(AppContext.BaseDirectory, ConfigFileName);
        File.WriteAllText(filePath, json);
    }
}
