using DahuaUserManager.Core.Services;
using DahuaUserManager.Models.Entities;
using DahuaUserManager.UI.Database;
using DahuaUserManager.UI.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace DahuaUserManager.UI.Windows
{
    public partial class ControllerManagerWindow : Window
    {
        private readonly ControllerDetectionService _detector = new();

        private readonly AttendanceDatabase _database = new();

        private readonly ControllerRepository _repository;

        private readonly ObservableCollection<ControllerInfo>
            _controllers = new();


        public ControllerManagerWindow()
        {
            InitializeComponent();

            _repository =
                new ControllerRepository(_database);

            ControllersGrid.ItemsSource =
                _controllers;

            Loaded +=
                ControllerManagerWindow_Loaded;

            Closing +=
                ControllerManagerWindow_Closing;
        }


        private async void ControllerManagerWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            DataGridLayoutService.Load(
                ControllersGrid,
                "ControllersGrid");

            await LoadControllersAsync();
        }


        private void ControllerManagerWindow_Closing(
            object? sender,
            System.ComponentModel.CancelEventArgs e)
        {
            DataGridLayoutService.Save(
                ControllersGrid,
                "ControllersGrid");
        }


        /// <summary>
        /// Загружает контроллеры из текущей выбранной базы.
        /// </summary>
        private async Task LoadControllersAsync()
        {
            try
            {
                await _database.InitializeAsync();

                List<ControllerInfo> controllers =
                    await _repository.GetAllAsync();

                _controllers.Clear();

                foreach (ControllerInfo controller
                         in controllers)
                {
                    _controllers.Add(controller);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.ToString(),
                    "Ошибка загрузки контроллеров",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        private void Add_Click(
            object sender,
            RoutedEventArgs e)
        {
            var controller =
                new ControllerInfo
                {
                    Name = "Новый контроллер",

                    IpAddress =
                        "192.168.0.",

                    Username =
                        "admin",

                    Password =
                        "Admin123!",

                    UseByDefault =
                        true,

                    AttendanceRole =
                        AttendanceRole.None
                };

            _controllers.Add(
                controller);

            ControllersGrid.SelectedItem =
                controller;

            ControllersGrid.ScrollIntoView(
                controller);
        }


        private void Delete_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (ControllersGrid.SelectedItem
                is not ControllerInfo selected)
            {
                MessageBox.Show(
                    "Выберите контроллер.");

                return;
            }


            MessageBoxResult result =
                MessageBox.Show(
                    $"Удалить контроллер?\n\n" +
                    $"{selected.Name}\n" +
                    $"{selected.IpAddress}",
                    "Удаление контроллера",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;


            _controllers.Remove(
                selected);
        }


        private async void Check_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (ControllersGrid.SelectedItem
                is not ControllerInfo selected)
            {
                MessageBox.Show(
                    "Выберите контроллер.");

                return;
            }


            try
            {
                int index =
                    ControllersGrid.SelectedIndex;


                // Сохраняем наши пользовательские настройки,
                // чтобы DetectAsync их не потерял.
                string oldName =
                    selected.Name;

                bool oldUseByDefault =
                    selected.UseByDefault;

                AttendanceRole oldAttendanceRole =
                    selected.AttendanceRole;

                string oldUsername =
                    selected.Username;

                string oldPassword =
                    selected.Password;


                ControllerInfo detected =
                    await _detector.DetectAsync(
                        selected.IpAddress,
                        selected.Username,
                        selected.Password);


                detected.Name =
                    oldName;

                detected.UseByDefault =
                    oldUseByDefault;

                detected.AttendanceRole =
                    oldAttendanceRole;


                if (string.IsNullOrWhiteSpace(
                        detected.Username))
                {
                    detected.Username =
                        oldUsername;
                }

                if (string.IsNullOrWhiteSpace(
                        detected.Password))
                {
                    detected.Password =
                        oldPassword;
                }


                if (index >= 0)
                {
                    _controllers[index] =
                        detected;

                    ControllersGrid.SelectedIndex =
                        index;
                }


                MessageBox.Show(
                    detected.IsOnline
                        ? $"Контроллер доступен.\n\n" +
                          $"Модель: {detected.Model}\n" +
                          $"API: {detected.ApiType}"
                        : "Контроллер недоступен.",
                    "Проверка контроллера");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.ToString(),
                    "Ошибка проверки контроллера",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        private async void CheckAll_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_controllers.Count == 0)
            {
                MessageBox.Show(
                    "Список контроллеров пуст.");

                return;
            }


            try
            {
                // Сохраняем пользовательские параметры
                // перед автоматическим определением устройств.
                Dictionary<string, ControllerUserSettings>
                    settings =
                        _controllers
                            .Where(x =>
                                !string.IsNullOrWhiteSpace(
                                    x.IpAddress))
                            .ToDictionary(
                                x => x.IpAddress,
                                x => new ControllerUserSettings
                                {
                                    Name =
                                        x.Name,

                                    UseByDefault =
                                        x.UseByDefault,

                                    AttendanceRole =
                                        x.AttendanceRole,

                                    Username =
                                        x.Username,

                                    Password =
                                        x.Password
                                },
                                StringComparer.OrdinalIgnoreCase);


                var detectedControllers =
                    await _detector
                        .DetectAllAsync(
                            _controllers);


                _controllers.Clear();


                foreach (ControllerInfo controller
                         in detectedControllers)
                {
                    if (settings.TryGetValue(
                            controller.IpAddress,
                            out ControllerUserSettings? saved))
                    {
                        controller.Name =
                            saved.Name;

                        controller.UseByDefault =
                            saved.UseByDefault;

                        controller.AttendanceRole =
                            saved.AttendanceRole;


                        if (string.IsNullOrWhiteSpace(
                                controller.Username))
                        {
                            controller.Username =
                                saved.Username;
                        }

                        if (string.IsNullOrWhiteSpace(
                                controller.Password))
                        {
                            controller.Password =
                                saved.Password;
                        }
                    }


                    _controllers.Add(
                        controller);
                }


                MessageBox.Show(
                    "Проверка всех контроллеров завершена.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.ToString(),
                    "Ошибка проверки контроллеров",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        private async void Save_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                // Завершаем редактирование текущей ячейки,
                // чтобы последнее введённое значение
                // точно попало в объект.
                ControllersGrid.CommitEdit(
                    DataGridEditingUnit.Cell,
                    true);

                ControllersGrid.CommitEdit(
                    DataGridEditingUnit.Row,
                    true);


                // Проверяем IP.
                var invalid =
                    _controllers.FirstOrDefault(x =>
                        string.IsNullOrWhiteSpace(
                            x.IpAddress));

                if (invalid != null)
                {
                    MessageBox.Show(
                        "У одного из контроллеров не указан IP-адрес.",
                        "Контроллеры",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }


                // Проверяем дубли IP в текущем списке.
                bool duplicateIp =
                    _controllers
                        .GroupBy(
                            x => x.IpAddress.Trim(),
                            StringComparer.OrdinalIgnoreCase)
                        .Any(x =>
                            x.Count() > 1);

                if (duplicateIp)
                {
                    MessageBox.Show(
                        "В списке есть контроллеры " +
                        "с одинаковым IP-адресом.",
                        "Контроллеры",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }


                // Получаем то, что сейчас хранится в БД.
                List<ControllerInfo> databaseControllers =
                    await _repository.GetAllAsync();


                HashSet<string> currentIps =
                    _controllers
                        .Select(x =>
                            x.IpAddress.Trim())
                        .ToHashSet(
                            StringComparer.OrdinalIgnoreCase);


                // Удаляем из БД контроллеры,
                // которые пользователь удалил из таблицы.
                foreach (ControllerInfo databaseController
                         in databaseControllers)
                {
                    if (!currentIps.Contains(
                            databaseController.IpAddress))
                    {
                        await _repository.DeleteAsync(
                            databaseController.IpAddress);
                    }
                }


                // Добавляем новые и обновляем существующие.
                foreach (ControllerInfo controller
                         in _controllers)
                {
                    await _repository.SaveAsync(
                        controller);
                }


                MessageBox.Show(
                    $"Контроллеры сохранены в базе:\n\n" +
                    $"{_database.CurrentDatabase.Name}",
                    "Контроллеры",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);


                await LoadControllersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.ToString(),
                    "Ошибка сохранения контроллеров",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        private void Close_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }
    }


    /// <summary>
    /// Временный внутренний класс,
    /// чтобы CheckAll не терял настройки,
    /// которые пользователь назначил вручную.
    /// </summary>
    internal class ControllerUserSettings
    {
        public string Name { get; set; } = "";

        public bool UseByDefault { get; set; }

        public AttendanceRole AttendanceRole { get; set; }

        public string Username { get; set; } = "";

        public string Password { get; set; } = "";
    }
}