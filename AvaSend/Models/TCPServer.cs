/*
 * @FilePath: TCPServer.cs
 * @Author: Window Wang 1598593280@qq.com
 * @Date: 2024-12-19 21:50:10
 * @LastEditors: Window Wang
 * @LastEditTime: 2024-12-20 00:54:52
 * 2024 by Window Wang, All Rights Reserved.
 * @Descripttion:
 */
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AvaSend.Models;

/// <summary>
/// TCP服务器
/// </summary>
public class TCPServer
{
    public string Ip { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 8080;
    public string SaveFolderPath { get; set; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads",
            "AvaSend"
        );

    public int SearchTimeout { get; set; } = 3000; // 毫秒

    private Socket _listener;
    private CancellationTokenSource _cancellationTokenSource;
    private int _reconnectAttempts = 0;
    private const int MaxReconnectAttempts = 5;

    // 启动服务器
    public async Task StartServerAsync()
    {
        if (!Directory.Exists(SaveFolderPath))
        {
            Directory.CreateDirectory(SaveFolderPath);
        }

        _cancellationTokenSource = new CancellationTokenSource();
        await RunServerAsync(_cancellationTokenSource.Token);
    }

    // 停止服务器
    public void StopServer()
    {
        _cancellationTokenSource?.Cancel();
        _listener?.Close();
    }

    // 运行服务器
    private async Task RunServerAsync(CancellationToken cancellationToken)
    {
        while (
            _reconnectAttempts < MaxReconnectAttempts && !cancellationToken.IsCancellationRequested
        )
        {
            try
            {
                StartServer();
                _reconnectAttempts = 0; // 重置重连次数
                break;
            }
            catch (Exception e)
            {
                Console.WriteLine($"服务器启动失败：{e.Message}");
                _reconnectAttempts++;
                Console.WriteLine(
                    $"尝试重新启动服务器（{_reconnectAttempts}/{MaxReconnectAttempts}）..."
                );
                await Task.Delay(2000, cancellationToken); // 等待2秒后重试
            }
        }

        if (_reconnectAttempts >= MaxReconnectAttempts)
        {
            Console.WriteLine("达到最大重连次数，停止尝试启动服务器。");
        }
    }

    // 启动服务器
    public void StartServer()
    {
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(Ip), Port);
        _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        _listener.Bind(endPoint);
        _listener.Listen(100);
        Console.WriteLine("服务器已启动，等待连接...");

        Task.Run(() => AcceptClientsAsync(_listener));
    }

    // 接受客户端连接
    private async Task AcceptClientsAsync(Socket listener)
    {
        while (true)
        {
            try
            {
                Socket handler = await listener.AcceptAsync();
                Console.WriteLine("客户端已连接");

                // 使用独立线程处理客户端
                _ = Task.Run(() => HandleClientAsync(handler));
            }
            catch (Exception e)
            {
                Console.WriteLine($"接受客户端时发生错误：{e.Message}");
                if (_reconnectAttempts < MaxReconnectAttempts)
                {
                    _reconnectAttempts++;
                    Console.WriteLine(
                        $"尝试重新启动服务器（{_reconnectAttempts}/{MaxReconnectAttempts}）..."
                    );
                    await Task.Delay(2000);
                    StartServer();
                }
                else
                {
                    Console.WriteLine("达到最大重连次数，停止接受客户端。");
                    break;
                }
            }
        }
    }

    // 重启服务器
    public void RestartServer(string ip, int port)
    {
        Ip = ip;
        Port = port;
        _listener?.Close();
        _reconnectAttempts = 0;
        Task.Run(() => RunServerAsync(_cancellationTokenSource.Token));
    }

