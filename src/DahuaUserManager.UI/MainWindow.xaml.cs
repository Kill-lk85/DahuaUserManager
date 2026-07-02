using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using DahuaUserManager.Api.Clients;

namespace DahuaUserManager.UI
{
    public partial class MainWindow : Window
    {
        private readonly RecordFinderClient _finder = new();

        private readonly ObservableCollection<AccessControlCard> _allUsers = new();
        private readonly ObservableCollection<AccessControlCard> _visibleUsers = new();

        private const string Username = "admin";
        private const string Password = "Admin123!";

        public MainWindow()
        {
            InitializeComponent();
            UsersGrid.ItemsSource = _visibleUsers;
        }

        private string GetSelectedIp()
        {
            if (ControllersList.SelectedItem is ListBoxItem item &&
                item.Tag is string ip)
            {
                return ip;
            }

            return "192.168.0.250";
        }

        private async void RefreshUsers_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string ip = GetSelectedIp();

                StatusText.Text = $"Загрузка пользователей с {ip}...";
                HeaderText.Text = $"Пользователи контроллера {ip}";

                List<AccessControlCard> users =
                    await _finder.GetAccessControlCardsAsync(ip, Username, Password);

                _allUsers.Clear();

                foreach (var user in users)
                    _allUsers.Add(user);

                ApplyFilter();

                StatusText.Text = $"Готово. Загружено: {_allUsers.Count}";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Ошибка";
                MessageBox.Show(ex.ToString(), "Ошибка");
            }
        }

        private async void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (UsersGrid.SelectedItem is not AccessControlCard selected)
            {
                MessageBox.Show("Выберите пользователя.");
                return;
            }

            var confirm = MessageBox.Show(
                $"Удалить пользователя?\n\nUserID: {selected.UserId}\nИмя: {selected.CardName}\nRecNo: {selected.RecNo}",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                string ip = GetSelectedIp();

                bool deleted = await _finder.DeleteCardByUserIdAsync(
                    ip,
                    Username,
                    Password,
                    selected.UserId);

                MessageBox.Show(
                    deleted ? "Пользователь удалён." : "Пользователь не найден или не удалён.");

                await RefreshUsersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Ошибка удаления");
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void ControllersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string ip = GetSelectedIp();
            HeaderText.Text = $"Пользователи контроллера {ip}";
        }

        private async Task RefreshUsersAsync()
        {
            string ip = GetSelectedIp();

            List<AccessControlCard> users =
                await _finder.GetAccessControlCardsAsync(ip, Username, Password);

            _allUsers.Clear();

            foreach (var user in users)
                _allUsers.Add(user);

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            string search = SearchBox.Text.Trim();

            _visibleUsers.Clear();

            IEnumerable<AccessControlCard> filtered = _allUsers;

            if (!string.IsNullOrWhiteSpace(search))
            {
                filtered = filtered.Where(x =>
                    x.UserId.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    x.CardName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    x.CardNo.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var user in filtered)
                _visibleUsers.Add(user);

            CountText.Text = $"Записей: {_visibleUsers.Count}";
        }
    }
}