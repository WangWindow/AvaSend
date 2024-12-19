using System;
using ReactiveUI;

namespace AvaSend.Models;

public class DataService : ReactiveObject
{
    private static readonly Lazy<DataService> _instance = new(() => new DataService());

    public static DataService Instance => _instance.Value;

    private string _username;
    private bool _isAnimationEnabled;
    private bool _isAutoSaved;

    private DataService()
    { }

    public string Username
    {
        get => _username;
        set => this.RaiseAndSetIfChanged(ref _username, value);
    }

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
