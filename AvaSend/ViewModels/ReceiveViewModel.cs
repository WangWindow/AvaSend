using ReactiveUI;
using AvaSend.Models;

namespace AvaSend.ViewModels;

public class ReceiveViewModel : ViewModelBase
{
    private readonly DataService _dataService;

    public ReceiveViewModel()
    {
        _dataService = DataService.Instance;
    }

    public string Username
    {
        get => _dataService.Username;
        set => _dataService.Username = value;
    }

    public bool IsAnimationEnabled
    {
        get => _dataService.IsAnimationEnabled;
        set => _dataService.IsAnimationEnabled = value;
    }

    public bool IsAutoSaved
    {
        get => _dataService.IsAutoSaved;
        set => _dataService.IsAutoSaved = value;
    }
}
