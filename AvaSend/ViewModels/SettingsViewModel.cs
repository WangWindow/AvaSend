using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ReactiveUI;

namespace AvaSend.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private string _ip;
        private string _port;
        private string _protocol;
        private string _defaultSavePath;

        public string Ip
        {
            get => _ip;
            set => this.RaiseAndSetIfChanged(ref _ip, value);
        }

        public string Port
        {
            get => _port;
            set => this.RaiseAndSetIfChanged(ref _port, value);
        }

        public string Protocol
        {
            get => _protocol;
            set => this.RaiseAndSetIfChanged(ref _protocol, value);
        }

        public string DefaultSavePath
        {
            get => _defaultSavePath;
            set => this.RaiseAndSetIfChanged(ref _defaultSavePath, value);
        }

        public ICommand SaveCommand { get; }

        public SettingsViewModel()
        {
            SaveCommand = ReactiveCommand.Create(SaveSettings);
        }

        private void SaveSettings()
        {
            // 保存设置的逻辑
        }
    }
}
