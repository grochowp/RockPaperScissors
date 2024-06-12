using System;
using System.Collections.Generic;

using System.Net.Sockets;

using System.Threading;

using System.Windows;

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
        private int player1Wins = 0;
        private int player2Wins = 0;

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
                        LogMessage($"{tcpClient.Client.RemoteEndPoint} się połączył \n");
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

        public void StopServer()
        {
            isRunning = false;
            tcpListener.Stop();
            serverThread.Join();
        }

        public void StartServer()
        {
            isRunning = true;
            serverThread = new Thread(ServerListen) { IsBackground = true };
            serverThread.Start();
        }

        private void HandleClient(TcpClient tcpClient)
        {
            Thread clientThread = new Thread(async () =>
            {
                var reader = new StreamReader(tcpClient.GetStream());
                while (tcpClient.Connected)
                {
                    string choice = await reader.ReadLineAsync();
                    if (!string.IsNullOrEmpty(choice))
                    {
                        if (choice == "RESET_REQUEST")
                        {
                            BroadcastMessage("RESET_REQUEST", tcpClient);
                        }
                        else if (choice == "RESET_APPROVE")
                        {
                            ResetScores();
                            BroadcastMessage("RESET_APPROVED", null);
                        }
                        else if (choice == "RESET_DENY")
                        {
                            BroadcastMessage("RESET_DENIED", null);
                        }
                        else
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
                }
            })
            { IsBackground = true };
            clientThread.Start();
        }

        private void DetermineWinner()
        {
            List<string> choices = new List<string>(clientChoices.Values);
            string result;

            if (choices[0] == choices[1])
            {
                result = "Remis!";
            }
            else if ((choices[0] == "Kamień" && choices[1] == "Nożyce") ||
                     (choices[0] == "Nożyce" && choices[1] == "Papier") ||
                     (choices[0] == "Papier" && choices[1] == "Kamień"))
            {
                result = "Gracz 1 wygrywa!";
                player1Wins++;
            }
            else
            {
                result = "Gracz 2 wygrywa!";
                player2Wins++;
            }

            BroadcastResult(result);
            clientChoices.Clear();
        }

        private void ResetScores()
        {
            player1Wins = 0;
            player2Wins = 0;
            BroadcastResult("Gra została zresetowana");
        }


        private void BroadcastResult(string result)
        {
            foreach (var client in clients)
            {
                StreamWriter writer = new StreamWriter(client.GetStream()) { AutoFlush = true };
                writer.WriteLine(result);
                writer.WriteLine($"Gracz 1 - {player1Wins}");
                writer.WriteLine($"Gracz 2 - {player2Wins}");
            }

            Dispatcher.Invoke(() => tbMessage.Text += result + "\n");
        }

        private void BroadcastMessage(string message, TcpClient excludeClient)
        {
            foreach (var client in clients)
            {
                if (client != excludeClient)
                {
                    StreamWriter writer = new StreamWriter(client.GetStream()) { AutoFlush = true };
                    writer.WriteLine(message);
                }
            }
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
