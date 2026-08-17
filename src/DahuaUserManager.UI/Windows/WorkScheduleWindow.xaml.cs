using DahuaUserManager.Models.Schedules;
using DahuaUserManager.UI.Database;
using System.Collections.ObjectModel;
using System.Windows;

namespace DahuaUserManager.UI.Windows;

public partial class WorkScheduleWindow : Window
{
    private readonly AttendanceDatabase _database = new();

    private readonly ScheduleRepository _repository;

    private readonly ObservableCollection<WorkSchedule>
        _schedules = new();

    private readonly List<long>
        _deletedScheduleIds = new();

    public WorkScheduleWindow()
    {
        InitializeComponent();

        _repository =
            new ScheduleRepository(_database);

        SchedulesGrid.ItemsSource =
            _schedules;

        Loaded +=
            WorkScheduleWindow_Loaded;
    }

    private async void WorkScheduleWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        await LoadSchedulesAsync();
    }

    private async Task LoadSchedulesAsync()
    {
        try
        {
            await _database.InitializeAsync();

            List<WorkSchedule> schedules =
                await _repository.GetSchedulesAsync();

            _schedules.Clear();

            foreach (WorkSchedule schedule in schedules)
            {
                _schedules.Add(schedule);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.ToString(),
                "Ошибка загрузки графиков",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void Add_Click(
        object sender,
        RoutedEventArgs e)
    {
        int number =
            _schedules.Count + 1;

        var schedule =
            new WorkSchedule
            {
                Name =
                    $"Смена {number}",

                StartTime =
                    new TimeSpan(8, 0, 0),

                EndTime =
                    new TimeSpan(17, 0, 0),

                NormHours = 8,

                IsActive = true
            };

        _schedules.Add(schedule);

        SchedulesGrid.SelectedItem =
            schedule;

        SchedulesGrid.ScrollIntoView(
            schedule);
    }

    private void Delete_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (SchedulesGrid.SelectedItem
            is not WorkSchedule selected)
        {
            MessageBox.Show(
                "Выберите график.",
                "Графики работы",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        MessageBoxResult result =
            MessageBox.Show(
                $"Удалить график «{selected.Name}»?",
                "Удаление графика",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return;

        if (selected.Id > 0)
        {
            _deletedScheduleIds.Add(
                selected.Id);
        }

        _schedules.Remove(selected);
    }

    private async void Save_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            SchedulesGrid.CommitEdit(
                System.Windows.Controls.DataGridEditingUnit.Cell,
                true);

            SchedulesGrid.CommitEdit(
                System.Windows.Controls.DataGridEditingUnit.Row,
                true);

            if (_schedules.Count == 0 &&
                _deletedScheduleIds.Count == 0)
            {
                MessageBox.Show(
                    "Нет изменений для сохранения.",
                    "Графики работы",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            foreach (WorkSchedule schedule
                     in _schedules)
            {
                if (string.IsNullOrWhiteSpace(
                        schedule.Name))
                {
                    MessageBox.Show(
                        "У графика должно быть название.",
                        "Графики работы",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                if (schedule.EndTime <=
                    schedule.StartTime)
                {
                    MessageBox.Show(
                        $"У графика «{schedule.Name}» " +
                        "время окончания должно быть позже времени начала.",
                        "Графики работы",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                if (schedule.NormHours <= 0)
                {
                    MessageBox.Show(
                        $"У графика «{schedule.Name}» " +
                        "норма часов должна быть больше нуля.",
                        "Графики работы",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }
            }

            // Сначала сохраняем/обновляем графики.
            foreach (WorkSchedule schedule
                     in _schedules)
            {
                long id =
                    await _repository
                        .SaveScheduleAsync(
                            schedule);

                schedule.Id = id;
            }

            // Затем удаляем отмеченные.
            foreach (long id
                     in _deletedScheduleIds)
            {
                try
                {
                    await _repository
                        .DeleteScheduleAsync(id);
                }
                catch
                {
                    MessageBox.Show(
                        "Не удалось удалить один из графиков.\n\n" +
                        "Возможно, он уже назначен сотруднику.",
                        "Удаление графика",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }

            _deletedScheduleIds.Clear();

            await LoadSchedulesAsync();

            MessageBox.Show(
                "Графики сохранены.",
                "Графики работы",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.ToString(),
                "Ошибка сохранения графиков",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void Close_Click(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }
}