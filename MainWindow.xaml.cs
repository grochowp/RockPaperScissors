using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace KlientSerwer
{
    public partial class MainWindow : Window
    {
        private ServerWindow serverWindow;

        public MainWindow()
        {
            InitializeComponent();
        }
        private int counter = 0;
        private async void StartServer_Click(object sender, RoutedEventArgs e)
        {

            int port = int.Parse(PortTextBox.Text);
            ServerWindow serverWindow = new ServerWindow("127.0.0.1", port);
            serverWindow.Show();

        }

        private async void AddPlayer_Click(object sender, RoutedEventArgs e)
        {
            int port = int.Parse(PortPlayerTextBox.Text);
            string addressIP = IPTextBox.Text;
            string username = NickTextBox.Text;

            TcpClient client = new TcpClient();
            try
            {
                await client.ConnectAsync(addressIP, port);
                // Połączenie udało się, sprawdź czy serwer jest pełny
                StreamReader reader = new StreamReader(client.GetStream());
                string response = await reader.ReadLineAsync();

                if (response == "SERVER_FULL")
                {
                    MessageBox.Show("Serwer jest pełny. Spróbuj ponownie później.");
                    client.Close(); // Zamknij połączenie z serwerem
                }
                else
                {
                    // Połączenie udało się i serwer nie jest pełny, otwórz okno klienta
                    Client clientWindow = new Client(client, username);
                    clientWindow.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas próby połączenia z serwerem: {ex.Message}");
                client.Close(); // Zamknij połączenie w przypadku błędu
            }
        }

    }
}
