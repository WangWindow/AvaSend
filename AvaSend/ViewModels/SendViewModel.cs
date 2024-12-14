using System.Collections.ObjectModel;
using ReactiveUI;

namespace AvaSend.ViewModels;

public class SendViewModel : ReactiveObject
{
    private ObservableCollection<string> _deviceList;

    public ObservableCollection<string> DeviceList
    {
        get => _deviceList;
        set => this.RaiseAndSetIfChanged(ref _deviceList, value);
    }

    public SendViewModel()
    {
        // 初始化设备列表
        DeviceList = new ObservableCollection<string>
        {
            "设备1: 192.168.1.1:8080 (用户名: User1)",
        };
    }
}
