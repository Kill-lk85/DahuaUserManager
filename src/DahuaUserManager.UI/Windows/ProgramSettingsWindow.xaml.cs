using DahuaUserManager.UI.Database;
using DahuaUserManager.UI.Services;
using DahuaUserManager.UI.Settings;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace DahuaUserManager.UI.Windows;

public partial class ProgramSettingsWindow : Window
{
    private readonly ProgramSettingsService
        _settingsService = new();

    private readonly DatabaseBackupService
        _backupService = new();

    private readonly TaskSchedulerService
        _taskSchedulerService = new();

    private ProgramSettings
        _settings = new();

    public ProgramSettingsWindow()
    {
        InitializeComponent();

        Loaded +=
            ProgramSettingsWindow_Loaded;
    }

    private void ProgramSettingsWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        _settings =
            _settingsService.Load();

        DatabaseFolderBox.Text =
            DatabaseManager.Instance.DatabasesFolder;

        BackupFolderBox.Text =
            _settings.BackupDestinationFolder;

        SevenZipPathBox.Text =
            _settings.SevenZipPath;

        BackupPasswordBox.Password =
            _settings.BackupPassword;

        EncryptFileNamesBox.IsChecked =
            _settings.EncryptFileNames;

        AutomaticBackupBox.IsChecked =
            _settings.AutomaticBackupEnabled;

        BackupTimeBox.Text =
            _settings.BackupTime;

        RetentionDaysBox.Text =
            _settings.RetentionDays.ToString();
    }

    private void BrowseBackupFolder_Click(
        object sender,
        RoutedEventArgs e)
    {
        var dialog =
            new OpenFolderDialog
            {
                Title =
                    "Куда сохранять резервные копии"
            };

        if (!string.IsNullOrWhiteSpace(
                BackupFolderBox.Text) &&
            Directory.Exists(
                BackupFolderBox.Text))
        {
            dialog.InitialDirectory =
                BackupFolderBox.Text;
        }

        if (dialog.ShowDialog(this) == true)
        {
            BackupFolderBox.Text =
                dialog.FolderName;
        }
    }

    private void BrowseSevenZip_Click(
        object sender,
        RoutedEventArgs e)
    {
        var dialog =
            new OpenFileDialog
            {
                Title =
                    "Выберите 7z.exe",

                Filter =
                    "7-Zip (7z.exe)|7z.exe|" +
                    "Исполняемые файлы (*.exe)|*.exe"
            };

        if (dialog.ShowDialog(this) == true)
        {
            SevenZipPathBox.Text =
                dialog.FileName;
        }
    }

    private async void BackupNow_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            ProgramSettings settings =
                ReadSettingsFromForm();

            Mouse.OverrideCursor =
                System.Windows.Input.Cursors.Wait;

            string archive =
                await _backupService
                    .CreateBackupAsync(
                        settings);

            MessageBox.Show(
                $"Резервная копия создана:\n\n{archive}",
                "Резервное копирование",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Ошибка резервного копирования",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private async void Save_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            ProgramSettings settings =
                ReadSettingsFromForm();

            _settingsService.Save(
                settings);

            await _taskSchedulerService
                .ApplyAsync(
                    settings);

            MessageBox.Show(
                settings.AutomaticBackupEnabled
                    ? "Настройки сохранены.\n\n" +
                      "Задача автоматического бэкапа создана/обновлена в Планировщике Windows."
                    : "Настройки сохранены.\n\n" +
                      "Автоматический бэкап отключён.",
                "Настройки программы",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Ошибка сохранения настроек",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private ProgramSettings ReadSettingsFromForm()
    {
        if (!int.TryParse(
                RetentionDaysBox.Text.Trim(),
                out int retentionDays) ||
            retentionDays < 1)
        {
            throw new InvalidOperationException(
                "Количество дней хранения должно быть числом не меньше 1.");
        }

        string time =
            BackupTimeBox.Text.Trim();

        if (!TimeOnly.TryParseExact(
                time,
                "HH:mm",
                out _))
        {
            throw new InvalidOperationException(
                "Время должно быть в формате HH:mm, например 23:00.");
        }

        return new ProgramSettings
        {
            BackupDestinationFolder =
                BackupFolderBox.Text.Trim(),

            SevenZipPath =
                SevenZipPathBox.Text.Trim(),

            BackupPassword =
                BackupPasswordBox.Password,

            EncryptFileNames =
                EncryptFileNamesBox.IsChecked == true,

            AutomaticBackupEnabled =
                AutomaticBackupBox.IsChecked == true,

            BackupTime =
                time,

            RetentionDays =
                retentionDays
        };
    }
}