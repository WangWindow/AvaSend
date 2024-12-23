using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AvaSend.Models;

/// <summary>
/// UDP客户端
/// </summary>
public class UDPClient
{
    // 设置接口
    public string Ip { get; set; } = "127.0.0.1";

    public int Port { get; set; } = 8081;

    public string SaveFolderPath { get; set; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads",
            "AvaSend"
        );

    public int SearchTimeout { get; set; } = 3000; // 毫秒

    private UdpClient _client;
    private IPEndPoint _endPoint;

    // 启动客户端
    public async Task StartClientAsync()
    {
        if (!Directory.Exists(SaveFolderPath))
        {
            Directory.CreateDirectory(SaveFolderPath);
        }

        _client = new UdpClient();
        _endPoint = new IPEndPoint(IPAddress.Parse(Ip), Port);
        await Task.CompletedTask;
    }

    // 停止客户端
    public void StopClient()
    {
        _client?.Close();
    }

    // 发送数据包
    private async Task SendPacketAsync(byte[] data)
    {
        await _client.SendAsync(data, data.Length, _endPoint);
    }

    // 发送文本数据
    public async Task SendTextDataAsync(string text)
    {
        try
        {
            byte[] typeData = Encoding.UTF8.GetBytes("T");
            await SendPacketAsync(typeData);

            byte[] msg = Encoding.UTF8.GetBytes(text);
            byte[] msgLength = BitConverter.GetBytes(msg.Length);
            await SendPacketAsync(msgLength);
            await SendPacketAsync(msg);
            Debug.WriteLine("已发送文本");
        }
        catch (Exception e)
        {
            Debug.WriteLine($"发送文本失败：{e.Message}");
        }
    }

    // 发送文件数据
    public async Task SendFileDataAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Debug.WriteLine("文件不存在");
                return;
            }

            byte[] typeData = Encoding.UTF8.GetBytes("F");
            await SendPacketAsync(typeData);

            byte[] fileNameBytes = Encoding.UTF8.GetBytes(Path.GetFileName(filePath));
            byte[] fileNameLength = BitConverter.GetBytes(fileNameBytes.Length);
            await SendPacketAsync(fileNameLength);
            await SendPacketAsync(fileNameBytes);

            long fileSize = new FileInfo(filePath).Length;
            byte[] fileSizeBytes = BitConverter.GetBytes(fileSize);
            await SendPacketAsync(fileSizeBytes);

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[8192];
                int bytesRead;
                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    await SendPacketAsync(buffer[..bytesRead]);
                }
            }
            Debug.WriteLine("文件已发送");
        }
        catch (Exception e)
        {
            Debug.WriteLine($"发送文件失败：{e.Message}");
        }
    }

    // 发送剪贴板内容
    public async Task SendClipboardDataAsync(string clipboardText)
    {
        try
        {
            byte[] typeData = Encoding.UTF8.GetBytes("C");
            await SendPacketAsync(typeData);

            byte[] textBytes = Encoding.UTF8.GetBytes(clipboardText);
            byte[] textLength = BitConverter.GetBytes(textBytes.Length);
            await SendPacketAsync(textLength);
            await SendPacketAsync(textBytes);
            Debug.WriteLine("剪贴板内容已发送");
        }
        catch (Exception e)
        {
            Debug.WriteLine($"发送剪贴板内容失败：{e.Message}");
        }
    }

    // 发送文件夹数据
    public async Task SendFolderDataAsync(string folderPath)
    {
        try
        {
            if (!Directory.Exists(folderPath))
            {
                Debug.WriteLine("文件夹不存在");
                return;
            }

            string folderName = Path.GetFileName(folderPath);
            byte[] typeData = Encoding.UTF8.GetBytes("D");
            await SendPacketAsync(typeData);

            byte[] nameBytes = Encoding.UTF8.GetBytes(folderName);
            byte[] nameLength = BitConverter.GetBytes(nameBytes.Length);
            await SendPacketAsync(nameLength);
            await SendPacketAsync(nameBytes);

            string[] files = Directory.GetFiles(folderPath);
            byte[] fileCount = BitConverter.GetBytes(files.Length);
            await SendPacketAsync(fileCount);

            foreach (var file in files)
            {
                await SendFileDataAsync(file);
            }
            Debug.WriteLine("文件夹数据已发送");
        }
        catch (Exception e)
        {
            Debug.WriteLine($"发送文件夹数据失败：{e.Message}");
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
