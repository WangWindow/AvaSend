using System;
using System.Diagnostics;
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
    public SettingsViewModel()
    {
        _dataService = DataService.Instance;
        SaveCommand = ReactiveCommand.Create(SaveSettings);

        // 初始化并启动 IP 更新定时器
        _ipUpdateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5), // 每 5 秒检测一次
        };
        _ipUpdateTimer.Tick += async (sender, e) => await UpdateIpAsync();
        _ipUpdateTimer.Start();

        // 初始获取本机 IP
        Task.Run(UpdateIpAsync);
    }

    private readonly DataService _dataService;
    private readonly DispatcherTimer _ipUpdateTimer;

    public string Ip
    {
        get => _dataService.Ip;
        set
        {
            _dataService.Ip = value;
            this.RaisePropertyChanged(nameof(Ip));
        }
    }

    public string Port
    {
        get => _dataService.Port;
        set
        {
            _dataService.Port = value;
            this.RaisePropertyChanged(nameof(Port));
        }
    }

    public string Protocol
    {
        get => _dataService.Protocol;
        set
        {
            _dataService.Protocol = value;
            this.RaisePropertyChanged(nameof(Protocol));
        }
    }

    public string SaveFolderPath
    {
        get => _dataService.SaveFolderPath;
        set
        {
            _dataService.SaveFolderPath = value;
            this.RaisePropertyChanged(nameof(SaveFolderPath));
        }
    }

    public string UserName
    {
        get => _dataService.UserName;
        set
        {
            _dataService.UserName = value;
            this.RaisePropertyChanged(nameof(UserName));
        }
    }

    public ICommand SaveCommand { get; }

    private void SaveSettings()
    {
        bool success = _dataService.SaveSettings();
        if (success)
        {
            // 可以添加保存成功的提示，例如显示通知
            Debug.WriteLine("设置已保存成功。");
        }
        else
        {
            // 处理保存失败的情况，例如显示错误消息
            Debug.WriteLine("保存设置时发生错误。");
        }
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
