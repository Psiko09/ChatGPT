using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace MyServer
{
    public partial class Form1 : Form
    {
        public class Client
        {
            public TcpClient tcp_client { get; set; }
            public NetworkStream client_stream { get; set; }
            public string username { get; set; }
            public override string ToString()
            {
                return username;
            }
        }

        private TcpListener listener;
        private IPAddress IP;
        private int PORT;
        int Buffer_Size = 4096;
        byte[] buffer;
        List<Client> client_list;

        public Form1()
        {
            InitializeComponent();
            IP = IPAddress.Any;
            PORT = 9049;
            listener = new TcpListener(IP, PORT);
            client_list = new List<Client>();

            Start();
        }

        public void Start()
        {
            listener.Start();

            Log("Server başlatıldı.");

            listener.BeginAcceptTcpClient(AcceptClientCallback, null);
        }

        private void AcceptClientCallback(IAsyncResult asyncResult)
        {
            TcpClient client = listener.EndAcceptTcpClient(asyncResult);
            client.ReceiveBufferSize = Buffer_Size;
            client.SendBufferSize = Buffer_Size;
            buffer = new byte[Buffer_Size];

            Client connectedClient = new Client();
            connectedClient.tcp_client = client;
            connectedClient.client_stream = client.GetStream();
            listener.BeginAcceptTcpClient(AcceptClientCallback, null);
            connectedClient.client_stream.BeginRead(buffer, 0, buffer.Length, new AsyncCallback(ReceiveCallBack), connectedClient);
        }

        private void ReceiveCallBack(IAsyncResult asyncResult)
        {
            Client connectedClient = (Client)asyncResult.AsyncState;
            try
            {
                int dataLength = connectedClient.client_stream.EndRead(asyncResult);

                if (dataLength == 0)
                {
                    RemoveClient(connectedClient);
                    return;
                }

                byte[] data = new byte[dataLength];
                Array.Copy(buffer, data, data.Length);

                string received = Encoding.UTF8.GetString(data);

                // DataController fonksiyonu içerisinde gelen mesaja göre nasıl tepki vermemizi gerektiren kodlar mevcut.
                DataController(connectedClient, received);

                connectedClient.client_stream.BeginRead(buffer, 0, buffer.Length, new AsyncCallback(ReceiveCallBack), connectedClient);
            }
            catch (IOException)
            {
                Log($"Client ayrıldı: {connectedClient.username}");
                RemoveClient(connectedClient);
            }
        }

        private void DataController(Client client, string syntax)
        {
           // Client tarafından gelen mesajlar burda kontrol ediliyor ve ona göre işlem yapılıyor

            // Client gelen verileri '£' karakteri ile ayırıyor bizde ondan dolayı split metodu ile parçalara ayırıyoruz.
            string[] data = syntax.Split('£');

            // Client ilk bağlandığı zaman "connected£kullanıcı_adı" diye bir mesaj gönderir.
            if (data[0] == "connected")
            {
                client.username = data[1];  // Client kullanıcı adını £ den sonra yazar bundan dolayı split çıktısının 1. indexi clientın kullanıcı adıdır.

                AddListBox(client); // ListBox içerisine clienti ekliyorum.
            }
            else if (data[0] == "my-machine-name")
            {
                Log($"{client.username}, Makine adını gönderdi: {data[1]}");
            }
        }

        public void SendMessageToClient(Client client, string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            client.client_stream.Write(data, 0, data.Length);
        }

        private void AddListBox(Client item)
        {
            if (InvokeRequired)
                Invoke(new Action<Client>(AddListBox), item);

            else
                listBoxClients.Items.Add(item);
        }

        private void RemoveClient(Client item)
        {
            item.tcp_client.Close();
            client_list.Remove(item);
            if (InvokeRequired)
                Invoke(new Action<Client>(RemoveClient), item);

            else
                listBoxClients.Items.Remove(item);
        }

        private void Log(string text)
        {
            if (InvokeRequired)
                Invoke(new Action<string>(Log), $"{text}\n");

            else
                richTextBoxLogs.AppendText($"{text}\n");
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (txtBoxMessage.Text.Length > 0)
            {
                Client selected_client = (Client)listBoxClients.SelectedItem;
                SendMessageToClient(selected_client, txtBoxMessage.Text);
            }
        }
    }
}
