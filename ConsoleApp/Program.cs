using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Threading;

Console.WriteLine("请选择启动类型: 1. 服务端 2. 客户端");
string choice = Console.ReadLine();

if (choice == "1")
{
    StartServer();
}
else if (choice == "2")
{
    StartClient();
}
else
{
    Console.WriteLine("无效选择");
}

void StartServer()
{
    // 设置默认IP和端口
    string ip = "127.0.0.1";
    int port = 8080;

    Console.WriteLine("请输入IP地址（默认127.0.0.1）：");
    string inputIp = Console.ReadLine();
    if (!string.IsNullOrEmpty(inputIp))
    {
        ip = inputIp;
    }

    Console.WriteLine("请输入端口（默认8080）：");
    string inputPort = Console.ReadLine();
    if (int.TryParse(inputPort, out int parsedPort))
    {
        port = parsedPort;
    }

    // 创建Socket
    IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
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

            // 接收数据类型
            byte[] typeBuffer = new byte[1];
            int bytesReceived = handler.Receive(typeBuffer);
            if (bytesReceived == 0)
            {
                Console.WriteLine("客户端断开连接");
                handler.Close();
                continue;
            }

            char dataType = (char)typeBuffer[0];

            if (dataType == 'T')
            {
                // 接收文本数据
                byte[] buffer = new byte[1024];
                bytesReceived = handler.Receive(buffer);
                string receivedText = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
                Console.WriteLine("接收到的文本: " + receivedText);
            }
            else if (dataType == 'F')
            {
                // 接收文件名
                byte[] nameBuffer = new byte[1024];
                bytesReceived = handler.Receive(nameBuffer);
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

            handler.Shutdown(SocketShutdown.Both);
            handler.Close();
        }
    }
    catch (Exception e)
    {
        Console.WriteLine(e.ToString());
    }
}

string GetUniqueFilePath(string filePath)
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

void StartClient()
{
    // 设置默认IP和端口
    string ip = "127.0.0.1";
    int port = 8080;

    Console.WriteLine("请输入IP地址（默认127.0.0.1）：");
    string inputIp = Console.ReadLine();
    if (!string.IsNullOrEmpty(inputIp))
    {
        ip = inputIp;
    }

    Console.WriteLine("请输入端口（默认8080）：");
    string inputPort = Console.ReadLine();
    if (int.TryParse(inputPort, out int parsedPort))
    {
        port = parsedPort;
    }

    // 创建Socket
    IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
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
                // 发送字符
                sender.Send(Encoding.UTF8.GetBytes("T"));

                Console.WriteLine("请输入要发送的字符：");
                string textToSend = Console.ReadLine();
                byte[] msg = Encoding.UTF8.GetBytes(textToSend);
                sender.Send(msg);
                Console.WriteLine("已发送字符");
            }
            else if (sendChoice == "2")
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
        Console.WriteLine(e.ToString());
    }
}