using DahuaUserManager.Models.Entities;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;

namespace DahuaUserManager.UI.Windows
{
    public partial class UserEditorWindow : Window
    {
        private readonly int _lastUserId;
        private readonly int _nextUserId;

        private readonly List<ControllerInfo> _controllers;

        private readonly ObservableCollection<ControllerInfo>
            _controllerChoices = new();

        private readonly ControllerInfo? _selectedController;

        private readonly bool _isEditMode;

        private bool _isLoading;

        public AccessUser User { get; private set; }

        public string PhotoPath { get; private set; } = "";

        public List<ControllerInfo> SelectedControllers { get; } =
            new();

        public int DepartId { get; private set; } = 1;


        public UserEditorWindow()
            : this(
                new AccessUser
                {
                    IsValid = true,
                    ValidFrom = DateTime.Today,
                    ValidTo = DateTime.Today.AddYears(10)
                },
                0,
                "",
                Enumerable.Empty<ControllerInfo>(),
                null)
        {
        }


        public UserEditorWindow(
            AccessUser user,
            int lastUserId,
            string lastCardNumber = "")
            : this(
                user,
                lastUserId,
                lastCardNumber,
                Enumerable.Empty<ControllerInfo>(),
                null)
        {
        }


        public UserEditorWindow(
            AccessUser user,
            int lastUserId,
            string lastCardNumber,
            ControllerInfo? selectedController)
            : this(
                user,
                lastUserId,
                lastCardNumber,
                selectedController == null
                    ? Enumerable.Empty<ControllerInfo>()
                    : new[] { selectedController },
                selectedController)
        {
        }


        public UserEditorWindow(
            AccessUser user,
            int lastUserId,
            string lastCardNumber,
            IEnumerable<ControllerInfo> controllers,
            ControllerInfo? selectedController)
        {
            InitializeComponent();

            User = user;

            _lastUserId = lastUserId;
            _nextUserId = lastUserId + 1;

            _controllers =
                controllers.ToList();

            _selectedController =
                selectedController;

            _isEditMode =
                !string.IsNullOrWhiteSpace(
                    User.UserId);


            // --------------------------------------------------------
            // Локальный список выбора контроллеров.
            //
            // Контроллеры уже пришли из выбранной SQLite-базы
            // через MainWindow.
            //
            // Создаём копии, чтобы галочки в окне пользователя
            // не изменяли реальные настройки контроллеров в БД.
            // --------------------------------------------------------

            foreach (ControllerInfo controller
                     in _controllers)
            {
                _controllerChoices.Add(
                    CloneController(
                        controller));
            }


            ControllersListBox.ItemsSource =
                _controllerChoices;


            ApplyMode();

            LoadUser(
                lastCardNumber);


            SelectAllControllersButton.Click +=
                (_, _) =>
                {
                    foreach (ControllerInfo controller
                             in _controllerChoices)
                    {
                        controller.UseByDefault =
                            true;
                    }

                    ControllersListBox.Items.Refresh();
                };


            ClearControllersButton.Click +=
                (_, _) =>
                {
                    foreach (ControllerInfo controller
                             in _controllerChoices)
                    {
                        controller.UseByDefault =
                            false;
                    }

                    ControllersListBox.Items.Refresh();
                };
        }


        private static ControllerInfo CloneController(
            ControllerInfo source)
        {
            return new ControllerInfo
            {
                Name =
                    source.Name,

                IpAddress =
                    source.IpAddress,

                Username =
                    source.Username,

                Password =
                    source.Password,

                Model =
                    source.Model,

                Firmware =
                    source.Firmware,

                ApiType =
                    source.ApiType,

                UseByDefault =
                    source.UseByDefault,

                IsOnline =
                    source.IsOnline,

                AttendanceRole =
                    source.AttendanceRole
            };
        }


        private void ApplyMode()
        {
            if (!_isEditMode)
                return;

            Title =
                "Изменение пользователя";

            Width =
                540;


            if (Content is DockPanel)
                return;


            var mainGrid =
                (Grid)((Border?)null ?? Content);

            mainGrid.ColumnDefinitions[1].Width =
                new GridLength(0);
        }


        private void LoadUser(
            string lastCardNumber)
        {
            _isLoading =
                true;


            UserIdBox.Text =
                User.UserId;

            FullNameBox.Text =
                User.FullName;

            CardNumberBox.Text =
                User.CardNumber;


            ValidFromPicker.SelectedDate =
                User.ValidFrom ??
                DateTime.Today;


            ValidToPicker.SelectedDate =
                User.ValidTo ??
                DateTime.Today.AddYears(10);


            IsValidBox.IsChecked =
                User.IsValid;


            LastUserIdText.Text =
                $"Последний UserID: {_lastUserId}; " +
                $"следующий: {_nextUserId}";


            LastCardText.Text =
                string.IsNullOrWhiteSpace(
                    lastCardNumber)
                    ? "Последняя карта: —"
                    : $"Последняя карта: {lastCardNumber}";


            _isLoading =
                false;


            ControllersListBox.Items.Refresh();
        }


