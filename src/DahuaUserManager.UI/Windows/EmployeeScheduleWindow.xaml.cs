using DahuaUserManager.Api.Clients;
using DahuaUserManager.Models.Schedules;
using DahuaUserManager.UI.Database;
using System.Collections.ObjectModel;
using System.Windows;

namespace DahuaUserManager.UI.Windows;

public partial class EmployeeScheduleWindow : Window
{
    private readonly AttendanceDatabase _database = new();

    private readonly ScheduleRepository _scheduleRepository;

    private readonly AttendanceRepository _attendanceRepository;

    private readonly ObservableCollection<EmployeeScheduleRow>
        _assignmentRows = new();

    public EmployeeScheduleWindow()
    {
        InitializeComponent();

        _scheduleRepository =
            new ScheduleRepository(_database);

        _attendanceRepository =
            new AttendanceRepository(_database);

        AssignmentsGrid.ItemsSource =
            _assignmentRows;

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
                    ?? g.Key
            })
            .OrderBy(x => x.UserName)
            .ThenBy(x => x.UserId)
            .ToList();

        EmployeeComboBox.ItemsSource =
            employees;

        if (employees.Count > 0)
            EmployeeComboBox.SelectedIndex = 0;
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

        if (items.Count > 0)
            ScheduleComboBox.SelectedIndex = 0;
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
        if (EmployeeComboBox.SelectedItem
            is not EmployeeScheduleEmployeeItem employee)
        {
            MessageBox.Show(
                "Выберите сотрудника.",
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

        try
        {
            await _scheduleRepository
                .AssignScheduleAsync(
                    employee.UserId,
                    scheduleItem.Schedule.Id,
                    DateFromPicker.SelectedDate.Value.Date);

            await LoadAssignmentsAsync();

            MessageBox.Show(
                $"Сотруднику «{employee.UserName}» назначен график " +
                $"«{scheduleItem.Schedule.Name}».",
                "Назначение графика",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.ToString(),
                "Ошибка назначения графика",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
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

public class EmployeeScheduleEmployeeItem
{
    public string UserId { get; set; } = "";

    public string UserName { get; set; } = "";

    public string DisplayName =>
        $"{UserName} ({UserId})";
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
    public string UserId { get; set; } = "";

    public string UserName { get; set; } = "";

    public string ScheduleName { get; set; } = "";

    public string StartTime { get; set; } = "";

    public string EndTime { get; set; } = "";

    public string NormHours { get; set; } = "";

    public string DateFrom { get; set; } = "";

    public string DateTo { get; set; } = "";
}