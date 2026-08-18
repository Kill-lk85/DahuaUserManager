using DahuaUserManager.Models.Schedules;
using DahuaUserManager.UI.Database;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace DahuaUserManager.UI.Windows;

public partial class EmployeeScheduleWindow : Window
{
    private readonly AttendanceDatabase _database = new();

    private readonly ScheduleRepository _scheduleRepository;

    private readonly AttendanceRepository _attendanceRepository;

    private readonly ObservableCollection<EmployeeScheduleRow>
        _assignmentRows = new();

    private readonly ObservableCollection<EmployeeScheduleEmployeeItem>
        _employees = new();

    public EmployeeScheduleWindow()
    {
        InitializeComponent();

        _scheduleRepository =
            new ScheduleRepository(_database);

        _attendanceRepository =
            new AttendanceRepository(_database);

        AssignmentsGrid.ItemsSource =
            _assignmentRows;

        EmployeesListBox.ItemsSource =
            _employees;

        DateFromPicker.SelectedDate =
            DateTime.Today;

        Loaded +=
            EmployeeScheduleWindow_Loaded;
    }

    private async void EmployeeScheduleWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        await LoadAllAsync();
    }

    private async Task LoadAllAsync()
    {
        try
        {
            await _database.InitializeAsync();

            await LoadEmployeesAsync();
            await LoadSchedulesAsync();
            await LoadAssignmentsAsync();

            UpdateSelectedEmployeesText();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.ToString(),
                "Ошибка загрузки",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task LoadEmployeesAsync()
    {
        // Сохраняем текущие галочки при обновлении окна.
        HashSet<string> selectedUserIds =
            _employees
                .Where(x => x.IsSelected)
                .Select(x => x.UserId)
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

        DateTime from =
            DateTime.Today.AddYears(-10);

        DateTime to =
            DateTime.Today.AddYears(10);

        var events =
            await _attendanceRepository
                .GetByPeriodAsync(
                    from,
                    to);

        var employees = events
            .Where(x =>
                !string.IsNullOrWhiteSpace(x.UserId))
            .GroupBy(x => x.UserId)
            .Select(g => new EmployeeScheduleEmployeeItem
            {
                UserId = g.Key,

                UserName = g
                    .Select(x => x.UserName)
                    .FirstOrDefault(x =>
                        !string.IsNullOrWhiteSpace(x))
                    ?? g.Key,

                IsSelected =
                    selectedUserIds.Contains(g.Key)
            })
            .OrderBy(x => x.UserName)
            .ThenBy(x => x.UserId)
            .ToList();

        _employees.Clear();

        foreach (EmployeeScheduleEmployeeItem employee
                 in employees)
        {
            _employees.Add(employee);
        }
    }

    private async Task LoadSchedulesAsync()
    {
        List<WorkSchedule> schedules =
            await _scheduleRepository
                .GetSchedulesAsync();

        List<EmployeeScheduleScheduleItem> items =
            schedules
                .Where(x => x.IsActive)
                .Select(x =>
                    new EmployeeScheduleScheduleItem
                    {
                        Schedule = x
                    })
                .OrderBy(x => x.Schedule.Name)
                .ToList();

        ScheduleComboBox.ItemsSource =
            items;

        if (items.Count > 0 &&
            ScheduleComboBox.SelectedIndex < 0)
        {
            ScheduleComboBox.SelectedIndex = 0;
        }
    }

    private async Task LoadAssignmentsAsync()
    {
        _assignmentRows.Clear();

        List<EmployeeSchedule> assignments =
            await _scheduleRepository
                .GetAllEmployeeAssignmentsAsync();

        List<WorkSchedule> schedules =
            await _scheduleRepository
                .GetSchedulesAsync();

        var scheduleMap =
            schedules.ToDictionary(
                x => x.Id);

        var allEvents =
            await _attendanceRepository
                .GetByPeriodAsync(
                    DateTime.Today.AddYears(-10),
                    DateTime.Today.AddYears(10));

        var employeeNames =
            allEvents
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.UserId))
                .GroupBy(x => x.UserId)
                .ToDictionary(
                    g => g.Key,
                    g => g
                        .Select(x => x.UserName)
                        .FirstOrDefault(x =>
                            !string.IsNullOrWhiteSpace(x))
                        ?? g.Key);

        foreach (EmployeeSchedule assignment
                 in assignments)
        {
            scheduleMap.TryGetValue(
                assignment.ScheduleId,
                out WorkSchedule? schedule);

            employeeNames.TryGetValue(
                assignment.UserId,
                out string? userName);

            _assignmentRows.Add(
                new EmployeeScheduleRow
                {
                    AssignmentId =
                        assignment.Id,

                    UserId =
                        assignment.UserId,

                    UserName =
                        userName ??
                        assignment.UserId,

                    ScheduleName =
                        schedule?.Name ?? "",

                    StartTime =
                        schedule?.StartTime.ToString(
                            @"hh\:mm") ?? "",

                    EndTime =
                        schedule?.EndTime.ToString(
                            @"hh\:mm") ?? "",

                    NormHours =
                        schedule?.NormHours
                            .ToString("0.##")
                        ?? "",

                    DateFrom =
                        assignment.DateFrom
                            .ToString("dd.MM.yyyy"),

                    DateTo =
                        assignment.DateTo.HasValue
                            ? assignment.DateTo.Value
                                .ToString("dd.MM.yyyy")
                            : ""
                });
        }
    }

    private async void Assign_Click(
        object sender,
        RoutedEventArgs e)
    {
        List<EmployeeScheduleEmployeeItem> selectedEmployees =
            _employees
                .Where(x => x.IsSelected)
                .ToList();

        if (selectedEmployees.Count == 0)
        {
            MessageBox.Show(
                "Отметьте хотя бы одного сотрудника.",
                "Назначение графика",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        if (ScheduleComboBox.SelectedItem
            is not EmployeeScheduleScheduleItem scheduleItem)
        {
            MessageBox.Show(
                "Выберите график.",
                "Назначение графика",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        if (DateFromPicker.SelectedDate == null)
        {
            MessageBox.Show(
                "Укажите дату начала графика.",
                "Назначение графика",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        DateTime dateFrom =
            DateFromPicker.SelectedDate.Value.Date;

        MessageBoxResult confirm =
            MessageBox.Show(
                $"Назначить график «{scheduleItem.Schedule.Name}»\n" +
                $"с {dateFrom:dd.MM.yyyy}\n\n" +
                $"отмеченным сотрудникам: {selectedEmployees.Count}?",
                "Назначение графика",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
            return;

        var resultLines =
            new List<string>();

        int successCount = 0;

        foreach (EmployeeScheduleEmployeeItem employee
                 in selectedEmployees)
        {
            try
            {
                await _scheduleRepository
                    .AssignScheduleAsync(
                        employee.UserId,
                        scheduleItem.Schedule.Id,
                        dateFrom);

                successCount++;

                resultLines.Add(
                    $"✓ {employee.UserName} ({employee.UserId})");
            }
            catch (Exception ex)
            {
                resultLines.Add(
                    $"✗ {employee.UserName} ({employee.UserId}) — {ex.Message}");
            }
        }

        await LoadAssignmentsAsync();

        MessageBox.Show(
            $"График: {scheduleItem.Schedule.Name}\n" +
            $"Дата начала: {dateFrom:dd.MM.yyyy}\n\n" +
            $"Успешно: {successCount} из {selectedEmployees.Count}\n\n" +
            string.Join(
                Environment.NewLine,
                resultLines),
            "Назначение графика",
            MessageBoxButton.OK,
            successCount == selectedEmployees.Count
                ? MessageBoxImage.Information
                : MessageBoxImage.Warning);
    }

    private async void DeleteAssignment_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (AssignmentsGrid.SelectedItem
            is not EmployeeScheduleRow selected)
        {
            MessageBox.Show(
                "Выберите назначение графика в таблице.",
                "Удаление назначения",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        MessageBoxResult result =
            MessageBox.Show(
                $"Удалить назначение графика?\n\n" +
                $"Сотрудник: {selected.UserName}\n" +
                $"UserID: {selected.UserId}\n" +
                $"График: {selected.ScheduleName}\n" +
                $"С даты: {selected.DateFrom}" +
                (string.IsNullOrWhiteSpace(selected.DateTo)
                    ? ""
                    : $"\nПо дату: {selected.DateTo}"),
                "Удаление назначения",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            bool deleted =
                await _scheduleRepository
                    .DeleteEmployeeAssignmentAsync(
                        selected.AssignmentId);

            if (!deleted)
            {
                MessageBox.Show(
                    "Назначение уже отсутствует в базе.",
                    "Удаление назначения",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                await LoadAssignmentsAsync();
                return;
            }

            await LoadAssignmentsAsync();

            MessageBox.Show(
                "Назначение графика удалено.",
                "Удаление назначения",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.ToString(),
                "Ошибка удаления назначения",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void SelectAllEmployees_Click(
        object sender,
        RoutedEventArgs e)
    {
        foreach (EmployeeScheduleEmployeeItem employee
                 in _employees)
        {
            employee.IsSelected = true;
        }

        UpdateSelectedEmployeesText();
    }

    private void ClearEmployees_Click(
        object sender,
        RoutedEventArgs e)
    {
        foreach (EmployeeScheduleEmployeeItem employee
                 in _employees)
        {
            employee.IsSelected = false;
        }

        UpdateSelectedEmployeesText();
    }

    private void EmployeeCheckBox_Changed(
        object sender,
        RoutedEventArgs e)
    {
        UpdateSelectedEmployeesText();
    }

    private void UpdateSelectedEmployeesText()
    {
        int count =
            _employees.Count(x => x.IsSelected);

        SelectedEmployeesText.Text =
            $"Отмечено: {count}";
    }

    private async void Refresh_Click(
        object sender,
        RoutedEventArgs e)
    {
        await LoadAllAsync();
    }

    private void Close_Click(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }
}

public class EmployeeScheduleEmployeeItem :
    INotifyPropertyChanged
{
    private bool _isSelected;

    public string UserId { get; set; } = "";

    public string UserName { get; set; } = "";

    public bool IsSelected
    {
        get => _isSelected;

        set
        {
            if (_isSelected == value)
                return;

            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public string DisplayName =>
        $"{UserName} ({UserId})";

    public event PropertyChangedEventHandler?
        PropertyChanged;

    private void OnPropertyChanged(
        [CallerMemberName]
        string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(
                propertyName));
    }
}

public class EmployeeScheduleScheduleItem
{
    public WorkSchedule Schedule { get; set; } =
        new();

    public string DisplayName =>
        $"{Schedule.Name}  " +
        $"[{Schedule.StartTime:hh\\:mm}–" +
        $"{Schedule.EndTime:hh\\:mm}]";
}

public class EmployeeScheduleRow
{
    public long AssignmentId { get; set; }

    public string UserId { get; set; } = "";

    public string UserName { get; set; } = "";

    public string ScheduleName { get; set; } = "";

    public string StartTime { get; set; } = "";

    public string EndTime { get; set; } = "";

    public string NormHours { get; set; } = "";

    public string DateFrom { get; set; } = "";

    public string DateTo { get; set; } = "";
}