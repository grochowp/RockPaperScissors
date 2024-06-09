using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace KlientSerwer
{
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void StartServer_Click(object sender, RoutedEventArgs e)
        {

            int port = int.Parse(PortTextBox.Text);
            ServerWindow serverWindow = new ServerWindow("127.0.0.1", port);
            serverWindow.Show();

        }

        private void AddPlayer_Click(object sender, RoutedEventArgs e)
        {
            int port = int.Parse(PortPlayerTextBox.Text);
            string addressIP = IPTextBox.Text;
            TcpClient client = new TcpClient(addressIP, port);
            Client clientWindow = new Client(client);
            clientWindow.Show();
        }
    }
}
