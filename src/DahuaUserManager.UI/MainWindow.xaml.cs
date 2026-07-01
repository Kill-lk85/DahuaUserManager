using System.IO;
using System.Windows;
using DahuaUserManager.Api.Clients;

namespace DahuaUserManager.UI
{
    public partial class MainWindow : Window
    {
        private readonly RawHttpClient _rawClient = new();
        private readonly DigestAuthenticator _digest = new();

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void CheckController_Click(object sender, RoutedEventArgs e)
        {
            const string ip = "192.168.0.250";
            const string username = "admin";
            const string password = "Admin123!";
            const string path = "/cgi-bin/magicBox.cgi?action=getSystemInfo";

            string firstResponse = await _rawClient.SendGetAsync(ip, path);

            string authLine = firstResponse
                .Replace("\r\n", "\n")
                .Split('\n')
                .FirstOrDefault(x => x.TrimStart().StartsWith("WWW-Authenticate:", StringComparison.OrdinalIgnoreCase))
                ?? "";

            if (string.IsNullOrWhiteSpace(authLine))
            {
                string file = SaveResponse("response.txt", firstResponse);
                MessageBox.Show($"WWW-Authenticate не найден.\nФайл:\n{file}");
                return;
            }

            string digestHeader = authLine
                .Substring(authLine.IndexOf(':') + 1)
                .Trim();

            DigestInfo info = _digest.Parse(digestHeader);

            string authorization = _digest.CreateAuthorizationHeader(
                username,
                password,
                "GET",
                path,
                info);

            var headers = new Dictionary<string, string>
            {
                ["Authorization"] = authorization
            };

            string secondResponse = await _rawClient.SendGetAsync(ip, path, headers);

            string fullLog =
                "===== FIRST RESPONSE =====\r\n" +
                firstResponse +
                "\r\n\r\n===== AUTHORIZATION =====\r\n" +
                authorization +
                "\r\n\r\n===== SECOND RESPONSE =====\r\n" +
                secondResponse;

            string fileName = SaveResponse("digest_test.txt", fullLog);

            MessageBox.Show(
                $"Готово. Лог сохранён:\n\n{fileName}",
                "Digest test",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private static string SaveResponse(string fileName, string text)
        {
            string fullPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                fileName);

            File.WriteAllText(fullPath, text);

            return fullPath;
        }
    }
}