    // 停止服务器
    private async Task HandleClientAsync(Socket handler)
    {
        try
        {
            byte[] typeBuffer = new byte[1];
            int bytesReceived = await handler.ReceiveAsync(
                new ArraySegment<byte>(typeBuffer),
                SocketFlags.None
            );
            if (bytesReceived == 0)
            {
                Console.WriteLine("客户端断开连接");
                handler.Close();
                return;
            }

            char dataType = (char)typeBuffer[0];

            switch (dataType)
            {
                case 'T':
                    await HandleTextDataAsync(handler);
                    break;
                case 'F':
                    await HandleFileDataAsync(handler, SaveFolderPath);
                    break;
                case 'C':
                    await HandleClipboardDataAsync(handler);
                    break;
                case 'D':
                    await HandleFolderDataAsync(handler, SaveFolderPath);
                    break;
                default:
                    Console.WriteLine("未知的数据类型");
                    break;
            }

            handler.Shutdown(SocketShutdown.Both);
            handler.Close();
        }
        catch (Exception e)
        {
            Console.WriteLine($"处理客户端时发生错误：{e.Message}");
        }
    }

    // 处理文本数据
    private async Task HandleTextDataAsync(Socket handler)
    {
        try
        {
            byte[] bufferLength = new byte[4];
            int bytesReceived = await handler.ReceiveAsync(
                new ArraySegment<byte>(bufferLength),
                SocketFlags.None
            );
            int textLength = BitConverter.ToInt32(bufferLength, 0);

            byte[] buffer = new byte[textLength];
            bytesReceived = await handler.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                SocketFlags.None
            );
            string receivedText = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
            Console.WriteLine("接收到的文本: " + receivedText);

            // 发送确认消息
            string confirmation = "文本已接收";
            byte[] confirmationBytes = Encoding.UTF8.GetBytes(confirmation);
            byte[] confirmationLength = BitConverter.GetBytes(confirmationBytes.Length);
            await handler.SendAsync(new ArraySegment<byte>(confirmationLength), SocketFlags.None);
            await handler.SendAsync(new ArraySegment<byte>(confirmationBytes), SocketFlags.None);
        }
        catch (Exception e)
        {
            Console.WriteLine($"处理文本数据时发生错误：{e.Message}");
        }
    }

    // 处理文件数据
    private async Task HandleFileDataAsync(Socket handler, string folderPath)
    {
        try
        {
            // 接收文件名长度
            byte[] nameLengthBuffer = new byte[4];
            int bytesReceived = await handler.ReceiveAsync(
                new ArraySegment<byte>(nameLengthBuffer),
                SocketFlags.None
            );
            int nameLength = BitConverter.ToInt32(nameLengthBuffer, 0);

            // 接收文件名
            byte[] nameBuffer = new byte[nameLength];
            bytesReceived = await handler.ReceiveAsync(
                new ArraySegment<byte>(nameBuffer),
                SocketFlags.None
            );
            string fileName = Encoding.UTF8.GetString(nameBuffer, 0, bytesReceived);

            // 接收文件大小
            byte[] sizeBuffer = new byte[8];
            bytesReceived = await handler.ReceiveAsync(
                new ArraySegment<byte>(sizeBuffer),
                SocketFlags.None
            );
            long fileSize = BitConverter.ToInt64(sizeBuffer, 0);

            // 检查并创建文件夹
            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine("文件夹不存在，正在创建...");
                Directory.CreateDirectory(folderPath);
            }

            string savePath = Path.Combine(folderPath, fileName);

            // 检查文件是否已存在
            if (File.Exists(savePath))
            {
                savePath = GetUniqueFilePath(savePath);
                Console.WriteLine($"文件已存在，保存为新文件：{savePath}");
            }

            // 接收文件数据
            using (FileStream fs = new FileStream(savePath, FileMode.Create, FileAccess.Write))
            {
                byte[] buffer = new byte[8192];
                long totalBytesReceived = 0;
                while (totalBytesReceived < fileSize)
                {
                    int bytesToReceive = (int)
                        Math.Min(buffer.Length, fileSize - totalBytesReceived);
                    bytesReceived = await handler.ReceiveAsync(
                        new ArraySegment<byte>(buffer, 0, bytesToReceive),
                        SocketFlags.None
                    );
                    if (bytesReceived == 0)
                    {
                        throw new SocketException();
                    }
                    await fs.WriteAsync(buffer, 0, bytesReceived);
                    totalBytesReceived += bytesReceived;
                }
            }
            Console.WriteLine("文件已保存到 " + savePath);

            // 发送确认消息
            string confirmation = "文件已接收";
            byte[] confirmationBytes = Encoding.UTF8.GetBytes(confirmation);
            byte[] confirmationLength = BitConverter.GetBytes(confirmationBytes.Length);
            await handler.SendAsync(new ArraySegment<byte>(confirmationLength), SocketFlags.None);
            await handler.SendAsync(new ArraySegment<byte>(confirmationBytes), SocketFlags.None);
        }
        catch (Exception e)
        {
            Console.WriteLine($"处理文件数据时发生错误：{e.Message}");
        }
    }

    // 处理剪贴板数据
    private async Task HandleClipboardDataAsync(Socket handler)
    {
        try
        {
            byte[] bufferLength = new byte[4];
            int bytesReceived = await handler.ReceiveAsync(
                new ArraySegment<byte>(bufferLength),
                SocketFlags.None
            );
            int textLength = BitConverter.ToInt32(bufferLength, 0);

            byte[] buffer = new byte[textLength];
            bytesReceived = await handler.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                SocketFlags.None
            );
            string clipboardText = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
            Console.WriteLine("接收到的剪贴板内容: " + clipboardText);

            // 发送确认消息
            string confirmation = "剪贴板内容已接收";
            byte[] confirmationBytes = Encoding.UTF8.GetBytes(confirmation);
            byte[] confirmationLength = BitConverter.GetBytes(confirmationBytes.Length);
            await handler.SendAsync(new ArraySegment<byte>(confirmationLength), SocketFlags.None);
            await handler.SendAsync(new ArraySegment<byte>(confirmationBytes), SocketFlags.None);
        }
        catch (Exception e)
        {
            Console.WriteLine($"处理剪贴板数据时发生错误：{e.Message}");
        }
    }

    // 处理文件夹数据
    private async Task HandleFolderDataAsync(Socket handler, string folderPath)
    {
        try
        {
            // 接收文件夹名长度
            byte[] nameLengthBuffer = new byte[4];
            int bytesReceived = await handler.ReceiveAsync(
                new ArraySegment<byte>(nameLengthBuffer),
                SocketFlags.None
            );
            int nameLength = BitConverter.ToInt32(nameLengthBuffer, 0);

            // 接收文件夹名
            byte[] nameBuffer = new byte[nameLength];
            bytesReceived = await handler.ReceiveAsync(
                new ArraySegment<byte>(nameBuffer),
                SocketFlags.None
            );
            string folderName = Encoding.UTF8.GetString(nameBuffer, 0, bytesReceived);

            string folderPathFull = Path.Combine(folderPath, folderName);

            if (!Directory.Exists(folderPathFull))
            {
                Directory.CreateDirectory(folderPathFull);
                Console.WriteLine($"文件夹已创建：{folderPathFull}");
            }
            else
            {
                Console.WriteLine($"文件夹已存在：{folderPathFull}");
            }

            // 发送确认消息
            string confirmation = "文件夹已接收";
            byte[] confirmationBytes = Encoding.UTF8.GetBytes(confirmation);
            byte[] confirmationLength = BitConverter.GetBytes(confirmationBytes.Length);
            await handler.SendAsync(new ArraySegment<byte>(confirmationLength), SocketFlags.None);
            await handler.SendAsync(new ArraySegment<byte>(confirmationBytes), SocketFlags.None);
        }
        catch (Exception e)
        {
            Console.WriteLine($"处理文件夹数据时发生错误：{e.Message}");
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
