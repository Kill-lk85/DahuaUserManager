using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DahuaUserManager.Api.Clients;
using DahuaUserManager.Models.Entities;

namespace DahuaUserManager.UI.Windows
{
    public partial class PhotoCaptureWindow : Window
    {
        private const double TargetRatio = 190.0 / 230.0;

        private readonly List<ControllerInfo> _controllers;
        private readonly string _userId;
        private readonly PhotoCaptureClient _photoCaptureClient = new();
        private readonly DispatcherTimer _timer = new();

        private byte[]? _lastFrame;
        private byte[]? _capturedFrame;
        private bool _isLoadingFrame;

        public string PhotoPath { get; private set; } = "";

        public PhotoCaptureWindow(
            IEnumerable<ControllerInfo> controllers,
            ControllerInfo? selectedController,
            string userId)
        {
            InitializeComponent();

            _controllers = controllers.ToList();
            _userId = string.IsNullOrWhiteSpace(userId) ? "new" : userId;

            ControllersBox.ItemsSource = _controllers;

            if (selectedController != null)
            {
                ControllerInfo? found = _controllers.FirstOrDefault(x =>
                    x.IpAddress == selectedController.IpAddress);

                ControllersBox.SelectedItem = found ?? selectedController;
            }
            else if (_controllers.Count > 0)
            {
                ControllersBox.SelectedIndex = 0;
            }

            _timer.Interval = TimeSpan.FromMilliseconds(33);
            _timer.Tick += Timer_Tick;

            Loaded += PhotoCaptureWindow_Loaded;
        }

        private void PhotoCaptureWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _timer.Start();
        }

        private async void Timer_Tick(object? sender, EventArgs e)
        {
            await LoadLiveFrameAsync();
        }

        private async void ControllersBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            _lastFrame = null;
            LiveImage.Source = null;
            LivePlaceholder.Visibility = Visibility.Visible;

            await LoadLiveFrameAsync();
        }

        private async void RefreshFrame_Click(object sender, RoutedEventArgs e)
        {
            await LoadLiveFrameAsync();
        }

        private async Task LoadLiveFrameAsync()
        {
            if (_isLoadingFrame)
                return;

            if (ControllersBox.SelectedItem is not ControllerInfo controller)
                return;

            try
            {
                _isLoadingFrame = true;

                byte[] bytes = await _photoCaptureClient.GetSnapshotBytesAsync(
                    controller.IpAddress,
                    controller.Username,
                    controller.Password);

                _lastFrame = bytes;

                LiveImage.Source = CreateBitmap(bytes);
                LivePlaceholder.Visibility = Visibility.Collapsed;

                StatusText.Text =
                    $"Поток: {controller.IpAddress}   {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                StatusText.Text =
                    "Ошибка получения кадра:\n" + ex.Message;
            }
            finally
            {
                _isLoadingFrame = false;
            }
        }

        private void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            if (_lastFrame == null)
            {
                MessageBox.Show(
                    "Сначала дождитесь изображения с контроллера.",
                    "Захват фотографии",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            _capturedFrame = CropToPreviewRatio(_lastFrame);

            CapturedImage.Source = CreateBitmap(_capturedFrame);
            CapturedPlaceholder.Visibility = Visibility.Collapsed;
            OkButton.IsEnabled = true;

            StatusText.Text = "Фото сделано и обрезано под рамку. Нажмите OK.";
        }

        private async void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (_capturedFrame == null)
                return;

            try
            {
                PhotoPath = await _photoCaptureClient.SaveSnapshotBytesAsync(
                    _capturedFrame,
                    _userId);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.ToString(),
                    "Ошибка сохранения фото");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _timer.Stop();
        }

        private static byte[] CropToPreviewRatio(byte[] bytes)
        {
            BitmapSource source = CreateBitmap(bytes);

            int sourceWidth = source.PixelWidth;
            int sourceHeight = source.PixelHeight;

            double sourceRatio = (double)sourceWidth / sourceHeight;

            int cropWidth = sourceWidth;
            int cropHeight = sourceHeight;

            if (sourceRatio > TargetRatio)
            {
                cropWidth = (int)Math.Round(sourceHeight * TargetRatio);
            }
            else
            {
                cropHeight = (int)Math.Round(sourceWidth / TargetRatio);
            }

            int x = Math.Max(0, (sourceWidth - cropWidth) / 2);
            int y = Math.Max(0, (sourceHeight - cropHeight) / 2);

            var cropped = new CroppedBitmap(
                source,
                new System.Windows.Int32Rect(
                    x,
                    y,
                    cropWidth,
                    cropHeight));

            var encoder = new JpegBitmapEncoder();
            encoder.QualityLevel = 90;
            encoder.Frames.Add(BitmapFrame.Create(cropped));

            using var stream = new MemoryStream();
            encoder.Save(stream);

            return stream.ToArray();
        }

        private static BitmapImage CreateBitmap(byte[] bytes)
        {
            using var stream = new MemoryStream(bytes);

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();

            return image;
        }
    }
}