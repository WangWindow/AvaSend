using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AvaSend.Models;

/// <summary>
/// TCP客户端
/// </summary>
public class TCPClient
{
    // 设置接口
    public string Ip { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 8080;
    public string SaveFolderPath { get; set; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads",
            "AvaSend"
        );
    public int SearchTimeout { get; set; } = 3000; // 毫秒

    private Socket _sender;
    private CancellationTokenSource _cancellationTokenSource;
    private int _reconnectAttempts = 0;
    private const int MaxReconnectAttempts = 5;

    // 启动客户端
    public async Task StartClientAsync()
    {
        if (!Directory.Exists(SaveFolderPath))
        {
            Directory.CreateDirectory(SaveFolderPath);
        }

        _cancellationTokenSource = new CancellationTokenSource();
        await ConnectAsync(_cancellationTokenSource.Token);
    }

    // 停止客户端
    public void StopClient()
    {
        _cancellationTokenSource?.Cancel();
        Disconnect();
    }

    // 连接服务器
    private async Task ConnectAsync(CancellationToken cancellationToken)
    {
        while (_reconnectAttempts < MaxReconnectAttempts)
        {
            try
            {
                _sender = new Socket(
                    AddressFamily.InterNetwork,
                    SocketType.Stream,
                    ProtocolType.Tcp
                );
                var connectTask = _sender.ConnectAsync(new IPEndPoint(IPAddress.Parse(Ip), Port));
                if (
                    await Task.WhenAny(connectTask, Task.Delay(SearchTimeout, cancellationToken))
                    == connectTask
                )
                {
                    // 连接成功
                    Console.WriteLine("已连接到服务器");
                    _reconnectAttempts = 0;
                    // 开始监听断开
                    _ = Task.Run(() => ListenForDisconnectAsync(cancellationToken));
                    return;
                }
                else
                {
                    // 超时未连接
                    Console.WriteLine("连接超时，未找到服务器");
                    Disconnect();
                    break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"连接失败：{e.Message}");
                _reconnectAttempts++;
                await Task.Delay(2000, cancellationToken); // 等待2秒后重试
            }
        }

        Console.WriteLine("达到最大重连次数，停止尝试");
    }

    // 监听断开
    private async Task ListenForDisconnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            byte[] buffer = new byte[1];
            while (!cancellationToken.IsCancellationRequested)
            {
                int received = await _sender.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    SocketFlags.None
                );
                if (received == 0)
                {
                    throw new SocketException();
                }
            }
        }
        catch
        {
            Console.WriteLine("与服务器的连接已断开");
            _reconnectAttempts++;
            if (_reconnectAttempts <= MaxReconnectAttempts)
            {
                Console.WriteLine("尝试重新连接...");
                await ConnectAsync(cancellationToken);
            }
            else
            {
                Console.WriteLine("重连失败，停止客户端");
                Disconnect();
            }
        }
    }

    // 断开连接
    private void Disconnect()
    {
        try
        {
            _sender?.Shutdown(SocketShutdown.Both);
            _sender?.Close();
        }
        catch { }
        finally
        {
            _sender = null;
        }
    }

    // 重连
    public async Task ReconnectAsync(string ip, int port)
    {
        StopClient();
        Ip = ip;
        Port = port;
        await StartClientAsync();
    }

    // 发送文本数据
    public void SendTextData(string text)
    {
        if (_sender == null || !_sender.Connected)
        {
            Console.WriteLine("未连接到服务器");
            return;
        }

        try
        {
            byte[] typeData = Encoding.UTF8.GetBytes("T");
            _sender.Send(typeData);

            byte[] msg = Encoding.UTF8.GetBytes(text);
            byte[] msgLength = BitConverter.GetBytes(msg.Length);
            _sender.Send(msgLength);
            _sender.Send(msg);
            Console.WriteLine("已发送文本");
        }
        catch (Exception e)
        {
            Console.WriteLine($"发送文本失败：{e.Message}");
        }
    }

    // 发送文件数据
    public void SendFileData(string filePath)
    {
        if (_sender == null || !_sender.Connected)
        {
            Console.WriteLine("未连接到服务器");
            return;
        }

        try
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine("文件不存在");
                return;
            }

            byte[] typeData = Encoding.UTF8.GetBytes("F");
            _sender.Send(typeData);

            byte[] fileNameBytes = Encoding.UTF8.GetBytes(Path.GetFileName(filePath));
            byte[] fileNameLength = BitConverter.GetBytes(fileNameBytes.Length);
            _sender.Send(fileNameLength);
            _sender.Send(fileNameBytes);

            long fileSize = new FileInfo(filePath).Length;
            byte[] fileSizeBytes = BitConverter.GetBytes(fileSize);
            _sender.Send(fileSizeBytes);

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[8192];
                int bytesRead;
                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    _sender.Send(buffer, bytesRead, SocketFlags.None);
                }
            }
            Console.WriteLine("文件已发送");
        }
        catch (Exception e)
        {
            Console.WriteLine($"发送文件失败：{e.Message}");
        }
    }

    // 发送剪贴板内容
    public void SendClipboardData(string clipboardText)
    {
        if (_sender == null || !_sender.Connected)
        {
            Console.WriteLine("未连接到服务器");
            return;
        }

        try
        {
            byte[] typeData = Encoding.UTF8.GetBytes("C");
            _sender.Send(typeData);

            byte[] textBytes = Encoding.UTF8.GetBytes(clipboardText);
            byte[] textLength = BitConverter.GetBytes(textBytes.Length);
            _sender.Send(textLength);
            _sender.Send(textBytes);
            Console.WriteLine("剪贴板内容已发送");
        }
        catch (Exception e)
        {
            Console.WriteLine($"发送剪贴板内容失败：{e.Message}");
        }
    }

    // 发送文件夹数据
    public void SendFolderData(string folderPath)
    {
        if (_sender == null || !_sender.Connected)
        {
            Console.WriteLine("未连接到服务器");
            return;
        }

        try
        {
            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine("文件夹不存在");
                return;
            }

            string folderName = Path.GetFileName(folderPath);
            byte[] typeData = Encoding.UTF8.GetBytes("D");
            _sender.Send(typeData);

            byte[] nameBytes = Encoding.UTF8.GetBytes(folderName);
            byte[] nameLength = BitConverter.GetBytes(nameBytes.Length);
            _sender.Send(nameLength);
            _sender.Send(nameBytes);

            string[] files = Directory.GetFiles(folderPath);
            byte[] fileCount = BitConverter.GetBytes(files.Length);
            _sender.Send(fileCount);

            foreach (var file in files)
            {
                SendFileData(file);
            }
            Console.WriteLine("文件夹数据已发送");
        }
        catch (Exception e)
        {
            Console.WriteLine($"发送文件夹数据失败：{e.Message}");
        }
    }

    // 获取唯一文件路径
    private string GetUniqueFilePath(string filePath)
    {
        string directory = Path.GetDirectoryName(filePath) ?? string.Empty;
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        string extension = Path.GetExtension(filePath);
        int count = 1;

        while (File.Exists(filePath))
        {
            filePath = Path.Combine(directory, $"{fileName}({count}){extension}");
            count++;
        }

        return filePath;
    }
}