        private void UserIdBox_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            if (_isLoading)
                return;


            if (string.IsNullOrWhiteSpace(
                    CardNumberBox.Text))
            {
                CardNumberBox.Text =
                    UserIdBox.Text.Trim();
            }
        }


        private void UseNextUserId_Click(
            object sender,
            RoutedEventArgs e)
        {
            UserIdBox.Text =
                _nextUserId.ToString();


            if (string.IsNullOrWhiteSpace(
                    CardNumberBox.Text))
            {
                CardNumberBox.Text =
                    UserIdBox.Text;
            }
        }


        private void CardEqualsUserId_Click(
            object sender,
            RoutedEventArgs e)
        {
            CardNumberBox.Text =
                UserIdBox.Text.Trim();
        }


        private void SelectPhoto_Click(
            object sender,
            RoutedEventArgs e)
        {
            var dialog =
                new OpenFileDialog
                {
                    Title =
                        "Выберите фото пользователя",

                    Filter =
                        "Изображения (*.jpg;*.jpeg;*.png)|" +
                        "*.jpg;*.jpeg;*.png|" +
                        "Все файлы (*.*)|*.*"
                };


            if (dialog.ShowDialog(this) == true)
            {
                SetPhoto(
                    dialog.FileName);
            }
        }


        private void CapturePhotoFromController_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_controllers.Count == 0)
            {
                MessageBox.Show(
                    "Нет контроллеров для захвата фото.",
                    "Захват с контроллера",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }


            string userId =
                UserIdBox.Text.Trim();


            if (string.IsNullOrWhiteSpace(
                    userId))
            {
                userId =
                    "new";
            }


            var window =
                new PhotoCaptureWindow(
                    _controllers,
                    _selectedController,
                    userId)
                {
                    Owner = this
                };


            if (window.ShowDialog() == true)
            {
                SetPhoto(
                    window.PhotoPath);
            }
        }


        private void ClearPhoto_Click(
            object sender,
            RoutedEventArgs e)
        {
            PhotoPath = "";

            PhotoPathText.Text = "";

            PhotoPreview.Source = null;

            NoPhotoText.Visibility =
                Visibility.Visible;
        }


        private void Save_Click(
            object sender,
            RoutedEventArgs e)
        {
            User.UserId =
                UserIdBox.Text.Trim();

            User.FullName =
                FullNameBox.Text.Trim();

            User.CardNumber =
                CardNumberBox.Text.Trim();

            User.ValidFrom =
                ValidFromPicker.SelectedDate ??
                DateTime.Today;

            User.ValidTo =
                ValidToPicker.SelectedDate ??
                DateTime.Today.AddYears(10);

            User.IsValid =
                IsValidBox.IsChecked == true;


            if (string.IsNullOrWhiteSpace(
                    User.UserId))
            {
                MessageBox.Show(
                    "Введите UserID.");

                return;
            }


            if (string.IsNullOrWhiteSpace(
                    User.FullName))
            {
                MessageBox.Show(
                    "Введите имя пользователя.");

                return;
            }


            // --------------------------------------------------------
            // Собираем выбранные контроллеры.
            //
            // Теперь никаких ControllerManager / appsettings.json.
            // Используется только список текущей выбранной базы.
            // --------------------------------------------------------

            SelectedControllers.Clear();


            foreach (ControllerInfo controller
                     in _controllerChoices)
            {
                if (!controller.UseByDefault)
                    continue;


                // Находим оригинальный объект
                // из текущего списка контроллеров.
                ControllerInfo? original =
                    _controllers.FirstOrDefault(
                        x =>
                            x.IpAddress.Equals(
                                controller.IpAddress,
                                StringComparison.OrdinalIgnoreCase));


                if (original != null)
                {
                    SelectedControllers.Add(
                        original);
                }
            }


            DialogResult =
                true;

            Close();
        }


        private void Cancel_Click(
            object sender,
            RoutedEventArgs e)
        {
            DialogResult =
                false;

            Close();
        }


        private void SetPhoto(
            string fileName)
        {
            PhotoPath =
                fileName;

            PhotoPathText.Text =
                PhotoPath;


            var image =
                new BitmapImage();

            image.BeginInit();

            image.CacheOption =
                BitmapCacheOption.OnLoad;

            image.UriSource =
                new Uri(PhotoPath);

            image.EndInit();

            image.Freeze();


            PhotoPreview.Source =
                image;

            NoPhotoText.Visibility =
                Visibility.Collapsed;
        }
    }
}