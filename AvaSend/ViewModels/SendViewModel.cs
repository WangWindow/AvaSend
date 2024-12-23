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

    // 发送类型选择
    private bool _isFileSelected;

    public bool IsFileSelected
    {
        get => _isFileSelected;
        set => this.RaiseAndSetIfChanged(ref _isFileSelected, value);
    }

    private bool _isFolderSelected;

    public bool IsFolderSelected
    {
        get => _isFolderSelected;
        set => this.RaiseAndSetIfChanged(ref _isFolderSelected, value);
    }

    private bool _isTextSelected;

    public bool IsTextSelected
    {
        get => _isTextSelected;
        set => this.RaiseAndSetIfChanged(ref _isTextSelected, value);
    }

    private string _inputData;

    public string InputData
    {
        get => _inputData;
        set => this.RaiseAndSetIfChanged(ref _inputData, value);
    }

    public bool IsInputReadOnly => !IsTextSelected;

    private string _targetDevice;

    public string TargetDevice
    {
        get => _targetDevice;
        set => this.RaiseAndSetIfChanged(ref _targetDevice, value);
    }

    // 传输进度
    private double _progress;

    public double Progress
    {
        get => _progress;
        set => this.RaiseAndSetIfChanged(ref _progress, value);
    }

    // 状态消息
    private string _statusMessage;

    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    // 是否可以发送
    public bool CanSend => !string.IsNullOrEmpty(TargetDevice) && !string.IsNullOrEmpty(InputData);

    // 命令
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
                this.RaisePropertyChanged(nameof(CanSend));
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
                this.RaisePropertyChanged(nameof(CanSend));
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
        this.RaisePropertyChanged(nameof(CanSend));
    }

    private void ConnectDevice()
    {
        // 分割 TargetDevice 字符串
        var parts = TargetDevice.Split(':');
        if (parts.Length == 2)
        {
            var TargetDeviceIP = parts[0];
            var TargetDevicePort = parts[1];

            // 初始化客户端
            if (_dataService.Protocol == "TCP")
            {
                _tcpClient = new TCPClient
                {
                    Ip = TargetDeviceIP,
                    Port = int.Parse(TargetDevicePort),
                };
                _tcpClient.StartClientAsync();
            }
            else if (_dataService.Protocol == "UDP")
            {
                _udpClient = new UDPClient
                {
                    Ip = TargetDeviceIP,
                    Port = int.Parse(TargetDevicePort),
                };
                _udpClient.StartClientAsync();
            }
        }
        this.RaisePropertyChanged(nameof(CanSend));
    }

    private async Task SendAsync()
    {
        Progress = 0;
        StatusMessage = string.Empty;

        if (_dataService.Protocol == "TCP")
        {
            // 使用 TCP 传输
            // _tcpClient.Ip = TargetDevice;
            // if (IsFileSelected)
            // {
            //     _tcpClient.SendFileData(InputData);
            // }
            // else if (IsFolderSelected)
            // {
            //     _tcpClient.SendFolderData(InputData);
            // }
            // else if (IsTextSelected)
            // {
            //     _tcpClient.SendTextData(InputData);
            // }
            _tcpClient.Ip = TargetDevice;
            if (IsFileSelected)
            {
                _tcpClient.SendFileDataNonBlockingAsync(InputData);
            }
            else if (IsFolderSelected)
            {
                _tcpClient.SendFolderDataNonBlockingAsync(InputData);
            }
            else if (IsTextSelected)
            {
                _tcpClient.SendTextDataNonBlockingAsync(InputData);
            }
        }
        else if (_dataService.Protocol == "UDP")
        {
            // 使用 UDP 传输
            _udpClient.Ip = TargetDevice;
            if (IsFileSelected)
            {
                await _udpClient.SendFileDataAsync(InputData);
            }
            else if (IsFolderSelected)
            {
                await _udpClient.SendFolderDataAsync(InputData);
            }
            else if (IsTextSelected)
            {
                await _udpClient.SendTextDataAsync(InputData);
            }
        }

        // // 模拟传输进度（实际应用中应根据传输情况更新进度）
        // for (int i = 0; i <= 100; i++)
        // {
        //     Progress = i;
        //     await Task.Delay(5); // 延时以模拟进度
        // }

        // // 传输完成提示
        // StatusMessage = "Done";
    }
}
