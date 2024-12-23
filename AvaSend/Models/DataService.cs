using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ReactiveUI;

namespace AvaSend.Models
{
    /// <summary>
    /// 数据服务
    /// </summary>
    public class DataService : ReactiveObject
    {
        private DataService()
        {
            _avaSendApp = new AvaSendApp();
            SaveFolderPath = ResolvePath("~/Downloads/AvaSend");
            LoadSettings();
            LoadDevices();
        }

        // 单例模式
        public static DataService Instance => _instance.Value;
        private static readonly Lazy<DataService> _instance = new(() => new DataService());

        private readonly AvaSendApp _avaSendApp;

        // 数据属性
        private string _username;
        public string UserName
        {
            get => _username;
            set => this.RaiseAndSetIfChanged(ref _username, value);
        }

        // IsServerEnabled
        private bool _isServerEnabled = true; // 默认值为 true
        public bool IsServerEnabled
        {
            get => _isServerEnabled;
            set => this.RaiseAndSetIfChanged(ref _isServerEnabled, value);
        }

        private Dictionary<string, string> _deviceList;

        public Dictionary<string, string> DeviceList
        {
            get => _deviceList;
            set => this.RaiseAndSetIfChanged(ref _deviceList, value);
        }

        private string _ip;
        public string Ip
        {
            get => _ip;
            set => this.RaiseAndSetIfChanged(ref _ip, value);
        }

        private string _port;
        public string Port
        {
            get => _port;
            set => this.RaiseAndSetIfChanged(ref _port, value);
        }

        private string _protocol;
        public string Protocol
        {
            get => _protocol;
            set => this.RaiseAndSetIfChanged(ref _protocol, ValidateProtocol(value));
        }

        private string _saveFolderPath;
        public string SaveFolderPath
        {
            get => _saveFolderPath;
            set => this.RaiseAndSetIfChanged(ref _saveFolderPath, value);
        }

        private bool _isAnimationEnabled;
        public bool IsAnimationEnabled
        {
            get => _isAnimationEnabled;
            set => this.RaiseAndSetIfChanged(ref _isAnimationEnabled, value);
        }

        private bool _isAutoSaved;
        public bool IsAutoSaved
        {
            get => _isAutoSaved;
            set => this.RaiseAndSetIfChanged(ref _isAutoSaved, value);
        }

        // 加载设置
        private void LoadSettings()
        {
            bool loadSuccess = _avaSendApp.LoadConfigurations();
            if (!loadSuccess)
            {
                Debug.WriteLine("配置加载失败，使用默认值。");
            }

            // 加载 Protocol 并验证
            var configuredProtocol = _avaSendApp.GetConfiguration("Protocol");
            Protocol = ValidateProtocol(configuredProtocol);

            // 加载 其他设置
            Ip = _avaSendApp.GetConfiguration("Ip") ?? "127.0.0.1";
            Port = _avaSendApp.GetConfiguration("Port") ?? "8080";
            SaveFolderPath =
                _avaSendApp.GetConfiguration("SaveFolderPath")
                ?? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads",
                    "AvaSend"
                );
            UserName = _avaSendApp.GetConfiguration("UserName") ?? GenerateRandomString(10);
            IsAnimationEnabled =
                bool.TryParse(
                    _avaSendApp.GetConfiguration("IsAnimationEnabled"),
                    out bool isAnimEnabled
                ) && isAnimEnabled;
            IsAutoSaved =
                bool.TryParse(_avaSendApp.GetConfiguration("IsAutoSaved"), out bool isAutoSaved)
                && isAutoSaved;

            // 加载 本机服务器是否启动
            IsServerEnabled =
                bool.TryParse(
                    _avaSendApp.GetConfiguration("IsServerEnabled"),
                    out bool isServerEnabled
                ) && isServerEnabled;
        }

        // 保存设置
        public bool SaveSettings()
        {
            try
            {
                _avaSendApp.AddOrUpdateConfiguration("Ip", Ip);
                _avaSendApp.AddOrUpdateConfiguration("Port", Port);
                _avaSendApp.AddOrUpdateConfiguration("Protocol", Protocol);
                _avaSendApp.AddOrUpdateConfiguration("SaveFolderPath", SaveFolderPath);
                _avaSendApp.AddOrUpdateConfiguration("UserName", UserName);
                _avaSendApp.AddOrUpdateConfiguration(
                    "IsAnimationEnabled",
                    IsAnimationEnabled.ToString()
                );
                _avaSendApp.AddOrUpdateConfiguration("IsAutoSaved", IsAutoSaved.ToString());

                // 保存 IsServerEnabled
                _avaSendApp.AddOrUpdateConfiguration("IsServerEnabled", IsServerEnabled.ToString());

                return _avaSendApp.SaveConfigurations();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"保存设置时发生错误: {ex.Message}");
                return false;
            }
        }

        // 加载设备列表
        public void LoadDevices()
        {
            bool loadSuccess = _avaSendApp.LoadDevices();
            if (!loadSuccess)
            {
                Debug.WriteLine("配置设备列表失败，使用默认值。");
            }
            DeviceList = _avaSendApp.DeviceList;
        }

        // 保存设备列表
        public bool SaveDevices()
        {
            try
            {
                foreach (var device in DeviceList)
                {
                    if (string.IsNullOrWhiteSpace(device.Key))
                    {
                        _avaSendApp.DeviceList.Remove(device.Key);
                    }
                    _avaSendApp.AddOrUpdateDevice(device.Key, device.Value);
                }
                return _avaSendApp.SaveDevices();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"保存设备列表时发生错误: {ex.Message}");
                return false;
            }
        }

        // 添加或更新设备
        public void AddOrUpdateDevice(string key, string value)
        {
            _avaSendApp.AddOrUpdateDevice(key, value);
        }

        // 验证 Protocol 的值
        private string ValidateProtocol(string? protocol)
        {
            return protocol switch
            {
                "TCP" => "TCP",
                "UDP" => "UDP",
                _ => "TCP", // 默认值
            };
        }

        // 生成随机字符串
        private string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(
                Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray()
            );
        }

        // 解析路径
        private string ResolvePath(string path)
        {
            if (path.StartsWith("~"))
            {
                path = Environment.ExpandEnvironmentVariables(path.Replace("~", string.Empty));
            }
            return Path.GetFullPath(path);
        }
    }
}
