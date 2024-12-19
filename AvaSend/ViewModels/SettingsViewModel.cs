using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AvaSend.Models;
using ReactiveUI;

namespace AvaSend.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly DataService _dataService;
    private readonly DispatcherTimer _ipUpdateTimer;

    public string Ip
    {
        get => _dataService.Ip;
        set => _dataService.Ip = value;
    }

    public string Port
    {
        get => _dataService.Port;
        set => _dataService.Port = value;
    }

    public string Protocol
    {
        get => _dataService.Protocol;
        set => _dataService.Protocol = value;
    }

    public string SaveFolderPath
    {
        get => _dataService.SaveFolderPath;
        set => _dataService.SaveFolderPath = value;
    }

    public string UserName
    {
        get => _dataService.UserName;
        set => _dataService.UserName = value;
    }

    public ICommand SaveCommand { get; }

    public SettingsViewModel()
    {
        _dataService = DataService.Instance;
        SaveCommand = ReactiveCommand.Create(SaveSettings);

        // 初始化并启动 IP 更新定时器
        _ipUpdateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5), // 每10秒检测一次
        };
        _ipUpdateTimer.Tick += async (sender, e) => await UpdateIpAsync();
        _ipUpdateTimer.Start();

        // 初始获取本机 IP
        Task.Run(UpdateIpAsync);
    }

    private void SaveSettings()
    {
        _dataService.SaveSettings();
    }

    private async Task UpdateIpAsync()
    {
        string currentIp = GetLocalIpAddress();
        if (currentIp != Ip)
        {
            Ip = currentIp;
            _dataService.Ip = currentIp;
            _dataService.SaveSettings();
        }
        await Task.CompletedTask;
    }

    private string GetLocalIpAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var ip = host.AddressList.FirstOrDefault(a =>
                a.AddressFamily == AddressFamily.InterNetwork
            );
            return ip?.ToString() ?? "127.0.0.1";
        }
        catch
        {
            return "127.0.0.1";
        }
    }
}
