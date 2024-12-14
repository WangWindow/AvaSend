using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace AvaSend.Models;

/// <summary>
/// TCP 服务器
/// </summary>
public class TCPServer
{
    public string Ip { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 8080;

    /// <summary>
    /// 启动服务器并等待客户端连接
    /// </summary>
    public void StartServer()
    {
        ConfigureServer();

        // 创建Socket
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(Ip), Port);
        Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        try
        {
            listener.Bind(endPoint);
            listener.Listen(10);

            Console.WriteLine("等待连接...");

            while (true)
            {
                Socket handler = listener.Accept();
                Console.WriteLine("连接已建立");

                HandleClient(handler);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    /// <summary>
    /// 启动服务器并使用指定的IP和端口
    /// </summary>
    /// <param name="ip">服务器IP地址</param>
    /// <param name="port">服务器端口</param>
    public void StartServer(string ip, int port)
    {
        Ip = ip;
        Port = port;
        StartServer();
    }

    /// <summary>
    /// 配置服务器的IP和端口
    /// </summary>
    private void ConfigureServer(string inputIp = "127.0.0.1", string inputPort = "8080")
    {
        // 设置默认IP和端口
        if (!string.IsNullOrEmpty(inputIp))
        {
            Ip = inputIp;
        }
        if (int.TryParse(inputPort, out int parsedPort))
        {
            Port = parsedPort;
        }
    }

    /// <summary>
    /// 处理客户端连接
    /// </summary>
    /// <param name="handler">客户端Socket</param>
    private void HandleClient(Socket handler)
    {
        // 接收数据类型
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
            HandleFileData(handler);
        }

        handler.Shutdown(SocketShutdown.Both);
        handler.Close();
    }

    /// <summary>
    /// 处理文本数据
    /// </summary>
    /// <param name="handler">客户端Socket</param>
    private void HandleTextData(Socket handler)
    {
        // 接收文本数据
        byte[] buffer = new byte[1024];
        int bytesReceived = handler.Receive(buffer);
        string receivedText = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
        Console.WriteLine("接收到的文本: " + receivedText);
    }

    /// <summary>
    /// 处理文件数据
    /// </summary>
    /// <param name="handler">客户端Socket</param>
    private void HandleFileData(Socket handler)
    {
        // 接收文件名
        byte[] nameBuffer = new byte[1024];
        int bytesReceived = handler.Receive(nameBuffer);
        string fileName = Encoding.UTF8.GetString(nameBuffer, 0, bytesReceived);

        // 接收文件大小
        byte[] sizeBuffer = new byte[8];
        handler.Receive(sizeBuffer);
        long fileSize = BitConverter.ToInt64(sizeBuffer, 0);

        // 接收文件数据
        Console.WriteLine("请输入保存文件的文件夹路径：");
        string folderPath = Console.ReadLine();

        if (!Directory.Exists(folderPath))
        {
            Console.WriteLine("文件夹不存在，创建文件夹...");
            Directory.CreateDirectory(folderPath);
        }

        string savePath = Path.Combine(folderPath, fileName);

        if (File.Exists(savePath))
        {
            Console.WriteLine("文件已存在，选择覆盖 (O) 还是另存 (S)？");
            string choice = Console.ReadLine();
            if (choice.ToUpper() == "S")
            {
                savePath = GetUniqueFilePath(savePath);
            }
        }

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
    /// 获取唯一的文件路径
    /// </summary>
    /// <param name="filePath">原始文件路径</param>
    /// <returns>唯一的文件路径</returns>
    private string GetUniqueFilePath(string filePath)
    {
        string directory = Path.GetDirectoryName(filePath);
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
/// TCP 客户端
/// </summary>
public class TCPClient
{
    public string Ip { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 8080;

    /// <summary>
    /// 启动客户端并连接到服务器
    /// </summary>
    public void StartClient()
    {
        ConfigureClient();

        // 创建Socket
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(Ip), Port);
        Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        try
        {
            sender.Connect(endPoint);
            Console.WriteLine("连接到服务器");

            while (true)
            {
                Console.WriteLine("请选择发送类型: 1. 字符 2. 文件 3. 断开连接");
                string sendChoice = Console.ReadLine();

                if (sendChoice == "1")
                {
                    SendTextData(sender);
                }
                else if (sendChoice == "2")
                {
                    SendFileData(sender);
                }
                else if (sendChoice == "3")
                {
                    Console.WriteLine("断开连接");
                    break;
                }
                else
                {
                    Console.WriteLine("无效选择");
                }
            }

            sender.Shutdown(SocketShutdown.Both);
            sender.Close();
        }
        catch (Exception e)
        {
            Console.WriteLine("连接失败，请重新输入IP和端口。");
            Console.WriteLine(e.ToString());
        }
    }

    /// <summary>
    /// 启动客户端并使用指定的IP和端口连接到服务器
    /// </summary>
    /// <param name="ip">服务器IP地址</param>
    /// <param name="port">服务器端口</param>
    public void StartClient(string ip, int port)
    {
        Ip = ip;
        Port = port;
        StartClient();
    }

    /// <summary>
    /// 配置客户端的IP和端口
    /// </summary>
    private void ConfigureClient()
    {
        // 设置默认IP和端口
        Console.WriteLine("请输入IP地址（默认127.0.0.1）：");
        string inputIp = Console.ReadLine();
        if (!string.IsNullOrEmpty(inputIp))
        {
            Ip = inputIp;
        }

        Console.WriteLine("请输入端口（默认8080）：");
        string inputPort = Console.ReadLine();
        if (int.TryParse(inputPort, out int parsedPort))
        {
            Port = parsedPort;
        }
    }

    /// <summary>
    /// 发送文本数据到服务器
    /// </summary>
    /// <param name="sender">客户端Socket</param>
    private void SendTextData(Socket sender)
    {
        // 发送字符
        sender.Send(Encoding.UTF8.GetBytes("T"));

        Console.WriteLine("请输入要发送的字符：");
        string textToSend = Console.ReadLine();
        byte[] msg = Encoding.UTF8.GetBytes(textToSend);
        sender.Send(msg);
        Console.WriteLine("已发送字符");
    }

    /// <summary>
    /// 发送文件数据到服务器
    /// </summary>
    /// <param name="sender">客户端Socket</param>
    private void SendFileData(Socket sender)
    {
        // 发送文件
        sender.Send(Encoding.UTF8.GetBytes("F"));

        Console.WriteLine("请输入文件路径：");
        string filePath = Console.ReadLine();
        if (File.Exists(filePath))
        {
            // 发送文件名
            string fileName = Path.GetFileName(filePath);
            byte[] nameBuffer = Encoding.UTF8.GetBytes(fileName);
            sender.Send(nameBuffer);

            // 发送文件大小
            long fileSize = new FileInfo(filePath).Length;
            byte[] sizeBuffer = BitConverter.GetBytes(fileSize);
            sender.Send(sizeBuffer);

            // 发送文件数据
            byte[] fileData = File.ReadAllBytes(filePath);
            sender.Send(fileData);
            Console.WriteLine("已发送文件");

            // 接收服务器的确认消息
            byte[] buffer = new byte[1024];
            int bytesReceived = sender.Receive(buffer);
            string confirmation = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
            Console.WriteLine(confirmation);
        }
        else
        {
            Console.WriteLine("文件不存在");
        }
    }
}
