using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using DahuaUserManager.Api.Clients;
using DahuaUserManager.Core.Managers;
using DahuaUserManager.Core.Services;
using DahuaUserManager.Models.Entities;
using DahuaUserManager.UI.Windows;

namespace DahuaUserManager.UI
{
    public partial class MainWindow : Window
    {
        private readonly RecordFinderClient _finder = new();
        private readonly UserService _userService = new();
        private readonly ControllerManager _controllerManager = new();

        private readonly ObservableCollection<ControllerInfo> _controllers = new();
        private readonly ObservableCollection<AccessControlCard> _allUsers = new();
        private readonly ObservableCollection<AccessControlCard> _visibleUsers = new();

        private bool _isLoadingControllers;

        public MainWindow()
        {
            InitializeComponent();

            ControllersList.ItemsSource = _controllers;
            UsersGrid.ItemsSource = _visibleUsers;

            LoadControllers();
        }

        private void OpenControllerManager_Click(object sender, RoutedEventArgs e)
        {
            var window = new ControllerManagerWindow
            {
                Owner = this
            };

            window.ShowDialog();

            LoadControllers();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void LoadControllers()
        {
            _isLoadingControllers = true;

            _controllerManager.Load();

            _controllers.Clear();

            foreach (var controller in _controllerManager.Controllers)
                _controllers.Add(controller);

            if (_controllers.Count > 0)
                ControllersList.SelectedIndex = 0;

            _isLoadingControllers = false;
        }

        private ControllerInfo? GetSelectedController()
        {
            return ControllersList.SelectedItem as ControllerInfo;
        }

        private void AddController_Click(object sender, RoutedEventArgs e)
        {
            string ip = NewControllerIpBox.Text.Trim();

            bool added = _controllerManager.AddController(ip);

            if (!added)
            {
                MessageBox.Show("Контроллер не добавлен. Проверьте IP или дубликат.");
                return;
            }

            NewControllerIpBox.Text = "192.168.0.";
            LoadControllers();

            StatusText.Text = $"Контроллер {ip} добавлен и сохранён.";
        }

        private async void RefreshUsers_Click(object sender, RoutedEventArgs e)
        {
            await RefreshUsersAsync();
        }

        private async void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            ControllerInfo? controller = GetSelectedController();

            if (controller == null)
            {
                MessageBox.Show("Выберите контроллер.");
                return;
            }

            if (UsersGrid.SelectedItem is not AccessControlCard selected)
            {
                MessageBox.Show("Выберите пользователя.");
                return;
            }

            var confirm = MessageBox.Show(
                $"Удалить пользователя с текущего контроллера?\n\n" +
                $"Контроллер: {controller.IpAddress}\n" +
                $"UserID: {selected.UserId}\n" +
                $"Имя: {selected.CardName}",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            bool deleted = await _userService.DeleteUserCompletelyAsync(
                controller.IpAddress,
                controller.Username,
                controller.Password,
                selected.UserId);

            MessageBox.Show(deleted ? "Удалён." : "Не найден или не удалён.");

            await RefreshUsersAsync();
        }

        private async void DeleteSelectedFromAll_Click(object sender, RoutedEventArgs e)
        {
            if (UsersGrid.SelectedItem is not AccessControlCard selected)
            {
                MessageBox.Show("Выберите пользователя.");
                return;
            }

            var confirm = MessageBox.Show(
                $"Удалить пользователя СО ВСЕХ контроллеров в списке?\n\n" +
                $"UserID: {selected.UserId}\n" +
                $"Имя: {selected.CardName}",
                "Подтверждение удаления со всех",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            var results = new List<string>();

            foreach (ControllerInfo controller in _controllers)
            {
                try
                {
                    bool deleted = await _userService.DeleteUserCompletelyAsync(
                        controller.IpAddress,
                        controller.Username,
                        controller.Password,
                        selected.UserId);

                    results.Add($"{controller.IpAddress}: {(deleted ? "удалён" : "не найден / не удалён")}");
                }
                catch (Exception ex)
                {
                    results.Add($"{controller.IpAddress}: ошибка - {ex.Message}");
                }
            }

            MessageBox.Show(
                string.Join(Environment.NewLine, results),
                "Результат удаления");

            await RefreshUsersAsync();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private async void ControllersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingControllers)
                return;

            ControllerInfo? controller = GetSelectedController();

            if (controller == null)
                return;

            HeaderText.Text = $"Пользователи контроллера {controller.IpAddress}";

            await RefreshUsersAsync();
        }

        private async Task RefreshUsersAsync()
        {
            try
            {
                ControllerInfo? controller = GetSelectedController();

                if (controller == null)
                {
                    MessageBox.Show("Выберите контроллер.");
                    return;
                }

                StatusText.Text = $"Загрузка пользователей с {controller.IpAddress}...";
                HeaderText.Text = $"Пользователи контроллера {controller.IpAddress}";

                List<AccessControlCard> users =
                    await _finder.GetAccessControlCardsAsync(
                        controller.IpAddress,
                        controller.Username,
                        controller.Password);

                _allUsers.Clear();

                foreach (var user in users)
                    _allUsers.Add(user);

                ApplyFilter();

                StatusText.Text = $"Готово. Загружено: {_allUsers.Count}";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Ошибка загрузки";
                MessageBox.Show(ex.ToString(), "Ошибка");
            }
        }

        private void ApplyFilter()
        {
            string search = SearchBox?.Text?.Trim() ?? "";

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