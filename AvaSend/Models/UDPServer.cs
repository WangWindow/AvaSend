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
                        await CreateTransferWindowAsync(folderName);
                        break;

                    case 'P':
                        _currentRelativePath = Encoding.UTF8.GetString(data);
                        break;

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

        private async Task CreateTransferWindowAsync(string folderName)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _fileListPanel = new StackPanel { Spacing = 5, Margin = new Thickness(10) };

                var scrollViewer = new ScrollViewer
                {
                    Content = _fileListPanel,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                };

                _transferWindow?.Close();
                _transferWindow = new Window
                {
                    Title = $"接收文件夹 - {folderName}",
                    Width = 600,
                    Height = 400,
                    Content = new DockPanel
                    {
                        Children =
                        {
                            new TextBlock
                            {
                                Text = $"保存位置: {_currentFolder}",
                                [DockPanel.DockProperty] = Dock.Top,
                                Margin = new Thickness(10),
                            },
                            scrollViewer,
                        },
                    },
                };

                _transferWindow.Show();
            });
        }

        private void UpdateProgress(string filePath, int bytesReceived)
        {
            _fileProgress[filePath] = _fileProgress[filePath] + bytesReceived;
            var fileInfo = new FileInfo(filePath);
            double progress = (_fileProgress[filePath] * 100.0) / fileInfo.Length;

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                var transfer = _transfers.FirstOrDefault(x => x.FullPath == filePath);
                if (transfer?.Progress != null)
                {
                    transfer.Progress.Value = Math.Min(progress, 100);
                }
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
                        var fileButton = new Button
                        {
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            Padding = new Thickness(10),
                            Margin = new Thickness(0, 0, 0, 5),
                            Background = transfer.IsCompleted ? Brushes.LightGreen : Brushes.White,
                            Content = new StackPanel
                            {
                                Children =
                                {
                                    new TextBlock
                                    {
                                        Text = $"文件: {transfer.FileName}",
                                        FontWeight = FontWeight.Bold,
                                    },
                                    new TextBlock
                                    {
                                        Text = $"保存位置: {transfer.FullPath}",
                                        Foreground = Brushes.Gray,
                                    },
                                    transfer.Progress,
                                },
                            },
                        };

                        if (transfer.IsCompleted)
                        {
                            fileButton.Click += (s, e) =>
                            {
                                Process.Start(
                                    new ProcessStartInfo
                                    {
                                        FileName = "explorer.exe",
                                        Arguments = $"/select,\"{transfer.FullPath}\"",
                                    }
                                );
                            };
                        }

                        _fileListPanel.Children.Add(fileButton);
                    }
                }
            });
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
