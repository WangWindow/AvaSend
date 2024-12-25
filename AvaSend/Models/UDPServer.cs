using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace AvaSend.Models
{
    public class TransferItem
    {
        public string FileName { get; set; }
        public string RelativePath { get; set; }
        public string FullPath { get; set; }
        public ProgressBar Progress { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class UDPServer
    {
        public string Ip { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 8081;
        public string SaveFolderPath { get; set; } =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads",
                "AvaSend"
            );

        private UdpClient _listener;
        private CancellationTokenSource _cts;
        private FileStream _currentFile;
        private Window _transferWindow;
        private string _currentFolder;
        private string _currentRelativePath;
        private List<TransferItem> _transfers = new();
        private StackPanel _fileListPanel;
        private Dictionary<string, long> _fileProgress = new();

        public async Task StartServerAsync()
        {
            try
            {
                Directory.CreateDirectory(SaveFolderPath);
                _cts = new CancellationTokenSource();
                _listener = new UdpClient(Port);

                while (!_cts.Token.IsCancellationRequested)
                {
                    var result = await _listener.ReceiveAsync();
                    if (result.Buffer.Length > 0)
                    {
                        await HandlePacketAsync(result.Buffer);
                    }
                }
            }
            catch (Exception ex) when (!_cts.Token.IsCancellationRequested)
            {
                throw new Exception($"服务器错误: {ex.Message}");
            }
        }

        public void StopServer()
        {
            _cts?.Cancel();
            _currentFile?.Dispose();
            _listener?.Close();
            _transferWindow?.Close();
        }

        private async Task HandlePacketAsync(byte[] packet)
        {
            try
            {
                char type = (char)packet[0];
                byte[] data = new byte[packet.Length - 1];
                Buffer.BlockCopy(packet, 1, data, 0, data.Length);

                switch (type)
                {
                    case 'T':
                        string text = Encoding.UTF8.GetString(data);
                        ShowTextWindow("收到文本", text);
                        break;

                    case 'D':
                        string folderName = Encoding.UTF8.GetString(data);
                        _currentFolder = Path.Combine(SaveFolderPath, folderName);
                        Directory.CreateDirectory(_currentFolder);
                        _transfers.Clear();
                        _fileProgress.Clear();
                        await CreateTransferWindowAsync(folderName);
                        break;

                    case 'P':
                        _currentRelativePath = Encoding.UTF8.GetString(data);
                        break;

                    // 在 HandlePacketAsync 方法中修改 'F' case:
                    case 'F':
                        string fileName = Encoding.UTF8.GetString(data);
                        string filePath;

                        if (!string.IsNullOrEmpty(_currentRelativePath))
                        {
                            string fullPath = Path.Combine(
                                _currentFolder ?? SaveFolderPath,
                                _currentRelativePath
                            );
                            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                            filePath = GetUniquePath(fullPath);
                        }
                        else
                        {
                            filePath = GetUniquePath(
                                Path.Combine(_currentFolder ?? SaveFolderPath, fileName)
                            );
                        }

                        _currentFile = File.Create(filePath);
                        _fileProgress[filePath] = 0;

                        var transfer = new TransferItem
                        {
                            FileName = fileName,
                            RelativePath = _currentRelativePath ?? ".",
                            FullPath = filePath,
                            Progress = new ProgressBar
                            {
                                Minimum = 0,
                                Maximum = 100,
                                Height = 3,
                                Margin = new Thickness(0, 5, 0, 0),
                            },
                        };

                        _transfers.Add(transfer);
                        await CreateSingleFileTransferWindowAsync(fileName, filePath);
                        await UpdateFileListAsync();
                        break;

                    case 'C':
                        if (_currentFile != null)
                        {
                            await _currentFile.WriteAsync(data);
                            UpdateProgress(_currentFile.Name, data.Length);
                        }
                        break;

                    case 'E':
                        if (_currentFile != null)
                        {
                            await _currentFile.FlushAsync();
                            var existingTransfer = _transfers.FirstOrDefault(t =>
                                t.FullPath == _currentFile.Name
                            );
                            if (existingTransfer != null)
                            {
                                existingTransfer.IsCompleted = true;
                                await UpdateFileListAsync();
                            }
                            _currentFile.Dispose();
                            _currentFile = null;
                            _currentRelativePath = null;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"处理数据包错误: {ex.Message}");
            }
        }

        private async Task CreateSingleFileTransferWindowAsync(string fileName, string savePath)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _fileListPanel = new StackPanel { Spacing = 10, Margin = new Thickness(10) };

                var scrollViewer = new ScrollViewer
                {
                    Content = _fileListPanel,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    Margin = new Thickness(0, 10, 0, 0),
                    MinHeight = 150,
                };

                var headerPanel = new StackPanel { Margin = new Thickness(10), Spacing = 5 };

                // 添加文件名标题
                headerPanel.Children.Add(
                    new TextBlock
                    {
                        Text = $"接收文件: {fileName}",
                        FontWeight = FontWeight.Bold,
                        FontSize = 16,
                    }
                );

                // 添加保存路径
                headerPanel.Children.Add(
                    new TextBlock { Text = $"保存位置: {savePath}", Foreground = Brushes.Gray }
                );

                // 添加取消按钮
                var cancelButton = new Button
                {
                    Content = "取消传输",
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 10, 0, 0),
                };

                cancelButton.Click += (s, e) =>
                {
                    _currentFile?.Dispose();
                    _currentFile = null;
                    _transferWindow?.Close();
                };

                headerPanel.Children.Add(cancelButton);

                _transferWindow?.Close();
                _transferWindow = new Window
                {
                    Title = "文件传输",
                    Width = 500,
                    Height = 300,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Content = new DockPanel
                    {
                        LastChildFill = true,
                        Children =
                        {
                            headerPanel,
                            new Border
                            {
                                Child = scrollViewer,
                                BorderBrush = Brushes.LightGray,
                                BorderThickness = new Thickness(1),
                                Margin = new Thickness(10),
                                [DockPanel.DockProperty] = Dock.Top,
                            },
                        },
                    },
                };

                _transferWindow.Show();
            });
        }

        private async Task CreateTransferWindowAsync(string folderName)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _fileListPanel = new StackPanel { Spacing = 10, Margin = new Thickness(10) };

                var scrollViewer = new ScrollViewer
                {
                    Content = _fileListPanel,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    Margin = new Thickness(0, 10, 0, 0),
                    MinHeight = 200,
                };

                var headerPanel = new StackPanel { Margin = new Thickness(10), Spacing = 5 };

                headerPanel.Children.Add(
                    new TextBlock
                    {
                        Text = $"传输文件夹: {folderName}",
                        FontWeight = FontWeight.Bold,
                        FontSize = 16,
                    }
                );

                headerPanel.Children.Add(
                    new TextBlock
                    {
                        Text = $"保存位置: {_currentFolder}",
                        Foreground = Brushes.Gray,
                    }
                );

                _transferWindow?.Close();
                _transferWindow = new Window
                {
                    Title = "文件夹传输",
                    Width = 700,
                    Height = 500,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Content = new DockPanel
                    {
                        Children =
                        {
                            headerPanel,
                            new Border
                            {
                                Child = scrollViewer,
                                BorderBrush = Brushes.LightGray,
                                BorderThickness = new Thickness(1),
                                Margin = new Thickness(10),
                                [DockPanel.DockProperty] = Dock.Top,
                            },
                        },
                    },
                };

                _transferWindow.Show();
            });
        }

        private async Task UpdateFileListAsync()
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_fileListPanel != null)
                {
                    _fileListPanel.Children.Clear();
                    foreach (var transfer in _transfers)
                    {
                        var container = new Border
                        {
                            Background = transfer.IsCompleted
                                ? new SolidColorBrush(Color.FromRgb(240, 255, 240))
                                : Brushes.White,
                            BorderBrush = Brushes.LightGray,
                            BorderThickness = new Thickness(1),
                            CornerRadius = new CornerRadius(4),
                            Padding = new Thickness(10),
                            Margin = new Thickness(0, 0, 0, 5),
                            Child = new DockPanel(),
                        };

                        var infoPanel = new StackPanel { Spacing = 4 };

                        // 文件名和进度百分比
                        var headerPanel = new DockPanel();
                        headerPanel.Children.Add(
                            new TextBlock
                            {
                                Text = transfer.FileName,
                                FontWeight = FontWeight.Bold,
                                [DockPanel.DockProperty] = Dock.Left,
                            }
                        );

                        var progressText = new TextBlock
                        {
                            Text = $"{transfer.Progress.Value:F1}%",
                            Foreground = Brushes.Gray,
                            [DockPanel.DockProperty] = Dock.Right,
                        };
                        headerPanel.Children.Add(progressText);
                        infoPanel.Children.Add(headerPanel);

                        // 相对路径
                        infoPanel.Children.Add(
                            new TextBlock
                            {
                                Text = $"相对路径: {transfer.RelativePath}",
                                Foreground = Brushes.Gray,
                            }
                        );

                        // 完整路径
                        infoPanel.Children.Add(
                            new TextBlock
                            {
                                Text = $"保存位置: {transfer.FullPath}",
                                Foreground = Brushes.Gray,
                                TextWrapping = TextWrapping.Wrap,
                            }
                        );

                        // 进度条
                        var progressBar = new ProgressBar
                        {
                            Value = transfer.Progress.Value,
                            Maximum = 100,
                            Height = 4,
                            Margin = new Thickness(0, 5, 0, 0),
                        };
                        infoPanel.Children.Add(progressBar);

                        // 更新引用
                        transfer.Progress = progressBar;

                        ((DockPanel)container.Child).Children.Add(infoPanel);

                        // 添加打开按钮（如果传输完成）
                        if (transfer.IsCompleted)
                        {
                            var openButton = new Button
                            {
                                Content = "打开所在文件夹",
                                HorizontalAlignment = HorizontalAlignment.Right,
                                Margin = new Thickness(10, 5, 0, 0),
                                [DockPanel.DockProperty] = Dock.Right,
                            };

                            openButton.Click += (s, e) =>
                                Process.Start(
                                    new ProcessStartInfo
                                    {
                                        FileName = "explorer.exe",
                                        Arguments = $"/select,\"{transfer.FullPath}\"",
                                    }
                                );

                            ((DockPanel)container.Child).Children.Add(openButton);
                        }

                        _fileListPanel.Children.Add(container);
                    }
                }
            });
        }

        private void UpdateProgress(string filePath, int bytesReceived)
        {
            try
            {
                _fileProgress[filePath] = _fileProgress[filePath] + bytesReceived;
                var fileInfo = new FileInfo(filePath);
                double progress = (_fileProgress[filePath] * 100.0) / fileInfo.Length;

                Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    var transfer = _transfers.FirstOrDefault(x => x.FullPath == filePath);
                    if (transfer?.Progress != null)
                    {
                        transfer.Progress.Value = Math.Min(progress, 100);
                        await UpdateFileListAsync();
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"更新进度失败: {ex.Message}");
            }
        }

        private string GetUniquePath(string path)
        {
            string dir = Path.GetDirectoryName(path);
            string name = Path.GetFileNameWithoutExtension(path);
            string ext = Path.GetExtension(path);
            int count = 1;

            while (File.Exists(path))
            {
                path = Path.Combine(dir, $"{name}({count++}){ext}");
            }
            return path;
        }

        private void ShowTextWindow(string title, string content)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                new Window
                {
                    Title = title,
                    Width = 400,
                    Height = 300,
                    Content = new TextBox
                    {
                        Text = content,
                        IsReadOnly = true,
                        AcceptsReturn = true,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(10),
                    },
                }.Show();
            });
        }
    }
}
