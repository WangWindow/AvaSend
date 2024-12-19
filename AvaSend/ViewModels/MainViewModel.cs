using System;
using System.Reactive;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using ReactiveUI;

namespace AvaSend.ViewModels;

public class MainViewModel : ReactiveObject
{
    private ReactiveObject _currentViewModel;

    public ReactiveObject CurrentViewModel
    {
        get => _currentViewModel;
        set => this.RaiseAndSetIfChanged(ref _currentViewModel, value);
    }

    public ReactiveCommand<Unit, Unit> ShowSendViewCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowReceiveViewCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowSettingsViewCommand { get; }

    public MainViewModel()
    {
        ShowSendViewCommand = ReactiveCommand.Create(ShowSendView);
        ShowReceiveViewCommand = ReactiveCommand.Create(ShowReceiveView);
        ShowSettingsViewCommand = ReactiveCommand.Create(ShowSettingsView);

        // 默认显示发送视图
        CurrentViewModel = new ReceiveViewModel();
    }

    private void ShowSendView()
    {
        CurrentViewModel = new SendViewModel();
    }

    private void ShowReceiveView()
    {
        CurrentViewModel = new ReceiveViewModel();
    }

    private void ShowSettingsView()
    {
        CurrentViewModel = new SettingsViewModel();
    }
}
