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
            var window = new ControllerManagerWindow { Owner = this };
            window.ShowDialog();
            LoadControllers();
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Close();

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

        private async void NewUser_Click(object sender, RoutedEventArgs e)
        {
            ControllerInfo? controller = GetSelectedController();

            if (controller == null)
            {
                MessageBox.Show("Выберите контроллер.");
                return;
            }

            try
            {
                StatusText.Text = $"Получение актуального списка с {controller.IpAddress}...";

                List<AccessControlCard> users = await _finder.GetAccessControlCardsAsync(
                    controller.IpAddress,
                    controller.Username,
                    controller.Password);

                _allUsers.Clear();

                foreach (AccessControlCard userItem in users)
                    _allUsers.Add(userItem);

                ApplyFilter();

                int lastUserId = GetLastUserId();
                string lastCardNumber = GetLastCardNumber();

                var user = new AccessUser
                {
                    IsValid = true,
                    ValidFrom = DateTime.Today,
                    ValidTo = DateTime.Today.AddYears(10)
                };

                var window = new UserEditorWindow(user, lastUserId, lastCardNumber)
                {
                    Owner = this
                };

                if (window.ShowDialog() != true)
                {
                    StatusText.Text = "Создание пользователя отменено.";
                    return;
                }

                StatusText.Text = $"Создание пользователя UserID={window.User.UserId}...";

                bool created = await _userService.CreateUserAsync(
                    controller.IpAddress,
                    controller.Username,
                    controller.Password,
                    window.User);

                if (!created)
                {
                    MessageBox.Show(
                        "Контроллер не подтвердил создание пользователя.",
                        "Создание пользователя",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    StatusText.Text = "Пользователь не создан.";
                    return;
                }

                await RefreshUsersAsync();

                SelectUserById(window.User.UserId);

                MessageBox.Show(
                    $"Пользователь создан.\n\nUserID: {window.User.UserId}\nИмя: {window.User.FullName}",
                    "Создание пользователя",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                StatusText.Text = $"Пользователь UserID={window.User.UserId} создан.";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Ошибка создания пользователя.";
                MessageBox.Show(ex.ToString(), "Ошибка создания пользователя");
            }
        }

        private void EditUser_Click(object sender, RoutedEventArgs e)
        {
            if (UsersGrid.SelectedItem is not AccessControlCard selected)
            {
                MessageBox.Show("Выберите пользователя.");
                return;
            }

            var user = new AccessUser
            {
                RecNo = selected.RecNo,
                UserId = selected.UserId,
                FullName = selected.CardName,
                CardNumber = selected.CardNo,
                CardStatus = selected.CardStatus.ToString(),
                IsValid = selected.IsValid,
                ValidFrom = ParseDate(selected.ValidDateStart),
                ValidTo = ParseDate(selected.ValidDateEnd)
            };

            var window = new UserEditorWindow(user, GetLastUserId(), GetLastCardNumber())
            {
                Owner = this
            };

            if (window.ShowDialog() == true)
            {
                MessageBox.Show(
                    $"Изменения подготовлены.\n\nUserID: {window.User.UserId}\nИмя: {window.User.FullName}",
                    "Изменение пользователя");

                StatusText.Text = $"Подготовлено изменение UserID={window.User.UserId}";
            }
        }

        private void UserPhoto_Click(object sender, RoutedEventArgs e)
        {
            if (UsersGrid.SelectedItem is not AccessControlCard selected)
            {
                MessageBox.Show("Выберите пользователя.");
                return;
            }

            MessageBox.Show(
                $"Работа с фото будет подключена следующим Build.\n\nUserID: {selected.UserId}\nИмя: {selected.CardName}",
                "Фото пользователя");
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
                $"Удалить пользователя с текущего контроллера?\n\nКонтроллер: {controller.IpAddress}\nUserID: {selected.UserId}\nИмя: {selected.CardName}",
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
                $"Удалить пользователя СО ВСЕХ контроллеров в списке?\n\nUserID: {selected.UserId}\nИмя: {selected.CardName}",
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

            MessageBox.Show(string.Join(Environment.NewLine, results), "Результат удаления");

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

                List<AccessControlCard> users = await _finder.GetAccessControlCardsAsync(
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

        private int GetLastUserId()
        {
            int max = 0;

            foreach (AccessControlCard user in _allUsers)
            {
                if (int.TryParse(user.UserId, out int id) && id > max)
                    max = id;
            }

            return max;
        }

        private string GetLastCardNumber()
        {
            long max = 0;

            foreach (AccessControlCard user in _allUsers)
            {
                if (long.TryParse(user.CardNo, out long cardNo) && cardNo > max)
                    max = cardNo;
            }

            return max > 0 ? max.ToString() : "";
        }

        private void SelectUserById(string userId)
        {
            AccessControlCard? user = _visibleUsers.FirstOrDefault(x =>
                x.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase));

            if (user == null)
                return;

            UsersGrid.SelectedItem = user;
            UsersGrid.ScrollIntoView(user);
        }

        private static DateTime? ParseDate(string value)
        {
            return DateTime.TryParse(value, out DateTime result)
                ? result
                : null;
        }
    }
}