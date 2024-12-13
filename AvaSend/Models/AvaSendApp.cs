using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvaSend.Models;

/// <summary>
/// AvaSendApp
/// 用于保存应用程序各项的信息的数据结构
/// </summary>
public class AvaSendApp
{
    /// <summary>
    /// 应用程序名称
    /// </summary>
    public string AppName { get; set; }

    /// <summary>
    /// 应用程序版本
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    /// 应用程序配置
    /// </summary>
    public Dictionary<string, string> Configurations { get; set; }

    /// <summary>
    /// 当前用户
    /// </summary>
    public string CurrentUser { get; set; }

    /// <summary>
    /// 日志文件路径
    /// </summary>
    public string LogFilePath { get; set; }

    /// <summary>
    /// 网络连接状态
    /// </summary>
    public bool IsConnected { get; set; }

    /// <summary>
    /// 构造函数
    /// </summary>
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
        return Configurations.ContainsKey(key) ? Configurations[key] : null;
    }
}
