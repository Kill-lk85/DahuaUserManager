using ClosedXML.Excel;
using DahuaUserManager.Models.Schedules;
using DahuaUserManager.UI.Database;
using Microsoft.Win32;
using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace DahuaUserManager.UI.Windows;

public partial class TimesheetPreviewWindow : Window
{
    private readonly List<AttendanceRecord> _records;
    private readonly HashSet<string> _selectedUserIds;
    private readonly DateTime _dateFrom;
    private readonly DateTime _dateTo;

    private readonly AttendanceDatabase _database = new();
    private readonly ScheduleRepository _scheduleRepository;

    private readonly List<TimesheetMonthPreview>
        _monthPreviews = new();

    public TimesheetPreviewWindow(
        IEnumerable<AttendanceRecord> records,
        IEnumerable<string> selectedUserIds,
        DateTime dateFrom,
        DateTime dateTo)
    {
        InitializeComponent();

        _records = records.ToList();

        _selectedUserIds =
            selectedUserIds.ToHashSet(
                StringComparer.OrdinalIgnoreCase);

        _dateFrom = dateFrom.Date;
        _dateTo = dateTo.Date;

        _scheduleRepository =
            new ScheduleRepository(_database);

        PeriodText.Text =
            $"Период: {_dateFrom:dd.MM.yyyy} — {_dateTo:dd.MM.yyyy}";

        Loaded += TimesheetPreviewWindow_Loaded;
    }

    private async void TimesheetPreviewWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            Mouse.OverrideCursor = Cursors.Wait;

            await _database.InitializeAsync();
            await BuildPreviewAsync();

            LoadingText.Visibility =
                Visibility.Collapsed;

            MonthsTabControl.Visibility =
                Visibility.Visible;

