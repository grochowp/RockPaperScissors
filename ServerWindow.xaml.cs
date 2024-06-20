using System;
using System.Collections.Generic;

using System.Net.Sockets;

using System.Threading;

using System.Windows;

using System.Windows.Threading;
using System.IO;
using System.Net.Http;
using System.Linq;

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
        private Dictionary<TcpClient, string> clientUsernames;
        private Dictionary<TcpClient, string> clientChoices;
        private Dictionary<TcpClient, int> playerWins; 


        public ServerWindow(string AddressIP, int Port)
        {
            InitializeComponent();
            tcpListener = new TcpListener(System.Net.IPAddress.Parse(AddressIP), Port);
            clients = new List<TcpClient>();
            clientChoices = new Dictionary<TcpClient, string>();
            clientUsernames = new Dictionary<TcpClient, string>();
            playerWins = new Dictionary<TcpClient, int>();
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
                        lock (clients)
                        {
                            if (clients.Count >= 2)
                            {
                                // Odmów połączenia gdy osiągnięto maksymalną liczbę klientów
                                StreamWriter writer = new StreamWriter(tcpClient.GetStream()) { AutoFlush = true };
                                writer.WriteLine("SERVER_FULL");
                                tcpClient.Close();
                            }
                            else
                            {
                                clients.Add(tcpClient);
                                LogMessage($"{tcpClient.Client.RemoteEndPoint} się połączył \n");
                                HandleClient(tcpClient);
                                NotifyPlayerCount();
                            }
                        }
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

        private void NotifyPlayerCount()
        {
            foreach (var client in clients)
            {
                StreamWriter writer = new StreamWriter(client.GetStream()) { AutoFlush = true };
                writer.WriteLine($"PLAYER_COUNT:{clients.Count}");

                if (clients.Count < 2)
                {
                    writer.WriteLine("Jestes sam. Brak drugiego gracza.");
                }
                else
                {
                    string player1 = clientUsernames.ContainsKey(clients[0]) ? clientUsernames[clients[0]] : "Gracz 1";
                    string player2 = clientUsernames.ContainsKey(clients[1]) ? clientUsernames[clients[1]] : "Gracz 2";
                    writer.WriteLine($"USERNAME: {player1} vs {player2}");
                }
            }
        }

        private void HandleClient(TcpClient tcpClient)
        {
            Thread clientThread = new Thread(async () =>
            {
                var reader = new StreamReader(tcpClient.GetStream());
                while (tcpClient.Connected)
                {
                    try
                    {
                        string message = await reader.ReadLineAsync();

                        if (!string.IsNullOrEmpty(message))
                        {
                            if (message.StartsWith("USERNAME"))
                            {
                                string username = message.Substring(9);
                                lock (clientUsernames)
                                {
                                    clientUsernames[tcpClient] = username;
                                }
                                NotifyPlayerCount();
                                LogMessage($"{username} się połączył \n");
                            }
                            else if (message == "RESET_REQUEST")
                            {
                                BroadcastMessage("RESET_REQUEST", tcpClient);
                            }
                            else if (message == "RESET_APPROVE")
                            {
                                ResetScores();
                                BroadcastMessage("RESET_APPROVED", null);
                            }
                            else if (message == "RESET_DENY")
                            {
                                BroadcastMessage("RESET_DENIED", null);
                            }
                            else if (message == "CLIENT_DISCONNECTED")
                            {
                                // Obsługa rozłączenia klienta
                                clients.Remove(tcpClient);
                                LogMessage($"{tcpClient.Client.RemoteEndPoint} rozłączył się \n");
                                break;
                            }
                            else
                            {
                                lock (clientChoices)
                                {
                                    clientChoices[tcpClient] = message;
                                    if (clientChoices.Count == 2)
                                    {
                                        DetermineWinner();
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        break; // W przypadku błędu wyjdź z pętli
                    }
                }

                lock (clients)
                {
                    clients.Remove(tcpClient);
                    clientChoices.Remove(tcpClient);
                    clientUsernames.Remove(tcpClient);
                    NotifyPlayerCount(); // Informuj klientów o liczbie połączeń po rozłączeniu
                }

                LogMessage($"{tcpClient.Client.RemoteEndPoint} się rozłączył \n");
            })
            { IsBackground = true };
            clientThread.Start();
        }

        private void DetermineWinner()
        {
            if (clientChoices.Count != 2)
            {
                // Nie można określić wyniku, jeśli nie ma dwóch wyborów
                return;
            }

            TcpClient[] clients = clientChoices.Keys.ToArray();
            string choice1 = clientChoices[clients[0]];
            string choice2 = clientChoices[clients[1]];
            string player1Name = clientUsernames.ContainsKey(clients[0]) ? clientUsernames[clients[0]] : "Gracz 1";
            string player2Name = clientUsernames.ContainsKey(clients[1]) ? clientUsernames[clients[1]] : "Gracz 2";
            string result;

            if (choice1 == choice2)
            {
                result = "Remis!";
            }
            else if ((choice1 == "Kamień" && choice2 == "Nożyce") ||
                     (choice1 == "Nożyce" && choice2 == "Papier") ||
                     (choice1 == "Papier" && choice2 == "Kamień"))
            {
                result = $"{player1Name} wygrywa!";
                if (!playerWins.ContainsKey(clients[0]))
                {
                    playerWins.Add(clients[0], 0); // Inicjalizacja wygranych dla nowego klienta
                }
                playerWins[clients[0]]++;
            }
            else
            {
                result = $"{player2Name} wygrywa!";
                if (!playerWins.ContainsKey(clients[1]))
                {
                    playerWins.Add(clients[1], 0); // Inicjalizacja wygranych dla nowego klienta
                }
                playerWins[clients[1]]++;
            }

            BroadcastResult(result);
            clientChoices.Clear();
        }

        private void ResetScores()
        {
            foreach (var client in playerWins.Keys.ToList())
            {
                playerWins[client] = 0;
            }
            BroadcastResult("Gra została zresetowana");
        }


        private void BroadcastResult(string result)
        {
            string player1 = clientUsernames.ContainsKey(clients[0]) ? clientUsernames[clients[0]] : "Gracz 1";
            string player2 = clientUsernames.ContainsKey(clients[1]) ? clientUsernames[clients[1]] : "Gracz 2";
            int player1Result = playerWins.ContainsKey(clients[0]) ? playerWins[clients[0]] : 0;
            int player2Result = playerWins.ContainsKey(clients[1]) ? playerWins[clients[1]] : 0;
            foreach (var client in clients)
            {
                StreamWriter writer = new StreamWriter(client.GetStream()) { AutoFlush = true };
                writer.WriteLine($"RESULT:{result}");
                writer.WriteLine($"{player1} - {player1Result}");
                writer.WriteLine($"{player2} - {player2Result}");
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
