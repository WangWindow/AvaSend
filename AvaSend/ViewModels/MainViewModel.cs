using ReactiveUI;
using System;
using System.Reactive;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace AvaSend.ViewModels;

public class MainViewModel : ReactiveObject
{
    private ReactiveObject _currentView;

    public ReactiveObject CurrentView
    {
        get => _currentView;
        set => this.RaiseAndSetIfChanged(ref _currentView, value);
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
        ShowSendView();
    }

    private void ShowSendView()
    {
        CurrentView = new SendViewModel();
    }

    private void ShowReceiveView()
    {
        CurrentView = new ReceiveViewModel();
    }

    private void ShowSettingsView()
    {
        CurrentView = new SettingsViewModel();
    }
}