using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace KlientSerwer
{
    public partial class Client : Window
    {
        private TcpClient client; // Klient TCP
        private StreamWriter writer; // Writer do wysylania danych do serwera
        private StreamReader reader; // Reader do odbierania danych z serwera
        private string username; // Nazwa uzytkownika

        public Client(TcpClient client, string username)
        {
            
            InitializeComponent();
            this.client = client;
            this.writer = new StreamWriter(this.client.GetStream()) { AutoFlush = true }; // Inicjalizacja writera
            this.reader = new StreamReader(this.client.GetStream()); // Inicjalizacja readera
            this.username = username; // Inicjalizacja nazwy użytkownika
            SendUsernameToServer(); // Wysłanie nazwy uzytkownika do serwera
            this.Title = $"Klient - {username}"; // Zmiana tytulu okienka klienta
            Task.Run(ListenForResults);  // Uruchomienie watku nasluchujacego wyniki z serwera

        this.Closed += Client_Closed;
    }

    private async void Client_Closed(object sender, EventArgs e)
    {
        // Powiadomienie serwera o rozłączeniu klienta
        try
        {
            await this.writer.WriteLineAsync("CLIENT_DISCONNECTED");
            await this.writer.FlushAsync();
        }
        catch (Exception ex)
        {
            // Obsługa błędów
            Console.WriteLine($"Błąd przy wysyłaniu informacji o rozłączeniu: {ex.Message}");
        }
        finally
        {
            // Zamknięcie strumieni i klienta
            this.writer.Close();
            this.reader.Close();
            this.client.Close();
        }
    }

        private async void SendUsernameToServer()
        {
            await writer.WriteLineAsync($"USERNAME:{username}");
        }

        // Obsluga klikniecia przycisku dla wyboru jednej z trzech opcji
        private async void ChoiceButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            string choice = button.Content.ToString();
            await SendChoiceToServer(choice); // Wyslanie wyboru do serwera
            ToggleChoiceButtons(false); // Wylaczenie przyciskow wyboru
        }

        // Obsluga klikniecia przycisku resetowania
        private async void Reset_Click(object sender, RoutedEventArgs e)
        {
            await SendChoiceToServer("RESET_REQUEST"); // Wyslanie informacji o resetowaniu do serwera
        }

        private async void ApproveResetButton_Click(object sender, RoutedEventArgs e)
        {
            await SendChoiceToServer("RESET_APPROVE");
            ToggleResetApprovalButtons(false); // Wylaczenie przyciskow odrzucenia/zgody na reset
        }

        private async void DenyResetButton_Click(object sender, RoutedEventArgs e)
        {
            await SendChoiceToServer("RESET_DENY");
            ToggleResetApprovalButtons(false);
        }

        // Metoda wysylajaca wybor do serwera
        private async Task SendChoiceToServer(string choice)
        {
            await this.writer.WriteLineAsync(choice); // Wyslanie wyboru do serwera
            await this.writer.FlushAsync();
        }

        // Metoda nasluchujaca wyniki z serwera
        private async Task ListenForResults()
        {
            try
            {
                while (true)
                {
                    // Odczytanie wiadomosci z serwera, ReadLineAsync czyta "linijka po linijce"
                    string message = await this.reader.ReadLineAsync();
                    if (message.StartsWith("RESET_REQUEST"))
                    {
                        Dispatcher.Invoke(() =>
                        {
                            // Wyswietlenie wiadomosci o probie resetu
                            ResultLabel.Content = "Drugi gracz chce zresetować grę.";
                            ToggleResetApprovalButtons(true); // Wlaczenie przyciskow odmowy/zgody na reset
                        });
                    }

                    else if (message == "RESET_DENIED")
                    {
                        Dispatcher.Invoke(() => ResultLabel.Content = "Odmowa restartu.");
                    }
                    else if (message.StartsWith("PLAYER_COUNT"))
                    {
                        int playerCount = int.Parse(message.Split(':')[1]);
                        Dispatcher.Invoke(() =>
                        {
                            if (playerCount < 2)
                            {
                                ResultLabel.Content = "Jestes sam. Brak drugiego gracza.";
                            }
                            else
                            {
                                ResultLabel.Content = "Drugi gracz jest podłączony.";
                            }
                        });
                    }
                    else if (message.StartsWith("RESULT"))
                    {
                        string result = message.Substring(7); // Usunięcie "RESULT:"

                        // Wyswietlenie wyniku gry oraz wlaczenie przyciskow odmowy/zgody na reset

                        Dispatcher.Invoke(() =>
                        {
                            ResultLabel.Content = result;
                            ToggleChoiceButtons(true);
                        });

                        // Odczytanie wynikow graczy, kolejno druga i trzecia linijka, nastepnie nastepuje ich wyswietlenie
                        string player1Score = await this.reader.ReadLineAsync();
                        string player2Score = await this.reader.ReadLineAsync();
                        Dispatcher.Invoke(() =>
                        {
                            Player1Result.Content = player1Score;
                            Player2Result.Content = player2Score;
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => ResultLabel.Content = $"Error receiving results: {ex.Message}");
            }
        }

        // Metoda resetujaca labele z wynikami
       

        // Metoda zmieniajaca widocznosc przyciskow do odrzucenia/akceptacji resetu
        private void ToggleResetApprovalButtons(bool show)
        {
            ApproveResetButton.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            DenyResetButton.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        // Wlaczenie/wylaczenie przyciskow do grania
        private void ToggleChoiceButtons(bool enable)
        {
            rockButton.IsEnabled = enable;
            paperButton.IsEnabled = enable;
            scissorsButton.IsEnabled = enable;
        }
    }
}
