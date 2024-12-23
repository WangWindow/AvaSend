using System.Reactive;
using AvaSend.Models;
using ReactiveUI;

namespace AvaSend.ViewModels
{
    public class ReceiveViewModel : ViewModelBase
    {
        private readonly DataService _dataService;
        private UDPServer _udpServer;
        private TCPServer _tcpServer;

        public ReceiveViewModel()
        {
            _dataService = DataService.Instance;
            SaveCommand = ReactiveCommand.Create(SaveSettings);
            ToggleServerCommand = ReactiveCommand.Create(ToggleServer);
        }

        public string UserName
        {
            get => _dataService.UserName;
            set
            {
                _dataService.UserName = value;
                SaveSettings();
            }
        }

        public bool IsAnimationEnabled
        {
            get => _dataService.IsAnimationEnabled;
            set
            {
                _dataService.IsAnimationEnabled = value;
                this.RaisePropertyChanged(nameof(IsAnimationEnabled));
                SaveSettings();
            }
        }

        public bool IsAutoSaved
        {
            get => _dataService.IsAutoSaved;
            set
            {
                _dataService.IsAutoSaved = value;
                this.RaisePropertyChanged(nameof(IsAutoSaved));
                SaveSettings();
            }
        }

        public bool IsServerEnabled
        {
            get => _dataService.IsServerEnabled;
            set
            {
                _dataService.IsServerEnabled = value;
                this.RaisePropertyChanged(nameof(IsServerEnabled));
                SaveSettings();
            }
        }

        public ReactiveCommand<Unit, Unit> SaveCommand { get; }
        public ReactiveCommand<Unit, Unit> ToggleServerCommand { get; }

        private void SaveSettings()
        {
            _dataService.SaveSettings();
        }

        private void ToggleServer()
        {
            var ServerIP = _dataService.Ip;
            var ServerPort = _dataService.Port;
            if (IsServerEnabled)
            {
                // 启动服务器
                if (_dataService.Protocol == "TCP")
                {
                    _tcpServer = new TCPServer { Ip = ServerIP, Port = int.Parse(ServerPort) };
                    _tcpServer.StartServerAsync();
                }
                else if (_dataService.Protocol == "UDP")
                {
                    _udpServer = new UDPServer { Ip = ServerIP, Port = int.Parse(ServerPort) };
                    _udpServer.StartServerAsync();
                }
            }
            else
            {
                // 停止服务器
                if (_dataService.Protocol == "TCP")
                {
                    if (_tcpServer != null)
                    {
                        _tcpServer.StopServer();
                    }
                }
                else if (_dataService.Protocol == "UDP")
                {
                    if (_udpServer != null)
                    {
                        _udpServer.StopServer();
                    }
                }
            }
        }
    }
}
