using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using AvaSend.Models;
using ReactiveUI;

namespace AvaSend.ViewModels;

public class SendViewModel : ReactiveObject
{
    private readonly DataService _dataService;
    private UDPClient _udpClient;
    private TCPClient _tcpClient;

    public SendViewModel()
    {
        _dataService = DataService.Instance;

        // 初始化命令
        SelectFileCommand = ReactiveCommand.CreateFromTask(SelectFileAsync);
        SelectFolderCommand = ReactiveCommand.CreateFromTask(SelectFolderAsync);
        SelectTextCommand = ReactiveCommand.Create(SelectText);
        SendCommand = ReactiveCommand.CreateFromTask(SendAsync);
        ConnectDeviceCommand = ReactiveCommand.Create(ConnectDevice);
    }

    // 是否 TCP
    private bool IsTcp => _dataService.Protocol == "TCP";

    // 是否已连接(仅 TCP 用)
    private bool _isConnected;
    public bool IsConnected
    {
        get => _isConnected;
        set => this.RaiseAndSetIfChanged(ref _isConnected, value);
    }

    // 发送类型选择
    private bool _isFileSelected;
    public bool IsFileSelected
    {
        get => _isFileSelected;
        set
        {
            this.RaiseAndSetIfChanged(ref _isFileSelected, value);
            this.RaisePropertyChanged(nameof(CanSend));
        }
    }

    private bool _isFolderSelected;
    public bool IsFolderSelected
    {
        get => _isFolderSelected;
        set
        {
            this.RaiseAndSetIfChanged(ref _isFolderSelected, value);
            this.RaisePropertyChanged(nameof(CanSend));
        }
    }

    private bool _isTextSelected;
    public bool IsTextSelected
    {
        get => _isTextSelected;
        set
        {
            this.RaiseAndSetIfChanged(ref _isTextSelected, value);
            this.RaisePropertyChanged(nameof(CanSend));
        }
    }

    private string _inputData;
    public string InputData
    {
        get => _inputData;
        set
        {
            this.RaiseAndSetIfChanged(ref _inputData, value);
            this.RaisePropertyChanged(nameof(CanSend));
        }
    }

    public bool IsInputReadOnly => !IsTextSelected;

    private string _targetDevice;
    public string TargetDevice
    {
        get => _targetDevice;
        set
        {
            this.RaiseAndSetIfChanged(ref _targetDevice, value);
            this.RaisePropertyChanged(nameof(CanSend));
        }
    }

    private double _progress;
    public double Progress
    {
        get => _progress;
        set => this.RaiseAndSetIfChanged(ref _progress, value);
    }

    private string _statusMessage;
    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    // 根据协议区分是否可发送: TCP 需已连接且已选发送类型；UDP 只需已选发送类型
    public bool CanSend
    {
        get
        {
            bool anyTypeSelected = IsFileSelected || IsFolderSelected || IsTextSelected;
            if (IsTcp)
                return IsConnected && anyTypeSelected;
            else
                return anyTypeSelected;
        }
    }

    // UDP 模式下连接按钮禁用 (只有 TCP 时启用)
    public bool IsConnectButtonEnabled => IsTcp;

    public ReactiveCommand<Unit, Unit> SelectFileCommand { get; }
    public ReactiveCommand<Unit, Unit> SelectFolderCommand { get; }
    public ReactiveCommand<Unit, Unit> SelectTextCommand { get; }
    public ReactiveCommand<Unit, Unit> SendCommand { get; }
    public ReactiveCommand<Unit, Unit> ConnectDeviceCommand { get; }

    private async Task SelectFileAsync()
    {
        var dialog = new OpenFileDialog();
        if (
            AvaSend.App.Current?.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime lifetime
        )
        {
            Window mainWindow = lifetime.MainWindow;
            var result = await dialog.ShowAsync(mainWindow);
            if (result != null && result.Length > 0)
            {
                InputData = result[0];
                IsFileSelected = true;
                IsFolderSelected = false;
                IsTextSelected = false;
                this.RaisePropertyChanged(nameof(IsInputReadOnly));
            }
        }
    }

    private async Task SelectFolderAsync()
    {
        var dialog = new OpenFolderDialog();
        if (
            AvaSend.App.Current?.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime lifetime
        )
        {
            Window mainWindow = lifetime.MainWindow;
            var result = await dialog.ShowAsync(mainWindow);
            if (result != null)
            {
                InputData = result;
                IsFileSelected = false;
                IsFolderSelected = true;
                IsTextSelected = false;
                this.RaisePropertyChanged(nameof(IsInputReadOnly));
            }
        }
    }

    private void SelectText()
    {
        InputData = string.Empty;
        IsFileSelected = false;
        IsFolderSelected = false;
        IsTextSelected = true;
        this.RaisePropertyChanged(nameof(IsInputReadOnly));
    }

    private async void ConnectDevice()
    {
        IsConnected = false;
        var parts = TargetDevice?.Split(':');
        if (parts != null && parts.Length == 2)
        {
            var TargetDeviceIP = parts[0];
            var TargetDevicePort = parts[1];

            if (IsTcp)
            {
                _tcpClient = new TCPClient
                {
                    Ip = TargetDeviceIP,
                    Port = int.Parse(TargetDevicePort),
                };
                try
                {
                    await _tcpClient.StartClientAsync();
                    IsConnected = true;
                }
                catch
                {
                    IsConnected = false;
                }
            }
            else
            {
                return;
            }
        }
        this.RaisePropertyChanged(nameof(CanSend));
    }

    private async Task SendAsync()
    {
        Progress = 0;
        StatusMessage = string.Empty;

        if (IsTcp)
        {
            // 使用 TCP 传输
            if (_tcpClient == null)
            {
                _tcpClient = new TCPClient();
                var parts = TargetDevice?.Split(':');
                if (parts != null && parts.Length == 2)
                {
                    _tcpClient.Ip = parts[0];
                    _tcpClient.Port = int.Parse(parts[1]);
                }
                try
                {
                    await _tcpClient.StartClientAsync();
                    IsConnected = true;
                }
                catch
                {
                    IsConnected = false;
                }
            }

            if (IsFileSelected)
            {
                await _tcpClient.SendFileAsync(InputData);
            }
            else if (IsFolderSelected)
            {
                await _tcpClient.SendFolderAsync(InputData);
            }
            else if (IsTextSelected)
            {
                await _tcpClient.SendTextAsync(InputData);
            }
        }
        else
        {
            // 使用 UDP 传输
            if (_udpClient == null)
            {
                var parts = TargetDevice?.Split(':');
                if (parts != null && parts.Length == 2)
                {
                    _udpClient = new UDPClient { Ip = parts[0], Port = int.Parse(parts[1]) };
                    _udpClient.Start();
                }
            }

            if (IsFileSelected)
            {
                await _udpClient.SendFileAsync(InputData);
            }
            else if (IsFolderSelected)
            {
                await _udpClient.SendFolderAsync(InputData);
            }
            else if (IsTextSelected)
            {
                await _udpClient.SendTextAsync(InputData);
            }
        }
    }
}
