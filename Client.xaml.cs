using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace KlientSerwer
{
    public partial class Client : Window
    {
        private TcpClient _client;
        private StreamWriter _writer;
        private StreamReader _reader;

        public Client(TcpClient client)
        {
            InitializeComponent();
            _client = client;
            _writer = new StreamWriter(_client.GetStream());
            _reader = new StreamReader(_client.GetStream());
        }

        private async void ChoiceButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            string choice = button.Content.ToString();
            await SendChoiceToServer(choice);
            string result = await _reader.ReadLineAsync();
            Dispatcher.Invoke(() => ResultLabel.Content = result);
        }

        private async Task SendChoiceToServer(string choice)
        {
            await _writer.WriteLineAsync(choice);
            await _writer.FlushAsync();
        }
    }
}
