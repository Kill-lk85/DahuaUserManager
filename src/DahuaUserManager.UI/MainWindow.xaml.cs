using System.Windows;
using DahuaUserManager.Api.Clients;

namespace DahuaUserManager.UI
{
    public partial class MainWindow : Window
    {
        private readonly RecordFinderClient _finder = new();

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void CheckController_Click(object sender, RoutedEventArgs e)
        {
            const string ip = "192.168.0.250";
            const string username = "admin";
            const string password = "Admin123!";
            const string userId = "1";

            try
            {
                AccessControlCard? card = await _finder.FindCardByUserIdAsync(
                    ip,
                    username,
                    password,
                    userId);

                if (card == null)
                {
                    MessageBox.Show($"UserID={userId} не найден.");
                    return;
                }

                MessageBoxResult confirm = MessageBox.Show(
                    $"Удалить пользователя?\n\n" +
                    $"UserID: {card.UserId}\n" +
                    $"RecNo: {card.RecNo}\n" +
                    $"Имя: {card.CardName}",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirm != MessageBoxResult.Yes)
                    return;

                bool deleted = await _finder.DeleteCardByUserIdAsync(
                    ip,
                    username,
                    password,
                    userId);

                MessageBox.Show(
                    deleted
                        ? $"UserID={userId} удалён."
                        : $"UserID={userId} не удалён или всё ещё найден.",
                    "Результат",
                    MessageBoxButton.OK,
                    deleted ? MessageBoxImage.Information : MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Ошибка");
            }
        }
    }
}