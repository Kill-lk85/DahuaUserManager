using System.Collections.ObjectModel;
using System.Windows;
using DahuaUserManager.Core.Managers;
using DahuaUserManager.Core.Services;
using DahuaUserManager.Models.Entities;
using DahuaUserManager.UI.Services;

namespace DahuaUserManager.UI.Windows
{
    public partial class ControllerManagerWindow : Window
    {
        private readonly ControllerManager _manager = new();
        private readonly ControllerDetectionService _detector = new();
        private readonly ObservableCollection<ControllerInfo> _controllers = new();

        public ControllerManagerWindow()
        {
            InitializeComponent();

            ControllersGrid.ItemsSource = _controllers;

            Loaded += ControllerManagerWindow_Loaded;
            Closing += ControllerManagerWindow_Closing;

            LoadControllers();
        }

        private void ControllerManagerWindow_Loaded(object sender, RoutedEventArgs e)
        {
            DataGridLayoutService.Load(ControllersGrid, "ControllersGrid");
        }

        private void ControllerManagerWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            DataGridLayoutService.Save(ControllersGrid, "ControllersGrid");
        }

        private void LoadControllers()
        {
            _manager.Load();

            _controllers.Clear();

            foreach (ControllerInfo controller in _manager.Controllers)
                _controllers.Add(controller);
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            _controllers.Add(new ControllerInfo
            {
                Name = "Новый контроллер",
                IpAddress = "192.168.0.",
                Username = "admin",
                Password = "Admin123!",
                UseByDefault = true
            });
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (ControllersGrid.SelectedItem is not ControllerInfo selected)
            {
                MessageBox.Show("Выберите контроллер.");
                return;
            }

            _controllers.Remove(selected);
        }

        private async void Check_Click(object sender, RoutedEventArgs e)
        {
            if (ControllersGrid.SelectedItem is not ControllerInfo selected)
            {
                MessageBox.Show("Выберите контроллер.");
                return;
            }

            int index = ControllersGrid.SelectedIndex;

            ControllerInfo detected = await _detector.DetectAsync(
                selected.IpAddress,
                selected.Username,
                selected.Password);

            detected.UseByDefault = selected.UseByDefault;

            if (index >= 0)
            {
                _controllers[index] = detected;
                ControllersGrid.SelectedIndex = index;
            }

            MessageBox.Show(
                detected.IsOnline
                    ? $"Контроллер доступен.\n\nМодель: {detected.Model}\nAPI: {detected.ApiType}"
                    : "Контроллер недоступен.",
                "Проверка контроллера");
        }

        private async void CheckAll_Click(object sender, RoutedEventArgs e)
        {
            if (_controllers.Count == 0)
            {
                MessageBox.Show("Список контроллеров пуст.");
                return;
            }

            Dictionary<string, bool> defaultFlags = _controllers
                .Where(x => !string.IsNullOrWhiteSpace(x.IpAddress))
                .ToDictionary(x => x.IpAddress, x => x.UseByDefault);

            var detectedControllers = await _detector.DetectAllAsync(_controllers);

            _controllers.Clear();

            foreach (ControllerInfo controller in detectedControllers)
            {
                if (defaultFlags.TryGetValue(controller.IpAddress, out bool useByDefault))
                    controller.UseByDefault = useByDefault;

                _controllers.Add(controller);
            }

            MessageBox.Show("Проверка всех контроллеров завершена.");
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            _manager.Controllers.Clear();

            foreach (ControllerInfo controller in _controllers)
                _manager.Controllers.Add(controller);

            _manager.Save();

            MessageBox.Show("Контроллеры сохранены.");
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}