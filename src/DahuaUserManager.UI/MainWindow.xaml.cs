using System.IO;
using System.Windows;
using DahuaUserManager.Api.Clients;

namespace DahuaUserManager.UI
{
    public partial class MainWindow : Window
    {
        private readonly RecordFinderClient _finder = new();
        private readonly DahuaClient _client = new();

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

            var log = new List<string>();

            try
            {
                AccessControlCard? before = await _finder.FindCardByUserIdAsync(
                    ip, username, password, userId);

                if (before == null)
                {
                    MessageBox.Show($"UserID={userId} уже не найден.");
                    return;
                }

                log.Add("===== BEFORE =====");
                log.Add($"UserID={before.UserId}");
                log.Add($"RecNo={before.RecNo}");
                log.Add($"Name={before.CardName}");
                log.Add("");

                string deletePath =
                    $"/cgi-bin/recordUpdater.cgi?action=remove&name=AccessControlCard&recno={before.RecNo}";

                log.Add("===== DELETE REQUEST =====");
                log.Add(deletePath);
                log.Add("");

                string deleteResult = await _client.ExecuteAuthenticatedGetAsync(
                    ip, username, password, deletePath);

                log.Add("===== DELETE RESPONSE BODY =====");
                log.Add(deleteResult);
                log.Add("");

                AccessControlCard? after = await _finder.FindCardByUserIdAsync(
                    ip, username, password, userId);

                log.Add("===== AFTER =====");

                if (after == null)
                {
                    log.Add("Пользователь больше не найден.");
                }
                else
                {
                    log.Add($"UserID={after.UserId}");
                    log.Add($"RecNo={after.RecNo}");
                    log.Add($"Name={after.CardName}");
                }

                string fileName = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "delete_test.txt");

                File.WriteAllText(fileName, string.Join(Environment.NewLine, log));

                MessageBox.Show(
                    $"Тест удаления выполнен.\n\nФайл:\n{fileName}",
                    "Delete test",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                string fileName = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "delete_test_error.txt");

                File.WriteAllText(fileName, ex.ToString());

                MessageBox.Show(
                    $"Ошибка. Лог:\n{fileName}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}