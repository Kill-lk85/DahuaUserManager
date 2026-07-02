using System.Windows;
using System.Windows.Media.Imaging;
using DahuaUserManager.Models.Entities;
using Microsoft.Win32;

namespace DahuaUserManager.UI.Windows
{
    public partial class UserEditorWindow : Window
    {
        private readonly int _lastUserId;
        private readonly int _nextUserId;
        private bool _isLoading;

        public AccessUser User { get; private set; }

        public string PhotoPath { get; private set; } = "";

        public UserEditorWindow()
            : this(
                new AccessUser
                {
                    IsValid = true,
                    ValidFrom = DateTime.Today,
                    ValidTo = DateTime.Today.AddYears(10)
                },
                0,
                "")
        {
        }

        public UserEditorWindow(
            AccessUser user,
            int lastUserId,
            string lastCardNumber = "")
        {
            InitializeComponent();

            User = user;

            _lastUserId = lastUserId;
            _nextUserId = lastUserId + 1;

            LoadUser(lastCardNumber);
        }

        private void LoadUser(string lastCardNumber)
        {
            _isLoading = true;

            UserIdBox.Text = User.UserId;
            FullNameBox.Text = User.FullName;
            CardNumberBox.Text = User.CardNumber;

            ValidFromPicker.SelectedDate =
                User.ValidFrom ?? DateTime.Today;

            ValidToPicker.SelectedDate =
                User.ValidTo ?? DateTime.Today.AddYears(10);

            IsValidBox.IsChecked = User.IsValid;

            LastUserIdText.Text =
                $"Последний UserID: {_lastUserId}; следующий: {_nextUserId}";

            LastCardText.Text =
                string.IsNullOrWhiteSpace(lastCardNumber)
                    ? "Последняя карта: —"
                    : $"Последняя карта: {lastCardNumber}";

            _isLoading = false;
        }

        private void UserIdBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_isLoading)
                return;

            if (string.IsNullOrWhiteSpace(CardNumberBox.Text))
                CardNumberBox.Text = UserIdBox.Text.Trim();
        }

        private void UseNextUserId_Click(object sender, RoutedEventArgs e)
        {
            UserIdBox.Text = _nextUserId.ToString();

            if (string.IsNullOrWhiteSpace(CardNumberBox.Text))
                CardNumberBox.Text = UserIdBox.Text;
        }

        private void CardEqualsUserId_Click(object sender, RoutedEventArgs e)
        {
            CardNumberBox.Text = UserIdBox.Text.Trim();
        }

        private void SelectPhoto_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Выберите фото пользователя",
                Filter = "Изображения (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|Все файлы (*.*)|*.*"
            };

            if (dialog.ShowDialog(this) == true)
            {
                PhotoPath = dialog.FileName;
                PhotoPathText.Text = PhotoPath;

                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri(PhotoPath);
                image.EndInit();

                PhotoPreview.Source = image;
                NoPhotoText.Visibility = Visibility.Collapsed;
            }
        }

        private void ClearPhoto_Click(object sender, RoutedEventArgs e)
        {
            PhotoPath = "";
            PhotoPathText.Text = "";
            PhotoPreview.Source = null;
            NoPhotoText.Visibility = Visibility.Visible;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            User.UserId = UserIdBox.Text.Trim();
            User.FullName = FullNameBox.Text.Trim();
            User.CardNumber = CardNumberBox.Text.Trim();
            User.ValidFrom = ValidFromPicker.SelectedDate ?? DateTime.Today;
            User.ValidTo = ValidToPicker.SelectedDate ?? DateTime.Today.AddYears(10);
            User.IsValid = IsValidBox.IsChecked == true;

            if (string.IsNullOrWhiteSpace(User.UserId))
            {
                MessageBox.Show("Введите UserID.");
                return;
            }

            if (string.IsNullOrWhiteSpace(User.FullName))
            {
                MessageBox.Show("Введите имя пользователя.");
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}