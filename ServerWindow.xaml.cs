using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Sockets.TcpListener;
using static System.Net.Sockets.TcpClient;
using System.Threading;
using System.Windows.Threading;
using System.IO;

namespace KlientSerwer
{
    /// <summary>
    /// Logika interakcji dla klasy ServerWindow.xaml
    /// </summary>
    public partial class ServerWindow : Window
    {
        private TcpListener tcpListener;
        private List<TcpClient> clients;
        private Thread serverThread;
        private bool isRunning;
        private Dictionary<TcpClient, string> clientChoices;

        public ServerWindow(string AddressIP, int Port)
        {
            InitializeComponent();
            tcpListener = new TcpListener(System.Net.IPAddress.Parse(AddressIP), Port);
            clients = new List<TcpClient>();
            clientChoices = new Dictionary<TcpClient, string>();
            tcpListener.Start();
            StartServer();
            LogMessage($"Serwer wystartował na porcie {Port} \n");
        }


        public void ServerListen()
        {
            try
            {
                while (isRunning)
                {
                    TcpClient tcpClient = tcpListener.AcceptTcpClient();
                    if (tcpClient != null)
                    {
                        clients.Add(tcpClient);
                        LogMessage($"{tcpClient.Client.RemoteEndPoint} has connected");
                        HandleClient(tcpClient);
                    }
                }
            }
            catch (SocketException ex)
            {
                LogMessage($"SocketException: {ex.Message}");
            }
            catch (Exception ex)
            {
                LogMessage($"Exception: {ex.Message}");
            }
        }
        private void HandleClient(TcpClient tcpClient)
        {
            var clientThread = new Thread(async () =>
            {
                var reader = new StreamReader(tcpClient.GetStream());
                while (tcpClient.Connected)
                {
                    string choice = await reader.ReadLineAsync();
                    if (!string.IsNullOrEmpty(choice))
                    {
                        lock (clientChoices)
                        {
                            clientChoices[tcpClient] = choice;
                            if (clientChoices.Count == 2)
                            {
                                DetermineWinner();
                            }
                        }
                    }
                }
            })
            { IsBackground = true };
            clientThread.Start();
        }

        private void DetermineWinner()
        {
            var choices = new List<string>(clientChoices.Values);
            string result;

            if (choices[0] == choices[1])
            {
                result = "It's a draw!";
            }
            else if ((choices[0] == "Rock" && choices[1] == "Scissors") ||
                     (choices[0] == "Scissors" && choices[1] == "Paper") ||
                     (choices[0] == "Paper" && choices[1] == "Rock"))
            {
                result = "Player 1 wins!";
            }
            else
            {
                result = "Player 2 wins!";
            }

            BroadcastResult(result);
            clientChoices.Clear();
        }

        private void BroadcastResult(string result)
        {
            foreach (var client in clients)
            {
                var writer = new StreamWriter(client.GetStream()) { AutoFlush = true };
                writer.WriteLine(result);
            }

            Dispatcher.Invoke(() => tbMessage.Text += result + "\n");
        }

        public void StartServer()
        {
            isRunning = true;
            serverThread = new Thread(ServerListen) { IsBackground = true };
            serverThread.Start();
        }

        public void StopServer() { 
            isRunning = false;
            tcpListener.Stop();
            serverThread.Join();
        }

        public void LogMessage(string mes)
        {
            Dispatcher.Invoke(() =>
            {
                this.tbMessage.Text += mes;
            });
        }

    }
}
