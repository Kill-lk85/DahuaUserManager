using System.Windows;

namespace DahuaUserManager.UI.Windows;

public partial class CreateDatabaseWindow : Window
{
    public string DatabaseName { get; private set; } = "";

    public CreateDatabaseWindow()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            DatabaseNameTextBox.Focus();
        };
    }

    private void Create_Click(
        object sender,
        RoutedEventArgs e)
    {
        string name =
            DatabaseNameTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show(
                "Введите название базы / объекта.",
                "Новая база",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            DatabaseNameTextBox.Focus();

            return;
        }

        DatabaseName = name;

        DialogResult = true;
    }
}