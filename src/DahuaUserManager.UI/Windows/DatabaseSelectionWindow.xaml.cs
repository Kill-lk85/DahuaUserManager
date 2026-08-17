using DahuaUserManager.UI.Database;
using System.Windows;
using System.Windows.Controls;

namespace DahuaUserManager.UI.Windows;

public partial class DatabaseSelectionWindow : Window
{
    private readonly DatabaseManager _databaseManager =
        DatabaseManager.Instance;

    public DatabaseSelectionWindow()
    {
        InitializeComponent();

        Loaded += DatabaseSelectionWindow_Loaded;
    }

    private void DatabaseSelectionWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        LoadDatabases();
    }

    private void LoadDatabases(
        string? selectDatabaseId = null)
    {
        _databaseManager.Load();

        List<DatabaseProfile> databases =
            _databaseManager.Databases.ToList();

        DatabaseComboBox.ItemsSource = databases;

        DatabaseProfile? selected = null;

        if (!string.IsNullOrWhiteSpace(selectDatabaseId))
        {
            selected = databases.FirstOrDefault(
                x => x.Id == selectDatabaseId);
        }

        selected ??= databases.FirstOrDefault(
            x => x.Id ==
                 _databaseManager.CurrentDatabase.Id);

        selected ??= databases.FirstOrDefault();

        if (selected != null)
        {
            DatabaseComboBox.SelectedItem = selected;
        }

        UpdateDatabaseFileText();
    }

    private void DatabaseComboBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        UpdateDatabaseFileText();
    }

    private void UpdateDatabaseFileText()
    {
        if (DatabaseComboBox.SelectedItem
            is not DatabaseProfile database)
        {
            DatabaseFileText.Text = "";
            return;
        }

        DatabaseFileText.Text =
            database.FileName;
    }

    private async void CreateDatabase_Click(
        object sender,
        RoutedEventArgs e)
    {
        var window =
            new CreateDatabaseWindow
            {
                Owner = this
            };

        bool? result =
            window.ShowDialog();

        if (result != true)
            return;

        try
        {
            DatabaseProfile database =
                _databaseManager.AddDatabase(
                    window.DatabaseName);

            // Временно выбираем новую базу,
            // чтобы AttendanceDatabase создал
            // в ней всю структуру таблиц.
            _databaseManager.SelectDatabase(
                database);

            var attendanceDatabase =
                new AttendanceDatabase();

            await attendanceDatabase
                .InitializeAsync();

            LoadDatabases(
                database.Id);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.ToString(),
                "Ошибка создания базы",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void OpenDatabase_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (DatabaseComboBox.SelectedItem
            is not DatabaseProfile database)
        {
            MessageBox.Show(
                "Выберите базу данных.",
                "База данных",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        try
        {
            _databaseManager.SelectDatabase(
                database);

            var attendanceDatabase =
                new AttendanceDatabase();

            await attendanceDatabase
                .InitializeAsync();

            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.ToString(),
                "Ошибка открытия базы",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult = false;
    }
}