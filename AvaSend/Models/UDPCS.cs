using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace AvaSend.Models;

/// <summary>
/// UDP 服务器
/// </summary>
public class UDPServer
{
    public string Ip { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 8080;

    public void StartServer()
    {
        UdpClient udpServer = new UdpClient(Port);
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, Port);

        try
        {
            Console.WriteLine("UDP 服务器已启动，等待消息...");

            while (true)
            {
                byte[] data = udpServer.Receive(ref remoteEP);
                string receivedText = Encoding.UTF8.GetString(data);
                Console.WriteLine($"接收到的消息: {receivedText}");

                // 发送确认消息
                byte[] confirmData = Encoding.UTF8.GetBytes("消息已接收");
                udpServer.Send(confirmData, confirmData.Length, remoteEP);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
        finally
        {
            udpServer.Close();
        }
    }
}

/// <summary>
/// UDP 客户端
/// </summary>
public class UDPClient
{
    public string Ip { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 8080;

    public void StartClient()
    {
        while (true)
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

            UdpClient udpClient = new UdpClient();
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(Ip), Port);

            try
            {
                while (true)
                {
                    Console.WriteLine("请选择发送类型: 1. 字符 2. 文件 3. 断开连接");
                    string sendChoice = Console.ReadLine();

                    if (sendChoice == "1")
                    {
                        // 发送字符
                        Console.WriteLine("请输入要发送的字符：");
                        string message = Console.ReadLine();
                        byte[] data = Encoding.UTF8.GetBytes(message);
                        udpClient.Send(data, data.Length, remoteEP);

                        // 接收确认消息
                        IPEndPoint fromEP = new IPEndPoint(IPAddress.Any, 0);
                        byte[] receivedData = udpClient.Receive(ref fromEP);
                        string confirmation = Encoding.UTF8.GetString(receivedData);
                        Console.WriteLine($"服务器确认: {confirmation}");
                    }
                    else if (sendChoice == "2")
                    {
                        // 发送文件
                        Console.WriteLine("请输入文件路径：");
                        string filePath = Console.ReadLine();
                        if (File.Exists(filePath))
                        {
                            // 发送文件名
                            string fileName = Path.GetFileName(filePath);
                            byte[] nameBuffer = Encoding.UTF8.GetBytes(fileName);
                            udpClient.Send(nameBuffer, nameBuffer.Length, remoteEP);

                            // 发送文件大小
                            long fileSize = new FileInfo(filePath).Length;
                            byte[] sizeBuffer = BitConverter.GetBytes(fileSize);
                            udpClient.Send(sizeBuffer, sizeBuffer.Length, remoteEP);

                            // 发送文件数据
                            byte[] fileData = File.ReadAllBytes(filePath);
                            udpClient.Send(fileData, fileData.Length, remoteEP);
                            Console.WriteLine("已发送文件");

                            // 接收服务器的确认消息
                            IPEndPoint fromEP = new IPEndPoint(IPAddress.Any, 0);
                            byte[] receivedData = udpClient.Receive(ref fromEP);
                            string confirmation = Encoding.UTF8.GetString(receivedData);
                            Console.WriteLine($"服务器确认: {confirmation}");
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
            }
            catch (Exception e)
            {
                Console.WriteLine("连接失败，请重新输入IP和端口。");
                Console.WriteLine(e.ToString());
            }
            finally
            {
                udpClient.Close();
            }
        }
    }
}