            SaveExcelButton.IsEnabled =
                _monthPreviews.Count > 0;
        }
        catch (Exception ex)
        {
            LoadingText.Text =
                "Не удалось сформировать предпросмотр.";

            MessageBox.Show(
                ex.ToString(),
                "Ошибка формирования табеля",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private async Task BuildPreviewAsync()
    {
        _monthPreviews.Clear();
        MonthsTabControl.Items.Clear();

        var employees =
            _records
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.UserId) &&
                    _selectedUserIds.Contains(x.UserId))
                .GroupBy(x => x.UserId)
                .Select(g => new TimesheetEmployee
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

        CultureInfo ru =
            CultureInfo.GetCultureInfo("ru-RU");

        DateTime monthCursor =
            new DateTime(
                _dateFrom.Year,
                _dateFrom.Month,
                1);

        DateTime lastMonth =
            new DateTime(
                _dateTo.Year,
                _dateTo.Month,
                1);

        while (monthCursor <= lastMonth)
        {
            TimesheetMonthPreview preview =
                await BuildMonthAsync(
                    monthCursor,
                    employees);

            _monthPreviews.Add(preview);

            string monthName =
                ru.DateTimeFormat
                    .GetMonthName(monthCursor.Month);

            monthName =
                char.ToUpper(monthName[0], ru) +
                monthName[1..];

            var grid =
                CreatePreviewGrid(
                    preview);

            var tab =
                new TabItem
                {
                    Header =
                        $"{monthName} {monthCursor.Year}",

                    Content = grid
                };

            MonthsTabControl.Items.Add(tab);

            monthCursor =
                monthCursor.AddMonths(1);
        }

        if (MonthsTabControl.Items.Count > 0)
            MonthsTabControl.SelectedIndex = 0;
    }

    private async Task<TimesheetMonthPreview>
        BuildMonthAsync(
            DateTime month,
            List<TimesheetEmployee> employees)
    {
        int daysInMonth =
            DateTime.DaysInMonth(
                month.Year,
                month.Month);

        DateTime monthStart =
            new DateTime(
                month.Year,
                month.Month,
                1);

        DateTime monthEnd =
            monthStart
                .AddMonths(1)
                .AddDays(-1);

        DateTime effectiveFrom =
            _dateFrom > monthStart
                ? _dateFrom
                : monthStart;

        DateTime effectiveTo =
            _dateTo < monthEnd
                ? _dateTo
                : monthEnd;

        var preview =
            new TimesheetMonthPreview
            {
                Month = month,
                EffectiveFrom = effectiveFrom,
                EffectiveTo = effectiveTo,
                DaysInMonth = daysInMonth
            };

        foreach (TimesheetEmployee employee
                 in employees)
        {
            var row =
                new TimesheetPreviewRow
                {
                    UserId = employee.UserId,
                    UserName = employee.UserName
                };

            for (int day = 1;
                 day <= daysInMonth;
                 day++)
            {
                DateTime date =
                    new DateTime(
                        month.Year,
                        month.Month,
                        day);

                if (date < effectiveFrom ||
                    date > effectiveTo)
                {
                    continue;
                }

                List<AttendanceRecord> dayEvents =
                    _records
                        .Where(x =>
                            x.UserId == employee.UserId &&
                            ParseReportDate(x.Date) == date)
                        .OrderBy(x =>
                            x.CreateTimeValue)
                        .ToList();

                AttendanceRecord? firstEntry =
                    dayEvents.FirstOrDefault(
                        x => x.Event == "Вход");

                AttendanceRecord? lastExit =
                    dayEvents.LastOrDefault(
                        x => x.Event == "Выход");

                if (firstEntry == null ||
                    lastExit == null)
                {
                    continue;
                }

                if (!TryConvertUnixTime(
                        firstEntry.CreateTime,
                        out DateTime entryTime) ||
                    !TryConvertUnixTime(
                        lastExit.CreateTime,
                        out DateTime exitTime))
                {
                    continue;
                }

                TimeSpan presence =
                    exitTime - entryTime;

                if (presence <= TimeSpan.Zero)
                    continue;

                WorkSchedule? schedule =
                    await _scheduleRepository
                        .GetScheduleForEmployeeAsync(
                            employee.UserId,
                            date);

                double normHours =
                    schedule?.NormHours ?? 8;

                if (normHours <= 0)
                    normHours = 8;

                double creditedHours =
                    Math.Round(
                        Math.Min(
                            presence.TotalHours,
                            normHours),
                        2,
                        MidpointRounding.AwayFromZero);

                if (creditedHours <= 0)
                    continue;

                row.DayHours[day] =
                    creditedHours;

                row.TotalHours +=
                    creditedHours;
            }

            row.TotalHours =
                Math.Round(
                    row.TotalHours,
                    2,
                    MidpointRounding.AwayFromZero);

            preview.Rows.Add(row);
        }

        return preview;
    }

    private static DataGrid CreatePreviewGrid(
        TimesheetMonthPreview preview)
    {
        var grid =
            new DataGrid
            {
                AutoGenerateColumns = false,
                IsReadOnly = true,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                SelectionMode =
                    DataGridSelectionMode.Single,

                GridLinesVisibility =
                    DataGridGridLinesVisibility.All,

                FrozenColumnCount = 2
            };

        grid.Columns.Add(
            new DataGridTextColumn
            {
                Header = "№",
                Binding =
                    new Binding(nameof(
                        TimesheetPreviewRow.Number)),
                Width = 45
            });

        grid.Columns.Add(
            new DataGridTextColumn
            {
                Header = "Ф.И.О.",
                Binding =
                    new Binding(nameof(
                        TimesheetPreviewRow.UserName)),
                Width =
                    new DataGridLength(
                        1,
                        DataGridLengthUnitType.Star),

                MinWidth = 190
            });

        for (int day = 1;
             day <= preview.DaysInMonth;
             day++)
        {
            int currentDay = day;

            var column =
                new DataGridTextColumn
                {
                    Header = day.ToString(),
                    Binding =
                        new Binding(
                            $"DayHours[{day}]")
                        {
                            StringFormat = "0.##",
                            TargetNullValue = ""
                        },
                    Width = 42
                };

            DateTime date =
                new DateTime(
                    preview.Month.Year,
                    preview.Month.Month,
                    currentDay);

            if (date < preview.EffectiveFrom ||
                date > preview.EffectiveTo)
            {
                column.Visibility =
                    Visibility.Collapsed;
            }

            grid.Columns.Add(column);
        }

        grid.Columns.Add(
            new DataGridTextColumn
            {
                Header = "Факт. работы",
                Binding =
                    new Binding(nameof(
                        TimesheetPreviewRow.TotalHours))
                    {
                        StringFormat = "0.##"
                    },
                Width = 100
            });

        int number = 1;

        foreach (TimesheetPreviewRow row
                 in preview.Rows)
        {
            row.Number = number++;
        }

        grid.ItemsSource =
            preview.Rows;

        return grid;
    }

    private void SaveExcel_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_monthPreviews.Count == 0)
        {
            MessageBox.Show(
                "Нет данных для сохранения.",
                "Табель Excel",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var dialog =
            new SaveFileDialog
            {
                Title =
                    "Сохранить табель учёта рабочего времени",

                Filter =
                    "Excel (*.xlsx)|*.xlsx",

                FileName =
                    $"Табель_{_dateFrom:yyyy-MM-dd}_{_dateTo:yyyy-MM-dd}.xlsx"
            };

        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            ExportToExcel(
                dialog.FileName);

            MessageBox.Show(
                "Табель Excel сохранён.",
                "Табель Excel",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.ToString(),
                "Ошибка экспорта табеля",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void ExportToExcel(
        string fileName)
    {
        using var workbook =
            new XLWorkbook();

        CultureInfo ru =
            CultureInfo.GetCultureInfo(
                "ru-RU");

        foreach (TimesheetMonthPreview preview
                 in _monthPreviews)
        {
            DateTime month =
                preview.Month;

            string monthName =
                ru.DateTimeFormat
                    .GetMonthName(
                        month.Month);

            monthName =
                char.ToUpper(
                    monthName[0],
                    ru) +
                monthName[1..];

            string sheetName =
                $"{monthName} {month.Year}";

            if (sheetName.Length > 31)
                sheetName = sheetName[..31];

            var sheet =
                workbook.Worksheets
                    .Add(sheetName);

            int colNumber = 1;
            int colName = 2;
            int colGrade = 3;
            int colProfession = 4;
            int firstDayCol = 5;
            int lastDayCol =
                firstDayCol +
                preview.DaysInMonth - 1;

            int colFact =
                lastDayCol + 1;

            int colTrip =
                colFact + 1;

            int colVacation =
                colFact + 2;

            int colSick =
                colFact + 3;

            int colUnpaid =
                colFact + 4;

            int lastCol =
                colUnpaid;

            sheet.Range(
                    1,
                    1,
                    1,
                    lastCol)
                .Merge();

            sheet.Cell(1, 1).Value =
                "ТАБЕЛЬ УЧЁТА РАБОЧЕГО ВРЕМЕНИ";

            sheet.Cell(1, 1)
                .Style.Font.Bold = true;

            sheet.Cell(1, 1)
                .Style.Font.FontSize = 16;

            sheet.Cell(1, 1)
                .Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            sheet.Range(
                    2,
                    1,
                    2,
                    lastCol)
                .Merge();

            sheet.Cell(2, 1).Value =
                $"{monthName} {month.Year} г.   " +
                $"Период данных: " +
                $"{preview.EffectiveFrom:dd.MM.yyyy} — " +
                $"{preview.EffectiveTo:dd.MM.yyyy}";

            sheet.Cell(2, 1)
                .Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            const int headerRow = 4;

            sheet.Cell(
                headerRow,
                colNumber).Value =
                "№ п/п";

            sheet.Cell(
                headerRow,
                colName).Value =
                "Ф.И.О.";

            sheet.Cell(
                headerRow,
                colGrade).Value =
                "Разряд";

            sheet.Cell(
                headerRow,
                colProfession).Value =
                "Профессия";

            for (int day = 1;
                 day <= preview.DaysInMonth;
                 day++)
            {
                int column =
                    firstDayCol +
                    day - 1;

                sheet.Cell(
                    headerRow,
                    column).Value =
                    day;

                DateTime date =
                    new DateTime(
                        month.Year,
                        month.Month,
                        day);

                sheet.Cell(
                        headerRow,
                        column)
                    .Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Center;

                if (date.DayOfWeek ==
                        DayOfWeek.Saturday ||
                    date.DayOfWeek ==
                        DayOfWeek.Sunday)
                {
                    sheet.Cell(
                            headerRow,
                            column)
                        .Style.Fill.BackgroundColor =
                        XLColor.FromHtml(
                            "#D9EAD3");
                }
            }

            sheet.Cell(
                headerRow,
                colFact).Value =
                "Факт. работы";

            sheet.Cell(
                headerRow,
                colTrip).Value =
                "Командировки (К)";

            sheet.Cell(
                headerRow,
                colVacation).Value =
                "Труд. отпуск (О)";

            sheet.Cell(
                headerRow,
                colSick).Value =
                "Болезнь (Б)";

            sheet.Cell(
                headerRow,
                colUnpaid).Value =
                "Отпуск без сохр. з/п (А)";

            sheet.Range(
                    headerRow,
                    1,
                    headerRow,
                    lastCol)
                .Style.Font.Bold = true;

            sheet.Range(
                    headerRow,
                    1,
                    headerRow,
                    lastCol)
                .Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;

            sheet.Range(
                    headerRow,
                    1,
                    headerRow,
                    lastCol)
                .Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            sheet.Range(
                    headerRow,
                    1,
                    headerRow,
                    lastCol)
                .Style.Alignment.WrapText = true;

            int rowNumber =
                headerRow + 1;

            foreach (TimesheetPreviewRow previewRow
                     in preview.Rows)
            {
                sheet.Cell(
                    rowNumber,
                    colNumber).Value =
                    previewRow.Number;

                sheet.Cell(
                    rowNumber,
                    colName).Value =
                    previewRow.UserName;

                sheet.Cell(
                    rowNumber,
                    colGrade).Value =
                    "";

                sheet.Cell(
                    rowNumber,
                    colProfession).Value =
                    "";

                for (int day = 1;
                     day <= preview.DaysInMonth;
                     day++)
                {
                    DateTime date =
                        new DateTime(
                            month.Year,
                            month.Month,
                            day);

                    int column =
                        firstDayCol +
                        day - 1;

                    if (date <
                            preview.EffectiveFrom ||
                        date >
                            preview.EffectiveTo)
                    {
                        continue;
                    }

                    if (!previewRow.DayHours
                        .TryGetValue(
                            day,
                            out double hours))
                    {
                        continue;
                    }

                    sheet.Cell(
                        rowNumber,
                        column).Value =
                        hours;

                    sheet.Cell(
                            rowNumber,
                            column)
                        .Style.NumberFormat.Format =
                        "0.##";

                    sheet.Cell(
                            rowNumber,
                            column)
                        .Style.Alignment.Horizontal =
                        XLAlignmentHorizontalValues.Center;

                    if (date.DayOfWeek ==
                            DayOfWeek.Saturday ||
                        date.DayOfWeek ==
                            DayOfWeek.Sunday)
                    {
                        sheet.Cell(
                                rowNumber,
                                column)
                            .Style.Fill.BackgroundColor =
                            XLColor.FromHtml(
                                "#E2F0D9");
                    }
                }

                sheet.Cell(
                    rowNumber,
                    colFact).Value =
                    previewRow.TotalHours;

                sheet.Cell(
                        rowNumber,
                        colFact)
                    .Style.NumberFormat.Format =
                    "0.##";

                sheet.Cell(
                    rowNumber,
                    colTrip).Value = "";

                sheet.Cell(
                    rowNumber,
                    colVacation).Value = "";

                sheet.Cell(
                    rowNumber,
                    colSick).Value = "";

                sheet.Cell(
                    rowNumber,
                    colUnpaid).Value = "";

                rowNumber++;
            }

            var usedTable =
                sheet.Range(
                    headerRow,
                    1,
                    rowNumber - 1,
                    lastCol);

            usedTable.Style.Border.TopBorder =
                XLBorderStyleValues.Thin;

            usedTable.Style.Border.BottomBorder =
                XLBorderStyleValues.Thin;

            usedTable.Style.Border.LeftBorder =
                XLBorderStyleValues.Thin;

            usedTable.Style.Border.RightBorder =
                XLBorderStyleValues.Thin;

            usedTable.Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;

            sheet.Column(colNumber).Width = 7;
            sheet.Column(colName).Width = 28;
            sheet.Column(colGrade).Width = 10;
            sheet.Column(colProfession).Width = 22;

            for (int column = firstDayCol;
                 column <= lastDayCol;
                 column++)
            {
                sheet.Column(column).Width = 4.5;
            }

            sheet.Column(colFact).Width = 12;
            sheet.Column(colTrip).Width = 14;
            sheet.Column(colVacation).Width = 14;
            sheet.Column(colSick).Width = 12;
            sheet.Column(colUnpaid).Width = 18;

            sheet.Row(headerRow).Height = 42;

            sheet.SheetView.FreezeRows(
                headerRow);

            sheet.SheetView.FreezeColumns(
                colProfession);

            sheet.PageSetup.PageOrientation =
                XLPageOrientation.Landscape;

            sheet.PageSetup.FitToPages(
                1,
                0);

            sheet.PageSetup.Margins.Top = 0.25;
            sheet.PageSetup.Margins.Bottom = 0.25;
            sheet.PageSetup.Margins.Left = 0.2;
            sheet.PageSetup.Margins.Right = 0.2;
        }

        workbook.SaveAs(
            fileName);
    }

    private static DateTime ParseReportDate(
        string value)
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

    private static bool TryConvertUnixTime(
        string value,
        out DateTime dateTime)
    {
        dateTime = default;

        if (!long.TryParse(
                value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out long unixTime))
        {
            return false;
        }

        try
        {
            dateTime =
                DateTimeOffset
                    .FromUnixTimeSeconds(
                        unixTime)
                    .LocalDateTime;

            return true;
        }
        catch
        {
            return false;
        }
    }
}

public class TimesheetMonthPreview
{
    public DateTime Month { get; set; }

    public DateTime EffectiveFrom { get; set; }

    public DateTime EffectiveTo { get; set; }

    public int DaysInMonth { get; set; }

    public List<TimesheetPreviewRow> Rows { get; } =
        new();
}

public class TimesheetPreviewRow
{
    public int Number { get; set; }

    public string UserId { get; set; } = "";

    public string UserName { get; set; } = "";

    public Dictionary<int, double> DayHours { get; } =
        new();

    public double TotalHours { get; set; }
}

internal class TimesheetEmployee
{
    public string UserId { get; set; } = "";

    public string UserName { get; set; } = "";
}