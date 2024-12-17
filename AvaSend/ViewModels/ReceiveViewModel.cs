using ReactiveUI;
using System.ComponentModel;

namespace AvaSend.ViewModels;

public class ReceiveViewModel : ReactiveObject
{
    private bool _isAnimationEnabled;

    public bool IsAnimationEnabled
    {
        get => _isAnimationEnabled;
        set => this.RaiseAndSetIfChanged(ref _isAnimationEnabled, value);
    }
}
