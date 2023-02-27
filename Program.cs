using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace MyClient
{
    internal class Program
    {
        private static TcpClient tcp_client;
        private static NetworkStream stream;
        private static string IP;
        private static int PORT;
        private static byte[] buffer;
        private static int Buffer_Size = 4096;


        static void Main(string[] args)
        {
            IP = "127.0.0.1";
            PORT = 9049;
            tcp_client = new TcpClient();

            Connect();
            Console.Read();
        }

        private static bool isConnectedToServer()
        {
            // Bu fonksiyon ile Sunucya bağlı olup olmadığımı kontrol ediyorum.
            return tcp_client != null && tcp_client.Connected;
        }

        private static void Connect()
        {
            Thread.Sleep(3000);
            try
            {
                if (!isConnectedToServer())
                {
                    tcp_client = new TcpClient();
                    tcp_client.BeginConnect(IP, PORT, new AsyncCallback(ConnectCallBack), null);
                }
            }
            catch (SocketException)
            {
                // Sunucuya bağlantı kurulamaz ise tekrar bağlanmaya çalışıyorum
                tcp_client = null;
                Connect();
            }
        }

        private static void ConnectCallBack(IAsyncResult asyncResult)
        {
            try
            {
                tcp_client.EndConnect(asyncResult);
                stream = tcp_client.GetStream();
                buffer = new byte[Buffer_Size];

                Console.WriteLine("Bağlantı kuruldu.");

                // Sunucuya ilk mesajı yolluyorum. Sunucu bu mesajı kendi içerisinde bulunan DataController fonksiyonu ile çözüyor.
                string FirstMessage = $"connected£{Environment.UserName}";
                SendStringToServer(FirstMessage);

                stream.BeginRead(buffer, 0, buffer.Length, new AsyncCallback(ReceiveCallBack), null);
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"{ex.Message}");
                Connect();
            }
        }

        private static void ReceiveCallBack(IAsyncResult asyncResult)
        {
            try
            {
                int receivedDataLength = stream.EndRead(asyncResult);

                if (receivedDataLength == 0)
                    return;

                byte[] data = new byte[receivedDataLength];
                Array.Copy(buffer, data, data.Length);

                string receivedText = Encoding.UTF8.GetString(data);
                CommandController(receivedText);    // gelen mesajları bu fonksiyona gönderiyorum
                Console.WriteLine(receivedText);
                

                stream.BeginRead(buffer, 0, buffer.Length, new AsyncCallback(ReceiveCallBack), null);
            }
            catch (IOException)
            {
                Console.WriteLine("Bağlantı kesildi");
                Connect();
            }
        }

        private static void CommandController(string data)
        {
            // bu fonksiyon gelen mesajlardan sunucunun ne istediğini anlar ve ona göre tepki verir.
            string[] command = data.Split('£');
            if (command[0] == "get-machine-name")
            {
                SendStringToServer($"my-machine-name£{Environment.MachineName}");
            }
        }

        private static void WriteCallBack(IAsyncResult asyncResult)
        {
            stream.EndWrite(asyncResult);
        }

        private static void SendStringToServer(string text)
        {
            if (isConnectedToServer())
            {
                byte[] data = Encoding.UTF8.GetBytes(text);
                stream.BeginWrite(data, 0, data.Length, new AsyncCallback(WriteCallBack), null);
            }
        }

    }
}
