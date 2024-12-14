using System;
using System.ComponentModel;
using System.Windows.Input;
using AvaSend.Models;
using ReactiveUI;

namespace AvaSend.ViewModels;

public class SettingsViewModel : ReactiveObject
{
    private string ip = string.Empty; // 初始化 ip 字段

    public string Ip
    {
        get => ip;
        set
        {
            if (ip != value)
            {
                ip = value;
                OnPropertyChanged(nameof(Ip));
            }
        }
    }

    private int port;

    public int Port
    {
        get => port;
        set
        {
            if (port != value)
            {
                port = value;
                OnPropertyChanged(nameof(Port));
            }
        }
    }

    public ICommand SaveCommand { get; }

    private TCPClient client;
    private TCPServer server;

    public SettingsViewModel()
    {
        // 初始化 TCPClient 和 TCPServer 实例
        client = new TCPClient();
        server = new TCPServer();

        // 设置初始值
        Ip = client.Ip;
        Port = client.Port;

        // 初始化命令
        SaveCommand = new DelegateCommand(SaveSettings);
    }

    private void SaveSettings()
    {
        client.Ip = Ip;
        client.Port = Port;
        server.Ip = Ip;
        server.Port = Port;

        // 重新连接客户端和服务器
        client.Reconnect(client.Ip, client.Port);
        server.RestartServer(server.Ip, server.Port);
    }

    public event PropertyChangedEventHandler? PropertyChanged; // 将事件声明为可为 null

    protected void OnPropertyChanged(string name)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

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
