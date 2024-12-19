using System;
using System.IO;
using System.Linq;
using ReactiveUI;

namespace AvaSend.Models;

/// <summary>
/// 数据服务
/// </summary>
public class DataService : ReactiveObject
{
    // 单例模式
    public static DataService Instance => _instance.Value;
    private static readonly Lazy<DataService> _instance = new(() => new DataService());

    private DataService()
    {
        _avaSendApp = new AvaSendApp();
        SaveFolderPath = ResolvePath("~/Downloads/AvaSend");
        LoadSettings();
    }

    // 数据
    private string _username;
    private string _ip;
    private string _port;
    private string _protocol;
    private string _saveFolderPath;
    private bool _isAnimationEnabled;
    private bool _isAutoSaved;
    private readonly AvaSendApp _avaSendApp;

    // 数据
    public string Ip
    {
        get => _ip;
        set => this.RaiseAndSetIfChanged(ref _ip, value);
    }
    public string Port
    {
        get => _port;
        set => this.RaiseAndSetIfChanged(ref _port, value);
    }
    public string Protocol
    {
        get => _protocol;
        set => this.RaiseAndSetIfChanged(ref _protocol, value);
    }
    public string SaveFolderPath
    {
        get => _saveFolderPath;
        set => this.RaiseAndSetIfChanged(ref _saveFolderPath, value);
    }
    public string UserName
    {
        get => _username;
        set => this.RaiseAndSetIfChanged(ref _username, value);
    }
    public bool IsAnimationEnabled
    {
        get => _isAnimationEnabled;
        set => this.RaiseAndSetIfChanged(ref _isAnimationEnabled, value);
    }
    public bool IsAutoSaved
    {
        get => _isAutoSaved;
        set => this.RaiseAndSetIfChanged(ref _isAutoSaved, value);
    }

    // 解析路径
    private string ResolvePath(string path)
    {
        if (path.StartsWith("~"))
        {
            string homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string relativePath = path.TrimStart('~', '/', '\\');
            return Path.Combine(homePath, relativePath);
        }
        return path;
    }

    // 生成随机字符串
    private string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(
            Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray()
        );
    }

    // 加载设置
    private void LoadSettings()
    {
        _avaSendApp.LoadConfigurations();
        Ip = _avaSendApp.GetConfiguration("Ip") ?? "127.0.0.1";
        Port = _avaSendApp.GetConfiguration("Port") ?? "8080";
        Protocol = _avaSendApp.GetConfiguration("Protocol") ?? "TCP";
        SaveFolderPath =
            _avaSendApp.GetConfiguration("SaveFolderPath")
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads",
                "AvaSend"
            );
        UserName = _avaSendApp.GetConfiguration("UserName") ?? GenerateRandomString(10);
        IsAnimationEnabled = bool.Parse(
            _avaSendApp.GetConfiguration("IsAnimationEnabled") ?? "false"
        );
        IsAutoSaved = bool.Parse(_avaSendApp.GetConfiguration("IsAutoSaved") ?? "false");
    }

    // 保存设置
    public void SaveSettings()
    {
        _avaSendApp.AddOrUpdateConfiguration("Ip", Ip);
        _avaSendApp.AddOrUpdateConfiguration("Port", Port);
        _avaSendApp.AddOrUpdateConfiguration("Protocol", Protocol);
        _avaSendApp.AddOrUpdateConfiguration("SaveFolderPath", SaveFolderPath);
        _avaSendApp.AddOrUpdateConfiguration("UserName", UserName);
        _avaSendApp.AddOrUpdateConfiguration("IsAnimationEnabled", IsAnimationEnabled.ToString());
        _avaSendApp.AddOrUpdateConfiguration("IsAutoSaved", IsAutoSaved.ToString());
        _avaSendApp.SaveConfigurations();
    }
}
