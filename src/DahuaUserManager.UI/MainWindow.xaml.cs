using System.Windows;
using DahuaUserManager.Api.Clients;

namespace DahuaUserManager.UI
{
    public partial class MainWindow : Window
    {
        private readonly DahuaClient _dahuaClient = new();

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void CheckController_Click(object sender, RoutedEventArgs e)
        {
            bool online = await _dahuaClient.IsOnlineAsync("192.168.0.250");

            MessageBox.Show(
                online ? "Контроллер доступен." : "Контроллер недоступен.",
                "Проверка контроллера",
                MessageBoxButton.OK,
                online ? MessageBoxImage.Information : MessageBoxImage.Error);
        }
    }
}