using ClosedXML.Excel;
using DahuaUserManager.Api.Clients;
using DahuaUserManager.Models.Database;
using DahuaUserManager.Models.Entities;
using DahuaUserManager.Models.Schedules;
using DahuaUserManager.UI.Database;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PdfColors = QuestPDF.Helpers.Colors;
using PdfDocument = QuestPDF.Fluent.Document;
using PdfPageSizes = QuestPDF.Helpers.PageSizes;

namespace DahuaUserManager.UI.Windows;

public partial class AttendanceReportWindow : Window
{
    private readonly AttendanceDatabase _attendanceDatabase = new();
    private readonly AttendanceRepository _attendanceRepository;
    private readonly ScheduleRepository _scheduleRepository;

    private readonly ObservableCollection<AttendanceRecord> _records = new();
    private readonly ObservableCollection<AttendanceSummary> _summary = new();
    private readonly List<ControllerInfo> _controllers;

    private bool _ignoreFilterChanges;
    private string _selectedEmployeeUserId = "";
    private readonly HashSet<string> _timesheetSelectedUserIds = new();
    private List<AttendanceRecord> _allLoadedRecords = new();

    public AttendanceReportWindow(IEnumerable<ControllerInfo> controllers)
    {
        InitializeComponent();

        _attendanceRepository =
            new AttendanceRepository(_attendanceDatabase);

        _scheduleRepository =
            new ScheduleRepository(_attendanceDatabase);

        AttendanceGrid.ItemsSource = _records;
        SummaryGrid.ItemsSource = _summary;

        _controllers = controllers.ToList();

        DateFromPicker.SelectedDate = DateTime.Today;
        DateToPicker.SelectedDate = DateTime.Today;

        var controllerItems = new List<ControllerSelectionItem>
        {
            new() { Name = "Все контроллеры" }
        };

        controllerItems.AddRange(
            _controllers
                .Where(x => x.AttendanceRole != AttendanceRole.None)
                .Select(x => new ControllerSelectionItem
                {
                    Name = $"{x.Name} ({GetRoleName(x.AttendanceRole)})",
                    Controller = x
                }));

        ControllerComboBox.ItemsSource = controllerItems;
        ControllerComboBox.SelectedIndex = 0;

        EmployeeComboBox.ItemsSource = new List<EmployeeSelectionItem>
        {
            new()
        };
        EmployeeComboBox.SelectedIndex = 0;
    }

