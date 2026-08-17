using DahuaUserManager.UI.Windows;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace DahuaUserManager.UI.Windows;

public partial class TimesheetEmployeeSelectionWindow : Window
{
    private readonly ObservableCollection<TimesheetEmployeeChoice> _allItems = new();
    private readonly ObservableCollection<TimesheetEmployeeChoice> _visibleItems = new();

    public IReadOnlyCollection<string> SelectedUserIds =>
        _allItems
            .Where(x => x.IsSelected)
            .Select(x => x.UserId)
            .ToList();

    public TimesheetEmployeeSelectionWindow(
        IEnumerable<EmployeeSelectionItem> employees,
        IEnumerable<string> selectedUserIds)
    {
        InitializeComponent();

        HashSet<string> selected =
            selectedUserIds.ToHashSet();

        foreach (EmployeeSelectionItem employee in employees)
        {
            _allItems.Add(new TimesheetEmployeeChoice
            {
                UserId = employee.UserId,
                UserName = employee.UserName,
                IsSelected = selected.Contains(employee.UserId)
            });
        }

        EmployeesList.ItemsSource = _visibleItems;

        ApplyFilter();
        UpdateSelectedCount();
    }

    private void SearchBox_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        string search =
            SearchBox.Text?.Trim() ?? "";

        _visibleItems.Clear();

        foreach (TimesheetEmployeeChoice item in _allItems)
        {
            if (string.IsNullOrWhiteSpace(search) ||
                item.UserName.Contains(
                    search,
                    StringComparison.OrdinalIgnoreCase) ||
                item.UserId.Contains(
                    search,
                    StringComparison.OrdinalIgnoreCase))
            {
                _visibleItems.Add(item);
            }
        }
    }

    private void SelectAll_Click(
        object sender,
        RoutedEventArgs e)
    {
        // Выбираем все видимые строки.
        foreach (TimesheetEmployeeChoice item in _visibleItems)
            item.IsSelected = true;

        EmployeesList.Items.Refresh();
        UpdateSelectedCount();
    }

    private void ClearAll_Click(
        object sender,
        RoutedEventArgs e)
    {
        // Снимаем отметки только у видимых строк.
        foreach (TimesheetEmployeeChoice item in _visibleItems)
            item.IsSelected = false;

        EmployeesList.Items.Refresh();
        UpdateSelectedCount();
    }

    private void EmployeeCheck_Changed(
        object sender,
        RoutedEventArgs e)
    {
        UpdateSelectedCount();
    }

    private void UpdateSelectedCount()
    {
        int selected =
            _allItems.Count(x => x.IsSelected);

        SelectedCountText.Text =
            $"Выбрано: {selected}";
    }

    private void Ok_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!_allItems.Any(x => x.IsSelected))
        {
            MessageBox.Show(
                "Выберите хотя бы одного сотрудника.",
                "Сотрудники табеля",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
    }
}

public class TimesheetEmployeeChoice
{
    public string UserId { get; set; } = "";

    public string UserName { get; set; } = "";

    public bool IsSelected { get; set; }
}