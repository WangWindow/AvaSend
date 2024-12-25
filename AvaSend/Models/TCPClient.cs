using System;
using System.Diagnostics;
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
        try
        {
            if (!Directory.Exists(SaveFolderPath))
            {
                Directory.CreateDirectory(SaveFolderPath);
            }

            _cancellationTokenSource = new CancellationTokenSource();

            // 开始连接
            Debug.WriteLine($"正在连接服务器 {Ip}:{Port}...");
            await ConnectAsync(_cancellationTokenSource.Token);

            if (_sender != null && _sender.Connected)
            {
                Debug.WriteLine("客户端启动成功");
            }
            else
            {
                Debug.WriteLine("客户端启动失败 - 无法连接到服务器");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"启动客户端时发生错误: {ex.Message}");
            throw;
        }
    }

    private async Task ConnectAsync(CancellationToken cancellationToken)
    {
        while (
            _reconnectAttempts < MaxReconnectAttempts && !cancellationToken.IsCancellationRequested
        )
        {
            try
            {
                // 创建新的Socket连接
                _sender = new Socket(
                    AddressFamily.InterNetwork,
                    SocketType.Stream,
                    ProtocolType.Tcp
                );

                // 设置连接超时
                using var timeoutCts = new CancellationTokenSource(SearchTimeout);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    timeoutCts.Token,
                    cancellationToken
                );

                Debug.WriteLine($"尝试连接 {Ip}:{Port} (第{_reconnectAttempts + 1}次)");

                // 异步连接并等待结果
                await _sender
                    .ConnectAsync(new IPEndPoint(IPAddress.Parse(Ip), Port))
                    .WaitAsync(linkedCts.Token);

                if (_sender.Connected)
                {
                    Debug.WriteLine("连接成功");
                    _reconnectAttempts = 0;

                    // 启动连接状态监控
                    _ = Task.Run(
                        () => ListenForDisconnectAsync(cancellationToken),
                        cancellationToken
                    );
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("连接超时");
                Disconnect();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"连接失败: {ex.Message}");
                Disconnect();
            }

            if (cancellationToken.IsCancellationRequested)
            {
                Debug.WriteLine("连接已取消");
                break;
            }

            _reconnectAttempts++;
            if (_reconnectAttempts < MaxReconnectAttempts)
            {
                Debug.WriteLine($"等待重试... ({_reconnectAttempts}/{MaxReconnectAttempts})");
                await Task.Delay(2000, cancellationToken);
            }
            else
            {
                Debug.WriteLine("达到最大重试次数，停止连接");
            }
        }
    }

    private async Task ListenForDisconnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            byte[] buffer = new byte[1];
            while (!cancellationToken.IsCancellationRequested)
            {
                try
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
                catch
                {
                    Debug.WriteLine("检测到连接断开");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"连接监控发生错误: {ex.Message}");
        }
        finally
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                _reconnectAttempts++;
                if (_reconnectAttempts <= MaxReconnectAttempts)
                {
                    Debug.WriteLine("尝试重新连接...");
                    await ConnectAsync(cancellationToken);
                }
                else
                {
                    Debug.WriteLine("重连失败，停止客户端");
                    Disconnect();
                }
            }
        }
    }

    private void Disconnect()
    {
        try
        {
            if (_sender != null)
            {
                if (_sender.Connected)
                {
                    _sender.Shutdown(SocketShutdown.Both);
                }
                _sender.Close();
                _sender.Dispose();
                Debug.WriteLine("已断开连接");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"断开连接时发生错误: {ex.Message}");
        }
        finally
        {
            _sender = null;
        }
    }

    // 停止客户端
    public void StopClient()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        Disconnect();
    }

    // 重连
    public async Task ReconnectAsync(string ip, int port)
    {
        StopClient();
        Ip = ip;
        Port = port;
        await StartClientAsync();
    }

    /*
        // 发送文本数据
        public void SendText(string text)
        {
            if (_sender == null || !_sender.Connected)
            {
                Debug.WriteLine("未连接到服务器");
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
                Debug.WriteLine("已发送文本");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"发送文本失败：{e.Message}");
            }
        }

        // 发送文件数据
        public void SendFile(string filePath)
        {
            if (_sender == null || !_sender.Connected)
            {
                Debug.WriteLine("未连接到服务器");
                return;
            }

            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.WriteLine("文件不存在");
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
                Debug.WriteLine("文件已发送");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"发送文件失败：{e.Message}");
            }
        }

        // 发送文件夹数据
        public void SendFolder(string folderPath)
        {
            if (_sender == null || !_sender.Connected)
            {
                Debug.WriteLine("未连接到服务器");
                return;
            }

            try
            {
                if (!Directory.Exists(folderPath))
                {
                    Debug.WriteLine("文件夹不存在");
                    return;
                }

                string folderName = Path.GetFileName(folderPath);
                byte[] typeData = Encoding.UTF8.GetBytes("D");
                _sender.Send(typeData);

                byte[] nameBytes = Encoding.UTF8.GetBytes(folderName);
                byte[] nameLength = BitConverter.GetBytes(nameBytes.Length);
                _sender.Send(nameLength);
                _sender.Send(nameBytes);

                // 获取文件夹中的所有文件和子文件夹
                string[] files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);
                byte[] fileCount = BitConverter.GetBytes(files.Length);
                _sender.Send(fileCount);

                foreach (var file in files)
                {
                    // 发送相对路径
                    string relativePath = Path.GetRelativePath(folderPath, file);
                    byte[] relativePathBytes = Encoding.UTF8.GetBytes(relativePath);
                    byte[] relativePathLength = BitConverter.GetBytes(relativePathBytes.Length);
                    _sender.Send(relativePathLength);
                    _sender.Send(relativePathBytes);

                    // 发送文件大小
                    long fileSize = new FileInfo(file).Length;
                    byte[] fileSizeBytes = BitConverter.GetBytes(fileSize);
                    _sender.Send(fileSizeBytes);

                    // 发送文件数据
                    using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                    {
                        byte[] buffer = new byte[8192];
                        int bytesRead;
                        while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            _sender.Send(buffer, bytesRead, SocketFlags.None);
                        }
                    }
                }
                Debug.WriteLine("文件夹数据已发送");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"发送文件夹数据失败：{e.Message}");
            }
        }
    */

    // 发送文本数据（非阻塞方式）
    public async Task SendTextAsync(string text)
    {
        if (_sender == null || !_sender.Connected)
        {
            Debug.WriteLine("未连接到服务器");
            return;
        }

        try
        {
            byte[] typeData = Encoding.UTF8.GetBytes("T");
            await _sender.SendAsync(new ArraySegment<byte>(typeData), SocketFlags.None);

            byte[] msg = Encoding.UTF8.GetBytes(text);
            byte[] msgLength = BitConverter.GetBytes(msg.Length);
            await _sender.SendAsync(new ArraySegment<byte>(msgLength), SocketFlags.None);
            await _sender.SendAsync(new ArraySegment<byte>(msg), SocketFlags.None);
            Debug.WriteLine("已发送文本");
        }
        catch (Exception e)
        {
            Debug.WriteLine($"发送文本失败：{e.Message}");
        }
    }

    // 发送文件数据（非阻塞方式）
    public async Task SendFileAsync(string filePath)
    {
        if (_sender == null || !_sender.Connected)
        {
            Debug.WriteLine("未连接到服务器");
            return;
        }

        try
        {
            if (!File.Exists(filePath))
            {
                Debug.WriteLine("文件不存在");
                return;
            }

            byte[] typeData = Encoding.UTF8.GetBytes("F");
            await _sender.SendAsync(new ArraySegment<byte>(typeData), SocketFlags.None);

            byte[] fileNameBytes = Encoding.UTF8.GetBytes(Path.GetFileName(filePath));
            byte[] fileNameLength = BitConverter.GetBytes(fileNameBytes.Length);
            await _sender.SendAsync(new ArraySegment<byte>(fileNameLength), SocketFlags.None);
            await _sender.SendAsync(new ArraySegment<byte>(fileNameBytes), SocketFlags.None);

            long fileSize = new FileInfo(filePath).Length;
            byte[] fileSizeBytes = BitConverter.GetBytes(fileSize);
            await _sender.SendAsync(new ArraySegment<byte>(fileSizeBytes), SocketFlags.None);

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[8192];
                int bytesRead;
                while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await _sender.SendAsync(
                        new ArraySegment<byte>(buffer, 0, bytesRead),
                        SocketFlags.None
                    );
                }
            }
            Debug.WriteLine("文件已发送");
        }
        catch (Exception e)
        {
            Debug.WriteLine($"发送文件失败：{e.Message}");
        }
    }

    // 发送文件夹数据（非阻塞方式）
    public async Task SendFolderAsync(string folderPath)
    {
        if (_sender == null || !_sender.Connected)
        {
            Debug.WriteLine("未连接到服务器");
            return;
        }

        try
        {
            if (!Directory.Exists(folderPath))
            {
                Debug.WriteLine("文件夹不存在");
                return;
            }

            string folderName = Path.GetFileName(folderPath);
            byte[] typeData = Encoding.UTF8.GetBytes("D");
            await _sender.SendAsync(new ArraySegment<byte>(typeData), SocketFlags.None);

            byte[] nameBytes = Encoding.UTF8.GetBytes(folderName);
            byte[] nameLength = BitConverter.GetBytes(nameBytes.Length);
            await _sender.SendAsync(new ArraySegment<byte>(nameLength), SocketFlags.None);
            await _sender.SendAsync(new ArraySegment<byte>(nameBytes), SocketFlags.None);

            // 获取文件夹中的所有文件和子文件夹
            string[] files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);
            byte[] fileCount = BitConverter.GetBytes(files.Length);
            await _sender.SendAsync(new ArraySegment<byte>(fileCount), SocketFlags.None);

            foreach (var file in files)
            {
                // 发送相对路径
                string relativePath = Path.GetRelativePath(folderPath, file);
                byte[] relativePathBytes = Encoding.UTF8.GetBytes(relativePath);
                byte[] relativePathLength = BitConverter.GetBytes(relativePathBytes.Length);
                await _sender.SendAsync(
                    new ArraySegment<byte>(relativePathLength),
                    SocketFlags.None
                );
                await _sender.SendAsync(
                    new ArraySegment<byte>(relativePathBytes),
                    SocketFlags.None
                );

                // 发送文件大小
                long fileSize = new FileInfo(file).Length;
                byte[] fileSizeBytes = BitConverter.GetBytes(fileSize);
                await _sender.SendAsync(new ArraySegment<byte>(fileSizeBytes), SocketFlags.None);

                // 发送文件数据
                using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    byte[] buffer = new byte[8192];
                    int bytesRead;
                    while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await _sender.SendAsync(
                            new ArraySegment<byte>(buffer, 0, bytesRead),
                            SocketFlags.None
                        );
                    }
                }
            }
            Debug.WriteLine("文件夹数据已发送");
        }
        catch (Exception e)
        {
            Debug.WriteLine($"发送文件夹数据失败：{e.Message}");
        }
    }
}