    private async void LoadReport_Click(object sender, RoutedEventArgs e)
    {
        if (DateFromPicker.SelectedDate == null ||
            DateToPicker.SelectedDate == null)
        {
            MessageBox.Show(
                "Выберите период отчёта.",
                "Посещаемость",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        DateTime selectedFrom =
            DateFromPicker.SelectedDate.Value.Date;

        DateTime selectedTo =
            DateToPicker.SelectedDate.Value.Date;

        if (selectedFrom > selectedTo)
        {
            MessageBox.Show(
                "Дата начала не может быть позже даты окончания.",
                "Посещаемость",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (ControllerComboBox.SelectedItem
            is not ControllerSelectionItem selection)
        {
            MessageBox.Show(
                "Выберите контроллер.",
                "Посещаемость",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        List<ControllerInfo> controllersToSync;

        if (selection.Controller == null)
        {
            controllersToSync = _controllers
                .Where(x =>
                    x.AttendanceRole != AttendanceRole.None)
                .ToList();
        }
        else
        {
            controllersToSync =
                new List<ControllerInfo>
                {
                    selection.Controller
                };
        }

        if (controllersToSync.Count == 0)
        {
            MessageBox.Show(
                "Нет контроллеров, назначенных для посещаемости.\n\n" +
                "В управлении контроллерами назначьте роль Вход или Выход.",
                "Посещаемость",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        try
        {
            Mouse.OverrideCursor = Cursors.Wait;

            _records.Clear();
            _summary.Clear();
            _allLoadedRecords.Clear();

            var client = new RecordFinderClient();

            int totalDays =
                (selectedTo - selectedFrom).Days + 1;

            int currentDayNumber = 0;

            int insertedCount = 0;
            int controllerErrors = 0;

            // --------------------------------------------------------
            // 1. СИНХРОНИЗАЦИЯ С КОНТРОЛЛЕРАМИ
            // --------------------------------------------------------
            //
            // Контроллеры теперь являются только источником новых событий.
            // Полученные записи сохраняются в SQLite.
            //
            // Отчёт ниже строится уже НЕ из ответа контроллера,
            // а из локальной базы SQLite.
            //
            // Запрашиваем каждый день отдельно, потому что Dahua
            // может обрезать большой журнал одним запросом.
            // --------------------------------------------------------

            for (DateTime day = selectedFrom;
                 day <= selectedTo;
                 day = day.AddDays(1))
            {
                currentDayNumber++;

                DateTime dayFrom =
                    day.Date;

                DateTime dayTo =
                    day.Date
                        .AddDays(1)
                        .AddSeconds(-1);

                CountText.Text =
                    $"Синхронизация: {day:dd.MM.yyyy} " +
                    $"({currentDayNumber}/{totalDays})";

                foreach (ControllerInfo controller
                         in controllersToSync)
                {
                    try
                    {
                        string response =
                            await client.GetAccessControlRecordRawAsync(
                                controller.IpAddress,
                                controller.Username,
                                controller.Password,
                                dayFrom,
                                dayTo);

                        List<AttendanceRecord> dayRecords =
                            ParseAttendanceRecords(
                                response,
                                controller);

                        foreach (AttendanceRecord record
                                 in dayRecords)
                        {
                            if (!TryConvertUnixTime(
                                    record.CreateTime,
                                    out DateTime eventTime))
                            {
                                continue;
                            }

                            // На всякий случай не сохраняем записи,
                            // которые Dahua вернул за пределами
                            // реально запрашиваемого дня.
                            if (eventTime.Date != day.Date)
                                continue;

                            var entity =
                                new AttendanceEventEntity
                                {
                                    RecNo = record.RecNo,
                                    UserId = record.UserId,
                                    UserName = record.UserName,
                                    ControllerName =
                                        controller.Name,
                                    ControllerIp =
                                        controller.IpAddress,
                                    Direction =
                                        GetRoleName(
                                            controller.AttendanceRole),
                                    EventTime = eventTime,
                                    EventUnixTime =
                                        record.CreateTimeValue,
                                    EventType = record.Event
                                };

                            bool inserted =
                                await _attendanceRepository
                                    .InsertIfNotExistsAsync(entity);

                            if (inserted)
                                insertedCount++;
                        }
                    }
                    catch
                    {
                        // Важно:
                        // недоступный контроллер больше не мешает
                        // показать уже сохранённую историю.
                        //
                        // После синхронизации мы всё равно прочитаем
                        // выбранный период из SQLite.
                        controllerErrors++;
                    }
                }
            }

            // --------------------------------------------------------
            // 2. ЧИТАЕМ ПЕРИОД ИЗ SQLITE
            // --------------------------------------------------------

            CountText.Text =
                "Чтение локальной базы...";

            DateTime databaseFrom =
                selectedFrom.Date;

            DateTime databaseTo =
                selectedTo.Date
                    .AddDays(1)
                    .AddTicks(-1);

            List<AttendanceEventEntity> databaseEvents =
                await _attendanceRepository.GetByPeriodAsync(
                    databaseFrom,
                    databaseTo);

            // Если выбран конкретный контроллер,
            // из локальной базы оставляем только его события.
            if (selection.Controller != null)
            {
                databaseEvents = databaseEvents
                    .Where(x =>
                        string.Equals(
                            x.ControllerIp,
                            selection.Controller.IpAddress,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            else
            {
                // Для "Все контроллеры" показываем только устройства,
                // которые сейчас назначены для посещаемости.
                HashSet<string> attendanceControllerIps =
                    _controllers
                        .Where(x =>
                            x.AttendanceRole != AttendanceRole.None)
                        .Select(x => x.IpAddress)
                        .Where(x =>
                            !string.IsNullOrWhiteSpace(x))
                        .ToHashSet(
                            StringComparer.OrdinalIgnoreCase);

                databaseEvents = databaseEvents
                    .Where(x =>
                        attendanceControllerIps.Contains(
                            x.ControllerIp))
                    .ToList();
            }

            _allLoadedRecords =
                databaseEvents
                    .Select(ConvertDatabaseEventToRecord)
                    .OrderBy(x => x.CreateTimeValue)
                    .ThenBy(x => x.UserName)
                    .ToList();

            BuildEmployeeFilter();
            ApplyFilters();

            string syncInfo =
                insertedCount > 0
                    ? $" | Новых: {insertedCount}"
                    : "";

            if (controllerErrors > 0)
            {
                syncInfo +=
                    $" | Ошибок синхронизации: {controllerErrors}";
            }

            CountText.Text =
                $"Записей: {_records.Count} | " +
                $"Сотрудников: " +
                $"{_summary.Select(x => x.UserId).Distinct().Count()}" +
                syncInfo;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.ToString(),
                "Ошибка отчёта",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private static AttendanceRecord ConvertDatabaseEventToRecord(
        AttendanceEventEntity entity)
    {
        return new AttendanceRecord
        {
            RecNo = entity.RecNo,
            Date = entity.EventTime.ToString(
                "dd.MM.yyyy",
                CultureInfo.InvariantCulture),
            Time = entity.EventTime.ToString(
                "HH:mm:ss",
                CultureInfo.InvariantCulture),
            UserId = entity.UserId,
            UserName = entity.UserName,
            ControllerName = entity.ControllerName,
            ControllerIp = entity.ControllerIp,
            ControllerRole =
                ParseAttendanceRole(entity.Direction),
            Event = entity.EventType,
            CreateTime =
                entity.EventUnixTime.ToString(
                    CultureInfo.InvariantCulture)
        };
    }

    private static AttendanceRole ParseAttendanceRole(
        string direction)
    {
        return direction switch
        {
            "Вход" => AttendanceRole.Entry,
            "Выход" => AttendanceRole.Exit,
            "Вход и выход" => AttendanceRole.Both,
            _ => AttendanceRole.None
        };
    }

    private void BuildEmployeeFilter()
    {
        var employees = _allLoadedRecords
            .Where(x => !string.IsNullOrWhiteSpace(x.UserId))
            .GroupBy(x => x.UserId)
            .Select(g => new EmployeeSelectionItem
            {
                UserId = g.Key,
                UserName = g
                    .Select(x => x.UserName)
                    .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
                    ?? g.Key
            })
            .OrderBy(x => x.UserName)
            .ThenBy(x => x.UserId)
            .ToList();

        var items = new List<EmployeeSelectionItem>
        {
            new()
        };

        items.AddRange(employees);

        _ignoreFilterChanges = true;

        EmployeeComboBox.ItemsSource = items;

        int index = 0;

        if (!string.IsNullOrWhiteSpace(_selectedEmployeeUserId))
        {
            int foundIndex = items.FindIndex(
                x => x.UserId == _selectedEmployeeUserId);

            if (foundIndex >= 0)
                index = foundIndex;
        }

        EmployeeComboBox.SelectedIndex = index;

        _ignoreFilterChanges = false;
    }

    private void ControllerFilter_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (_ignoreFilterChanges ||
            _allLoadedRecords.Count == 0)
        {
            return;
        }

        ApplyFilters();
    }

    private void EmployeeFilter_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (_ignoreFilterChanges)
            return;

        if (EmployeeComboBox.SelectedItem
            is EmployeeSelectionItem employee)
        {
            _selectedEmployeeUserId =
                employee.UserId ?? "";
        }
        else
        {
            _selectedEmployeeUserId = "";
        }

        if (_allLoadedRecords.Count == 0)
            return;

        ApplyFilters();
    }

    private void ApplyFilters()
    {
        IEnumerable<AttendanceRecord> query = _allLoadedRecords;

        if (ControllerComboBox.SelectedItem is ControllerSelectionItem controllerSelection &&
            controllerSelection.Controller != null)
        {
            query = query.Where(x =>
                string.Equals(
                    x.ControllerName,
                    controllerSelection.Controller.Name,
                    StringComparison.OrdinalIgnoreCase));
        }

        if (EmployeeComboBox.SelectedItem is EmployeeSelectionItem employee &&
            !string.IsNullOrWhiteSpace(employee.UserId))
        {
            query = query.Where(x => x.UserId == employee.UserId);
        }

        List<AttendanceRecord> filtered = query
            .OrderBy(x => x.CreateTimeValue)
            .ThenBy(x => x.UserName)
            .ToList();

        _records.Clear();
        foreach (AttendanceRecord record in filtered)
            _records.Add(record);

        BuildAttendanceResults(filtered);
        BuildSummary(filtered);

        CountText.Text =
            $"Записей: {_records.Count} | Сотрудников: {_summary.Select(x => x.UserId).Distinct().Count()}";
    }

    private static List<AttendanceRecord> ParseAttendanceRecords(
        string response,
        ControllerInfo controller)
    {
        var records = new Dictionary<int, AttendanceRecord>();

        foreach (string rawLine in response.Replace("\r\n", "\n").Split('\n'))
        {
            string line = rawLine.Trim();

            if (string.IsNullOrWhiteSpace(line) ||
                !line.StartsWith("records[", StringComparison.OrdinalIgnoreCase))
                continue;

            int openBracket = line.IndexOf('[');
            int closeBracket = line.IndexOf(']');
            int equalsIndex = line.IndexOf('=');

            if (openBracket < 0 || closeBracket < 0 || equalsIndex < 0)
                continue;

            if (!int.TryParse(
                    line.Substring(openBracket + 1,
                        closeBracket - openBracket - 1),
                    out int recordIndex))
                continue;

            string key = line.Substring(
                closeBracket + 2,
                equalsIndex - closeBracket - 2);

            string value = DecodeValue(line[(equalsIndex + 1)..]);

            if (!records.TryGetValue(recordIndex, out AttendanceRecord? record))
            {
                record = new AttendanceRecord
                {
                    ControllerName = controller.Name,
                    ControllerIp = controller.IpAddress,
                    ControllerRole = controller.AttendanceRole
                };
                records.Add(recordIndex, record);
            }

            switch (key)
            {
                case "RecNo":
                    record.RecNo = value;
                    break;
                case "UserID":
                    record.UserId = value;
                    break;
                case "CardName":
                    record.UserName = value;
                    break;
                case "CreateTime":
                    record.CreateTime = value;
                    if (TryConvertUnixTime(value, out DateTime dt))
                    {
                        record.Date = dt.ToString("dd.MM.yyyy");
                        record.Time = dt.ToString("HH:mm:ss");
                    }
                    break;
                case "CreateTimeRealUTC":
                    record.CreateTimeRealUtc = value;
                    break;
                case "Type":
                    record.RawEvent = value;
                    break;
                case "AttendanceState":
                    record.AttendanceState = TranslateAttendanceState(value);
                    break;
                case "Method":
                    record.Method = TranslateMethod(value);
                    break;
                case "Door":
                    record.Door = value;
                    break;
                case "ReaderID":
                    record.ReaderId = value;
                    break;
                case "Status":
                    record.Status = value;
                    break;
            }
        }

        foreach (AttendanceRecord record in records.Values)
        {
            record.Event = controller.AttendanceRole switch
            {
                AttendanceRole.Entry => "Вход",
                AttendanceRole.Exit => "Выход",
                _ => TranslateEvent(record.RawEvent)
            };
        }

        return records.Values.ToList();
    }

    private static void BuildAttendanceResults(List<AttendanceRecord> records)
    {
        foreach (AttendanceRecord record in records)
        {
            record.AttendanceResult = "";
            record.EntryTime = "";
            record.ExitTime = "";
            record.WorkTime = "";
        }

        foreach (var group in records
                     .Where(x => !string.IsNullOrWhiteSpace(x.UserId))
                     .GroupBy(x => new { x.UserId, x.Date }))
        {
            AttendanceRecord? currentEntry = null;

            foreach (AttendanceRecord record in group.OrderBy(x => x.CreateTimeValue))
            {
                if (record.Event == "Вход")
                {
                    if (currentEntry != null)
                        currentEntry.AttendanceResult = "Нет выхода";

                    currentEntry = record;
                    currentEntry.EntryTime = record.Time;
                    currentEntry.AttendanceResult = "В работе";
                }
                else if (record.Event == "Выход")
                {
                    if (currentEntry == null)
                    {
                        record.AttendanceResult = "Нет входа";
                        continue;
                    }

                    if (TryConvertUnixTime(currentEntry.CreateTime, out DateTime entry) &&
                        TryConvertUnixTime(record.CreateTime, out DateTime exit))
                    {
                        TimeSpan work = exit - entry;
                        if (work >= TimeSpan.Zero)
                        {
                            currentEntry.ExitTime = record.Time;
                            currentEntry.WorkTime = FormatTime(work);
                            currentEntry.AttendanceResult = "Завершено";
                        }
                    }

                    currentEntry = null;
                }
            }

            if (currentEntry != null)
                currentEntry.AttendanceResult = "Нет выхода";
        }
    }

    private void BuildSummary(List<AttendanceRecord> records)
    {
        _summary.Clear();

        foreach (var group in records
                     .Where(x => !string.IsNullOrWhiteSpace(x.UserId))
                     .GroupBy(x => new { x.Date, x.UserId, x.UserName })
                     .OrderBy(x => x.Key.Date)
                     .ThenBy(x => x.Key.UserName))
        {
            List<AttendanceRecord> events =
                group.OrderBy(x => x.CreateTimeValue).ToList();

            AttendanceRecord? firstEntry =
                events.FirstOrDefault(x => x.Event == "Вход");

            AttendanceRecord? lastExit =
                events.LastOrDefault(x => x.Event == "Выход");

            TimeSpan workTotal = TimeSpan.Zero;
            TimeSpan absenceTotal = TimeSpan.Zero;

            DateTime? currentEntry = null;
            DateTime? lastExitTime = null;

            foreach (AttendanceRecord record in events)
            {
                if (!TryConvertUnixTime(record.CreateTime, out DateTime time))
                    continue;

                if (record.Event == "Вход")
                {
                    if (currentEntry == null)
                    {
                        currentEntry = time;
                    }
                    else if (lastExitTime != null)
                    {
                        TimeSpan absence = time - lastExitTime.Value;
                        if (absence >= TimeSpan.Zero)
                            absenceTotal += absence;

                        currentEntry = time;
                        lastExitTime = null;
                    }
                }
                else if (record.Event == "Выход")
                {
                    if (currentEntry != null && lastExitTime == null)
                    {
                        TimeSpan work = time - currentEntry.Value;
                        if (work >= TimeSpan.Zero)
                        {
                            workTotal += work;
                            lastExitTime = time;
                        }
                    }
                }
            }

            string result =
                firstEntry == null && lastExit != null ? "Нет входа" :
                firstEntry != null && lastExit == null ? "Нет выхода" :
                firstEntry != null ? "Завершено" :
                "Нет данных";

            _summary.Add(new AttendanceSummary
            {
                Date = group.Key.Date,
                UserId = group.Key.UserId,
                UserName = group.Key.UserName,
                FirstEntry = firstEntry?.Time ?? "",
                LastExit = lastExit?.Time ?? "",
                WorkTime = FormatTime(workTotal),
                AbsenceTime = FormatTime(absenceTotal),
                Result = result
            });
        }
    }

    private void ExportExcel_Click(object sender, RoutedEventArgs e)
    {
        if (_summary.Count == 0)
        {
            MessageBox.Show("Сначала сформируйте отчёт.",
                "Экспорт Excel", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Сохранить отчёт Excel",
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = $"Посещаемость_{DateTime.Now:yyyy-MM-dd_HH-mm}.xlsx"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            using var workbook = new XLWorkbook();

            var summary = workbook.Worksheets.Add("Итоги");

            string[] headers =
            {
                "Дата", "UserID", "Сотрудник", "Пришёл",
                "Ушёл", "Рабочее время", "Отсутствовал", "Статус"
            };

            for (int i = 0; i < headers.Length; i++)
                summary.Cell(1, i + 1).Value = headers[i];

            int row = 2;
            foreach (AttendanceSummary item in _summary)
            {
                summary.Cell(row, 1).Value = item.Date;
                summary.Cell(row, 2).Value = item.UserId;
                summary.Cell(row, 3).Value = item.UserName;
                summary.Cell(row, 4).Value = item.FirstEntry;
                summary.Cell(row, 5).Value = item.LastExit;
                summary.Cell(row, 6).Value = item.WorkTime;
                summary.Cell(row, 7).Value = item.AbsenceTime;
                summary.Cell(row, 8).Value = item.Result;
                row++;
            }

            var table = summary.Range(1, 1, row - 1, headers.Length).CreateTable();
            table.Theme = XLTableTheme.TableStyleMedium2;
            summary.SheetView.FreezeRows(1);
            summary.Columns().AdjustToContents();

            var journal = workbook.Worksheets.Add("Журнал");

            string[] journalHeaders =
            {
                "Дата", "Время", "UserID", "Сотрудник",
                "Контроллер", "Событие", "Статус"
            };

            for (int i = 0; i < journalHeaders.Length; i++)
                journal.Cell(1, i + 1).Value = journalHeaders[i];

            row = 2;
            foreach (AttendanceRecord item in _records)
            {
                journal.Cell(row, 1).Value = item.Date;
                journal.Cell(row, 2).Value = item.Time;
                journal.Cell(row, 3).Value = item.UserId;
                journal.Cell(row, 4).Value = item.UserName;
                journal.Cell(row, 5).Value = item.ControllerName;
                journal.Cell(row, 6).Value = item.Event;
                journal.Cell(row, 7).Value = item.AttendanceResult;
                row++;
            }

            if (row > 2)
            {
                var journalTable =
                    journal.Range(1, 1, row - 1, journalHeaders.Length).CreateTable();
                journalTable.Theme = XLTableTheme.TableStyleMedium2;
            }

            journal.SheetView.FreezeRows(1);
            journal.Columns().AdjustToContents();

            workbook.SaveAs(dialog.FileName);

            MessageBox.Show("Excel-файл сохранён.",
                "Экспорт Excel", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(),
                "Ошибка экспорта Excel", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SelectTimesheetEmployees_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_allLoadedRecords.Count == 0)
        {
            MessageBox.Show(
                "Сначала сформируйте отчёт.",
                "Сотрудники табеля",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var employees = _allLoadedRecords
            .Where(x => !string.IsNullOrWhiteSpace(x.UserId))
            .GroupBy(x => x.UserId)
            .Select(g => new EmployeeSelectionItem
            {
                UserId = g.Key,
                UserName = g
                    .Select(x => x.UserName)
                    .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
                    ?? g.Key
            })
            .OrderBy(x => x.UserName)
            .ThenBy(x => x.UserId)
            .ToList();

        if (employees.Count == 0)
        {
            MessageBox.Show(
                "В сформированном отчёте нет сотрудников.",
                "Сотрудники табеля",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        // При первом открытии по умолчанию отмечаем всех.
        if (_timesheetSelectedUserIds.Count == 0)
        {
            foreach (EmployeeSelectionItem employee in employees)
                _timesheetSelectedUserIds.Add(employee.UserId);
        }

        var window = new TimesheetEmployeeSelectionWindow(
            employees,
            _timesheetSelectedUserIds)
        {
            Owner = this
        };

        if (window.ShowDialog() != true)
            return;

        _timesheetSelectedUserIds.Clear();

        foreach (string userId in window.SelectedUserIds)
            _timesheetSelectedUserIds.Add(userId);

        TimesheetEmployeesButton.Content =
            $"Сотрудники табеля ({_timesheetSelectedUserIds.Count})";
    }

    private void ExportTimesheetExcel_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_allLoadedRecords.Count == 0)
        {
            MessageBox.Show(
                "Сначала сформируйте отчёт.",
                "Табель",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (_timesheetSelectedUserIds.Count == 0)
        {
            MessageBox.Show(
                "Сначала нажмите «Сотрудники табеля» и отметьте сотрудников.",
                "Табель",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (DateFromPicker.SelectedDate == null ||
            DateToPicker.SelectedDate == null)
        {
            MessageBox.Show(
                "Выберите период.",
                "Табель",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        DateTime dateFrom =
            DateFromPicker.SelectedDate.Value.Date;

        DateTime dateTo =
            DateToPicker.SelectedDate.Value.Date;

        if (dateFrom > dateTo)
        {
            MessageBox.Show(
                "Дата начала не может быть позже даты окончания.",
                "Табель",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var records =
            _allLoadedRecords
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.UserId) &&
                    _timesheetSelectedUserIds.Contains(x.UserId))
                .ToList();

        if (records.Count == 0)
        {
            MessageBox.Show(
                "Для выбранных сотрудников нет данных.",
                "Табель",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var preview =
            new TimesheetPreviewWindow(
                records,
                _timesheetSelectedUserIds,
                dateFrom,
                dateTo)
            {
                Owner = this
            };

        preview.ShowDialog();
    }


    private static bool TryParseDuration(
        string value,
        out TimeSpan time)
    {
        time = TimeSpan.Zero;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        string[] parts =
            value.Split(':');

        if (parts.Length != 3)
            return false;

        if (!int.TryParse(
                parts[0],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int hours) ||
            !int.TryParse(
                parts[1],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int minutes) ||
            !int.TryParse(
                parts[2],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int seconds))
        {
            return false;
        }

        try
        {
            time = new TimeSpan(
                hours,
                minutes,
                seconds);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string SanitizeFileName(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Сотрудник";

        foreach (char c in
                 System.IO.Path.GetInvalidFileNameChars())
        {
            value = value.Replace(c, '_');
        }

        return value.Trim();
    }

    private void ExportPdf_Click(object sender, RoutedEventArgs e)
    {
        if (_summary.Count == 0)
        {
            MessageBox.Show("Сначала сформируйте отчёт.",
                "Экспорт PDF", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Сохранить отчёт PDF",
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = $"Посещаемость_{DateTime.Now:yyyy-MM-dd_HH-mm}.pdf"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            QuestPDF.Settings.License = LicenseType.Community;

            PdfDocument.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PdfPageSizes.A4.Landscape());
                    page.Margin(20);

                    page.Header()
                        .Text("Отчёт о посещаемости")
                        .FontSize(18)
                        .Bold();

                    page.Content().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(55);
                            columns.ConstantColumn(55);
                            columns.RelativeColumn(2);
                            columns.ConstantColumn(55);
                            columns.ConstantColumn(55);
                            columns.ConstantColumn(75);
                            columns.ConstantColumn(75);
                            columns.ConstantColumn(70);
                        });

                        string[] headers =
                        {
                            "Дата", "UserID", "Сотрудник", "Пришёл",
                            "Ушёл", "Работа", "Отсутствие", "Статус"
                        };

                        foreach (string header in headers)
                        {
                            table.Cell()
                                .Background(PdfColors.Grey.Lighten2)
                                .Padding(4)
                                .Text(header)
                                .Bold()
                                .FontSize(8);
                        }

                        foreach (AttendanceSummary item in _summary)
                        {
                            table.Cell().BorderBottom(1).BorderColor(PdfColors.Grey.Lighten2)
                                .Padding(3).Text(item.Date ?? "").FontSize(7);

                            table.Cell().BorderBottom(1).BorderColor(PdfColors.Grey.Lighten2)
                                .Padding(3).Text(item.UserId ?? "").FontSize(7);

                            table.Cell().BorderBottom(1).BorderColor(PdfColors.Grey.Lighten2)
                                .Padding(3).Text(item.UserName ?? "").FontSize(7);

                            table.Cell().BorderBottom(1).BorderColor(PdfColors.Grey.Lighten2)
                                .Padding(3).Text(item.FirstEntry ?? "").FontSize(7);

                            table.Cell().BorderBottom(1).BorderColor(PdfColors.Grey.Lighten2)
                                .Padding(3).Text(item.LastExit ?? "").FontSize(7);

                            table.Cell().BorderBottom(1).BorderColor(PdfColors.Grey.Lighten2)
                                .Padding(3).Text(item.WorkTime ?? "").FontSize(7);

                            table.Cell().BorderBottom(1).BorderColor(PdfColors.Grey.Lighten2)
                                .Padding(3).Text(item.AbsenceTime ?? "").FontSize(7);

                            table.Cell().BorderBottom(1).BorderColor(PdfColors.Grey.Lighten2)
                                .Padding(3).Text(item.Result ?? "").FontSize(7);
                        }
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.Span("Сформировано: ");
                            text.Span(DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
                        });
                });
            }).GeneratePdf(dialog.FileName);

            MessageBox.Show("PDF-файл сохранён.",
                "Экспорт PDF", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(),
                "Ошибка экспорта PDF", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static DateTime ParseReportDate(string value)
    {
        return DateTime.TryParseExact(
            value,
            "dd.MM.yyyy",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime date)
            ? date
            : DateTime.MinValue;
    }

    private static string FormatTime(TimeSpan time)
    {
        if (time < TimeSpan.Zero)
            return "";

        return $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}";
    }

    private static string GetRoleName(AttendanceRole role)
    {
        return role switch
        {
            AttendanceRole.Entry => "Вход",
            AttendanceRole.Exit => "Выход",
            AttendanceRole.Both => "Вход и выход",
            _ => "Не используется"
        };
    }

    private static bool TryConvertUnixTime(
        string value,
        out DateTime dateTime)
    {
        dateTime = default;

        if (!long.TryParse(value, NumberStyles.Integer,
                CultureInfo.InvariantCulture, out long unixTime))
            return false;

        try
        {
            dateTime = DateTimeOffset
                .FromUnixTimeSeconds(unixTime)
                .LocalDateTime;

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string DecodeValue(string value)
    {
        try
        {
            return Uri.UnescapeDataString(value);
        }
        catch
        {
            return value;
        }
    }

    private static string TranslateEvent(string value)
    {
        return value switch
        {
            "Entry" => "Вход",
            "Exit" => "Выход",
            _ => value
        };
    }

    private static string TranslateAttendanceState(string value)
    {
        return value switch
        {
            "0" => "Нет",
            "1" => "Присутствует",
            _ => value
        };
    }

    private static string TranslateMethod(string value)
    {
        return value switch
        {
            "0" => "Неизвестно",
            "1" => "Карта",
            "2" => "Пароль",
            "3" => "Отпечаток",
            "4" => "Лицо",
            _ => value
        };
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}

public class ControllerSelectionItem
{
    public string Name { get; set; } = "";
    public ControllerInfo? Controller { get; set; }
}

public class EmployeeSelectionItem
{
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";

    public string DisplayName =>
        string.IsNullOrWhiteSpace(UserId)
            ? "Все сотрудники"
            : $"{UserName} ({UserId})";
}

public class AttendanceRecord
{
    public string RecNo { get; set; } = "";
    public string Date { get; set; } = "";
    public string Time { get; set; } = "";
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string ControllerName { get; set; } = "";

    public string ControllerIp { get; set; } = "";
    public AttendanceRole ControllerRole { get; set; }
    public string Event { get; set; } = "";
    public string RawEvent { get; set; } = "";
    public string AttendanceState { get; set; } = "";
    public string Method { get; set; } = "";
    public string Door { get; set; } = "";
    public string ReaderId { get; set; } = "";
    public string Status { get; set; } = "";
    public string EntryTime { get; set; } = "";
    public string ExitTime { get; set; } = "";
    public string WorkTime { get; set; } = "";
    public string AttendanceResult { get; set; } = "";
    public string CreateTime { get; set; } = "";
    public string CreateTimeRealUtc { get; set; } = "";

    public long CreateTimeValue =>
        long.TryParse(CreateTime, NumberStyles.Integer,
            CultureInfo.InvariantCulture, out long value)
            ? value
            : 0;
}

public class AttendanceSummary
{
    public string Date { get; set; } = "";
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string FirstEntry { get; set; } = "";
    public string LastExit { get; set; } = "";
    public string WorkTime { get; set; } = "";
    public string AbsenceTime { get; set; } = "";
    public string Result { get; set; } = "";
}