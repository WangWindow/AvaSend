using ReactiveUI;

namespace AvaSend.ViewModels;

public class ReceiveViewModel : ViewModelBase
{
    private bool _isAnimationEnabled;
    private bool _isAutoSaved;

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
}
