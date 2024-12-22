using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using AvaSend.Models;
using ReactiveUI;

namespace AvaSend.ViewModels;

public class SendViewModel : ReactiveObject
{
    private readonly DataService _dataService;

    private Dictionary<string, string> _deviceList
    {
        get => _dataService.DeviceList ?? new Dictionary<string, string>();
        set
        {
            _dataService.DeviceList = value;
            this.RaisePropertyChanged(nameof(DeviceList));
            _dataService.SaveSettings();
        }
    }

    public ObservableCollection<string> DeviceList
    {
        get =>
            new ObservableCollection<string>(
                _deviceList != null ? _deviceList.Values : new List<string>()
            );
        set => _deviceList = value.ToDictionary(x => x, x => x);
    }

    private string _newDevice;
    public string NewDevice
    {
        get => _newDevice;
        set => this.RaiseAndSetIfChanged(ref _newDevice, value);
    }

    public ReactiveCommand<Unit, Unit> AddDeviceCommand { get; }
    public ReactiveCommand<Unit, Unit> RefreshDeviceListCommand { get; }

    public SendViewModel()
    {
        _dataService = DataService.Instance;
        _deviceList = _dataService.DeviceList ?? new Dictionary<string, string>();

        _dataService.LoadDevices();

        AddDeviceCommand = ReactiveCommand.Create(AddDevice);
        RefreshDeviceListCommand = ReactiveCommand.Create(RefreshDeviceList);
    }

    private void AddDevice()
    {
        if (!string.IsNullOrWhiteSpace(NewDevice))
        {
            if (_deviceList == null)
            {
                _deviceList = new Dictionary<string, string>();
            }

            _deviceList[NewDevice] = NewDevice;
            this.RaisePropertyChanged(nameof(DeviceList));
            _dataService.SaveDevices();
            NewDevice = string.Empty;
        }
    }

    private void RefreshDeviceList()
    {
        _dataService.LoadDevices();
    }
}
