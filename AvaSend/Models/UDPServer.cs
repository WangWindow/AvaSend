using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AvaSend.Models;

/// <summary>
/// UDP服务器
/// </summary>
public class UDPServer
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

    private UdpClient _listener;
    private CancellationTokenSource _cancellationTokenSource;

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
        try
        {
            _listener = new UdpClient(Port);
            Console.WriteLine("服务器已启动，等待连接...");

            while (!cancellationToken.IsCancellationRequested)
            {
                UdpReceiveResult result = await _listener.ReceiveAsync();
                _ = Task.Run(() => HandleClientAsync(result));
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"服务器运行时发生错误：{e.Message}");
        }
    }

    // 处理客户端数据
    private async Task HandleClientAsync(UdpReceiveResult result)
    {
        try
        {
            byte[] typeBuffer = result.Buffer;
            char dataType = (char)typeBuffer[0];

            switch (dataType)
            {
                case 'T':
                    await HandleTextDataAsync(result);
                    break;
                case 'F':
                    await HandleFileDataAsync(result, SaveFolderPath);
                    break;
                case 'C':
                    await HandleClipboardDataAsync(result);
                    break;
                case 'D':
                    await HandleFolderDataAsync(result, SaveFolderPath);
                    break;
                default:
                    Console.WriteLine("未知的数据类型");
                    break;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"处理客户端数据时发生错误：{e.Message}");
        }
    }

    // 处理文本数据
    private async Task HandleTextDataAsync(UdpReceiveResult result)
    {
        try
        {
            byte[] buffer = result.Buffer;
            int textLength = BitConverter.ToInt32(buffer, 1);

            string receivedText = Encoding.UTF8.GetString(buffer, 5, textLength);
            Console.WriteLine("接收到的文本: " + receivedText);

            // 发送确认消息
            string confirmation = "文本已接收";
            byte[] confirmationBytes = Encoding.UTF8.GetBytes(confirmation);
            await _listener.SendAsync(
                confirmationBytes,
                confirmationBytes.Length,
                result.RemoteEndPoint
            );
        }
        catch (Exception e)
        {
            Console.WriteLine($"处理文本数据时发生错误：{e.Message}");
        }
    }

    // 处理文件数据
    private async Task HandleFileDataAsync(UdpReceiveResult result, string folderPath)
    {
        try
        {
            byte[] buffer = result.Buffer;
            int nameLength = BitConverter.ToInt32(buffer, 1);
            string fileName = Encoding.UTF8.GetString(buffer, 5, nameLength);

            long fileSize = BitConverter.ToInt64(buffer, 5 + nameLength);

            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine("文件夹不存在，正在创建...");
                Directory.CreateDirectory(folderPath);
            }

            string savePath = Path.Combine(folderPath, fileName);

            if (File.Exists(savePath))
            {
                savePath = GetUniqueFilePath(savePath);
                Console.WriteLine($"文件已存在，保存为新文件：{savePath}");
            }

            using (FileStream fs = new FileStream(savePath, FileMode.Create, FileAccess.Write))
            {
                long totalBytesReceived = 0;
                while (totalBytesReceived < fileSize)
                {
                    UdpReceiveResult fileResult = await _listener.ReceiveAsync();
                    byte[] fileBuffer = fileResult.Buffer;
                    await fs.WriteAsync(fileBuffer, 0, fileBuffer.Length);
                    totalBytesReceived += fileBuffer.Length;
                }
            }
            Console.WriteLine("文件已保存到 " + savePath);

            // 发送确认消息
            string confirmation = "文件已接收";
            byte[] confirmationBytes = Encoding.UTF8.GetBytes(confirmation);
            await _listener.SendAsync(
                confirmationBytes,
                confirmationBytes.Length,
                result.RemoteEndPoint
            );
        }
        catch (Exception e)
        {
            Console.WriteLine($"处理文件数据时发生错误：{e.Message}");
        }
    }

    // 处理剪贴板数据
    private async Task HandleClipboardDataAsync(UdpReceiveResult result)
    {
        try
        {
            byte[] buffer = result.Buffer;
            int textLength = BitConverter.ToInt32(buffer, 1);

            string clipboardText = Encoding.UTF8.GetString(buffer, 5, textLength);
            Console.WriteLine("接收到的剪贴板内容: " + clipboardText);

            // 发送确认消息
            string confirmation = "剪贴板内容已接收";
            byte[] confirmationBytes = Encoding.UTF8.GetBytes(confirmation);
            await _listener.SendAsync(
                confirmationBytes,
                confirmationBytes.Length,
                result.RemoteEndPoint
            );
        }
        catch (Exception e)
        {
            Console.WriteLine($"处理剪贴板数据时发生错误：{e.Message}");
        }
    }

    // 处理文件夹数据
    private async Task HandleFolderDataAsync(UdpReceiveResult result, string folderPath)
    {
        try
        {
            byte[] buffer = result.Buffer;
            int nameLength = BitConverter.ToInt32(buffer, 1);
            string folderName = Encoding.UTF8.GetString(buffer, 5, nameLength);

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
            await _listener.SendAsync(
                confirmationBytes,
                confirmationBytes.Length,
                result.RemoteEndPoint
            );
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
