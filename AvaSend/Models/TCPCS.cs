using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Runtime.InteropServices;

namespace AvaSend.Models;

/// <summary>
///   TCP 服务器
/// </summary>
public class TCPServer
{
    public string Ip { get; set; }

    public int Port { get; set; }

    public string FileSavePath { get; set; }

    private Socket listener;

    public TCPServer()
    {
        Ip = "127.0.0.1";
        Port = 8080;
        FileSavePath = GetDefaultDownloadFolder();
    }

    /// <summary>
    ///   获取用户下载文件夹路径
    /// </summary>
    /// <returns> 下载文件夹路径 </returns>
    private string GetDefaultDownloadFolder()
    {
        string homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Path.Combine(homePath, "Downloads");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return Path.Combine(homePath, "Downloads");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return Path.Combine(homePath, "Downloads");
        }
        else
        {
            return homePath;
        }
    }

    /// <summary>
    ///   启动服务器并等待客户端连接
    /// </summary>
    public void StartServer()
    {
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(Ip), Port);
        listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        try
        {
            listener.Bind(endPoint);
            listener.Listen(10);
            Console.WriteLine("服务器已启动，等待连接...");

            while (true)
            {
                Socket handler = listener.Accept();
                Console.WriteLine("客户端已连接");

                HandleClient(handler);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"服务器启动失败：{e.Message}");
        }
    }

    /// <summary>
    ///   使用新的IP和端口重新启动服务器
    /// </summary>
    /// <param name="ip"> 服务器IP地址 </param>
    /// <param name="port"> 服务器端口 </param>
    public void RestartServer(string ip, int port)
    {
        Ip = ip;
        Port = port;
        listener?.Close();
        StartServer();
    }

    /// <summary>
    ///   处理客户端连接
    /// </summary>
    /// <param name="handler"> 客户端Socket </param>
    private void HandleClient(Socket handler)
    {
        try
        {
            byte[] typeBuffer = new byte[1];
            int bytesReceived = handler.Receive(typeBuffer);
            if (bytesReceived == 0)
            {
                Console.WriteLine("客户端断开连接");
                handler.Close();
                return;
            }

            char dataType = (char)typeBuffer[0];

            if (dataType == 'T')
            {
                HandleTextData(handler);
            }
            else if (dataType == 'F')
            {
                HandleFileData(handler, FileSavePath);
            }

            handler.Shutdown(SocketShutdown.Both);
            handler.Close();
        }
        catch (Exception e)
        {
            Console.WriteLine($"处理客户端时发生错误：{e.Message}");
        }
    }

    /// <summary>
    ///   处理文本数据
    /// </summary>
    /// <param name="handler"> 客户端Socket </param>
    private void HandleTextData(Socket handler)
    {
        byte[] buffer = new byte[1024];
        int bytesReceived = handler.Receive(buffer);
        string receivedText = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
        Console.WriteLine("接收到的文本: " + receivedText);
    }

    /// <summary>
    ///   处理文件数据
    /// </summary>
    /// <param name="handler"> 客户端Socket </param>
    /// <param name="folderPath"> 保存文件的文件夹路径 </param>
    private void HandleFileData(Socket handler, string folderPath)
    {
        // 接收文件名
        byte[] nameBuffer = new byte[1024];
        int bytesReceived = handler.Receive(nameBuffer);
        string fileName = Encoding.UTF8.GetString(nameBuffer, 0, bytesReceived);

        // 接收文件大小
        byte[] sizeBuffer = new byte[8];
        handler.Receive(sizeBuffer);
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
                bytesReceived = handler.Receive(buffer);
                fs.Write(buffer, 0, bytesReceived);
                totalBytesReceived += bytesReceived;
            }
        }
        Console.WriteLine("文件已保存到 " + savePath);

        // 通知客户端文件已接收
        handler.Send(Encoding.UTF8.GetBytes("文件已接收"));
    }

    /// <summary>
    ///   获取唯一的文件路径
    /// </summary>
    /// <param name="filePath"> 原始文件路径 </param>
    /// <returns> 唯一的文件路径 </returns>
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

/// <summary>
///   TCP 客户端
/// </summary>
public class TCPClient
{
    public string Ip { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 8080;

    private Socket sender;

    /// <summary>
    ///   启动客户端并连接到服务器
    /// </summary>
    public void StartClient()
    {
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(Ip), Port);
        sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        try
        {
            sender.Connect(endPoint);
            Console.WriteLine("已连接到服务器");
        }
        catch (Exception e)
        {
            Console.WriteLine($"连接失败：{e.Message}");
        }
    }

    /// <summary>
    ///   使用新的IP和端口重新连接
    /// </summary>
    /// <param name="ip"> 服务器IP地址 </param>
    /// <param name="port"> 服务器端口 </param>
    public void Reconnect(string ip, int port)
    {
        Ip = ip;
        Port = port;
        if (sender != null && sender.Connected)
        {
            sender.Shutdown(SocketShutdown.Both);
            sender.Close();
        }
        StartClient();
    }

    /// <summary>
    ///   发送文本数据到服务器
    /// </summary>
    /// <param name="text"> 要发送的文本 </param>
    public void SendTextData(string text)
    {
        try
        {
            // 发送类型标识
            sender.Send(Encoding.UTF8.GetBytes("T"));

            // 发送文本数据
            byte[] msg = Encoding.UTF8.GetBytes(text);
            sender.Send(msg);
            Console.WriteLine("已发送文本");
        }
        catch (Exception e)
        {
            Console.WriteLine($"发送文本失败：{e.Message}");
        }
    }

    /// <summary>
    ///   发送文件数据到服务器
    /// </summary>
    /// <param name="filePath"> 要发送的文件路径 </param>
    public void SendFileData(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine("文件不存在");
                return;
            }

            // 发送类型标识
            sender.Send(Encoding.UTF8.GetBytes("F"));

            // 发送文件名
            string fileName = Path.GetFileName(filePath);
            byte[] nameBuffer = Encoding.UTF8.GetBytes(fileName);
            sender.Send(nameBuffer);

            // 发送文件大小
            long fileSize = new FileInfo(filePath).Length;
            byte[] sizeBuffer = BitConverter.GetBytes(fileSize);
            sender.Send(sizeBuffer);

            // 发送文件数据
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[8192];
                int bytesRead;
                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    sender.Send(buffer, bytesRead, SocketFlags.None);
                }
            }
            Console.WriteLine("已发送文件");

            // 接收服务器的确认消息
            byte[] bufferResponse = new byte[1024];
            int bytesReceived = sender.Receive(bufferResponse);
            string confirmation = Encoding.UTF8.GetString(bufferResponse, 0, bytesReceived);
            Console.WriteLine("服务器响应: " + confirmation);
        }
        catch (Exception e)
        {
            Console.WriteLine($"发送文件失败：{e.Message}");
        }
    }
}
