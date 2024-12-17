using System;
using System.ComponentModel;
using System.Windows.Input;
using AvaSend.Models;
using ReactiveUI;

namespace AvaSend.ViewModels;

public class SettingsViewModel : ReactiveObject
{
    private string _ip;
    private int _port;
    private string _protocol;

    private TCPClient _client;
    private TCPServer _server;

    public ICommand SaveCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged; // 将事件声明为可为 null

    public string Ip
    {
        get => _ip;
        set
        {
            if (_ip != value)
            {
                _ip = value;
                OnPropertyChanged(nameof(Ip));
            }
        }
    }

    public int Port
    {
        get => _port;
        set
        {
            if (_port != value)
            {
                _port = value;
                OnPropertyChanged(nameof(Port));
            }
        }
    }

    public string Protocol
    {
        get => _protocol;
        set
        {
            if (_protocol != value)
            {
                _protocol = value;
                OnPropertyChanged(nameof(Protocol));
            }
        }
    }

    public SettingsViewModel()
    {
        // 初始化 TCPClient 和 TCPServer 实例
        _client = new TCPClient();
        _server = new TCPServer();

        // 设置初始值
        Ip = _client.Ip;
        Port = _client.Port;

        // 初始化命令
        SaveCommand = new DelegateCommand(SaveSettings);
    }

    private void SaveSettings()
    {
        _client.Ip = Ip;
        _client.Port = Port;
        _server.Ip = Ip;
        _server.Port = Port;

        // 重新连接客户端和服务器
        _client.Reconnect(_client.Ip, _client.Port);
        _server.RestartServer(_server.Ip, _server.Port);
    }

    protected void OnPropertyChanged(string name)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

/// <summary>
/// 委托命令
/// </summary>
public class DelegateCommand : ICommand
{
    private readonly Action execute;
    private readonly Func<bool>? canExecute;

    public DelegateCommand(Action executeAction, Func<bool>? canExecuteFunc = null)
    {
        execute = executeAction ?? throw new ArgumentNullException(nameof(executeAction));
        canExecute = canExecuteFunc;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => canExecute == null || canExecute();

    public void Execute(object? parameter) => execute();

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
