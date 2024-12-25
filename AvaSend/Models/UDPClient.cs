using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AvaSend.Models
{
    public class UDPClient
    {
        public string Ip { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 8081;
        private UdpClient _client;
        private const int MAX_CHUNK = 8192; // 8KB chunks

        public void Start() => _client = new UdpClient();

        public void Stop() => _client?.Close();

        public async Task SendTextAsync(string text)
        {
            await SendPacketAsync('T', Encoding.UTF8.GetBytes(text));
        }

        public async Task SendFileAsync(string filePath, string relativePath = null)
        {
            try
            {
                string fileName = Path.GetFileName(filePath);
                await SendPacketAsync('F', Encoding.UTF8.GetBytes(fileName));

                if (!string.IsNullOrEmpty(relativePath))
                {
                    await SendPacketAsync('P', Encoding.UTF8.GetBytes(relativePath));
                }

                using var fs = File.OpenRead(filePath);
                byte[] buffer = new byte[MAX_CHUNK];
                int bytesRead;

                while ((bytesRead = await fs.ReadAsync(buffer, 0, MAX_CHUNK)) > 0)
                {
                    byte[] chunk = new byte[bytesRead];
                    Buffer.BlockCopy(buffer, 0, chunk, 0, bytesRead);
                    await SendPacketAsync('C', chunk);
                }

                await SendPacketAsync('E', Array.Empty<byte>());
            }
            catch (Exception ex)
            {
                throw new Exception($"发送文件失败: {ex.Message}");
            }
        }

        public async Task SendFolderAsync(string folderPath)
        {
            try
            {
                string folderName = Path.GetFileName(folderPath);
                await SendPacketAsync('D', Encoding.UTF8.GetBytes(folderName));

                foreach (
                    string file in Directory.GetFiles(
                        folderPath,
                        "*.*",
                        SearchOption.AllDirectories
                    )
                )
                {
                    string relativePath = Path.GetRelativePath(folderPath, file);
                    await SendFileAsync(file, relativePath);
                }

                await SendPacketAsync('E', Array.Empty<byte>());
            }
            catch (Exception ex)
            {
                throw new Exception($"发送文件夹失败: {ex.Message}");
            }
        }

        private async Task SendPacketAsync(char type, byte[] data)
        {
            try
            {
                byte[] packet = new byte[1 + data.Length];
                packet[0] = (byte)type;
                Buffer.BlockCopy(data, 0, packet, 1, data.Length);

                await _client.SendAsync(
                    packet,
                    packet.Length,
                    new IPEndPoint(IPAddress.Parse(Ip), Port)
                );
                await Task.Delay(1); // 简单流控
            }
            catch (Exception ex)
            {
                throw new Exception($"发送数据包失败: {ex.Message}");
            }
        }
    }
}
