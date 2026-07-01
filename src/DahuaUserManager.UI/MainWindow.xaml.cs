using System.Windows;
using System.Windows.Controls;
using DahuaUserManager.Api.Clients;

namespace DahuaUserManager.UI
{
    public partial class MainWindow : Window
    {
        private readonly RecordFinderClient _finder = new();

        private readonly List<ControllerInfo> _controllers =
        [
            new ControllerInfo { Name = "Контроллер 250", IpAddress = "192.168.0.250", Username = "admin", Password = "Admin123!" },
            new ControllerInfo { Name = "Контроллер 251", IpAddress = "192.168.0.251", Username = "admin", Password = "Admin123!" },
            new ControllerInfo { Name = "Контроллер 252", IpAddress = "192.168.0.252", Username = "admin", Password = "Admin123!" }
        ];

        private List<AccessControlCard> _allUsers = [];
        private bool _isLoaded;

        public MainWindow()
        {
            InitializeComponent();

            _isLoaded = true;
            ControllersList.SelectedIndex = 0;
            UpdateSelectedControllerUi();
        }

        private ControllerInfo GetSelectedController()
        {
            if (ControllersList.SelectedItem is ListBoxItem item &&
                item.Tag is string ip)
            {
                return _controllers.First(c => c.IpAddress == ip);
            }

            return _controllers[0];
        }

        private async void RefreshUsers_Click(object sender, RoutedEventArgs e)
        {
            await RefreshUsersAsync();
        }

        private async Task RefreshUsersAsync()
        {
            try
            {
                ControllerInfo controller = GetSelectedController();

                StatusText.Text = $"Загрузка {controller.IpAddress}...";

                _allUsers = await _finder.GetAccessControlCardsAsync(controller);

                ApplyFilter();

                StatusText.Text = $"Загружено: {_allUsers.Count}";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Ошибка";
                MessageBox.Show(ex.ToString(), "Ошибка");
            }
        }

        private async void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (UsersGrid.SelectedItem is not AccessControlCard card)
            {
                MessageBox.Show("Выберите пользователя.");
                return;
            }

            ControllerInfo controller = GetSelectedController();

            if (MessageBox.Show(
                $"Удалить пользователя с контроллера {controller.IpAddress}?\n\n" +
                $"Имя: {card.CardName}\n" +
                $"UserID: {card.UserId}\n" +
                $"RecNo: {card.RecNo}",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }

            bool deleted = await _finder.DeleteCardByUserIdAsync(controller, card.UserId);

            MessageBox.Show(deleted ? "Пользователь удалён." : "Удаление не удалось.");

            if (deleted)
                await RefreshUsersAsync();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isLoaded)
                return;

            ApplyFilter();
        }

        private void ControllersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded)
                return;

            UpdateSelectedControllerUi();
        }

        private void UpdateSelectedControllerUi()
        {
            ControllerInfo controller = GetSelectedController();

            HeaderText.Text = $"Пользователи контроллера {controller.IpAddress}";
            StatusText.Text = $"Выбран {controller.IpAddress}";

            _allUsers = [];
            UsersGrid.ItemsSource = _allUsers;
            CountText.Text = "Записей: 0";
        }

        private void ApplyFilter()
        {
            string search = SearchBox.Text.Trim();

            List<AccessControlCard> filtered = _allUsers;

            if (!string.IsNullOrWhiteSpace(search))
            {
                filtered = _allUsers
                    .Where(u =>
                        u.UserId.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        u.CardName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        u.CardNo.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            UsersGrid.ItemsSource = filtered;
            CountText.Text = $"Записей: {filtered.Count}";
        }
    }
}