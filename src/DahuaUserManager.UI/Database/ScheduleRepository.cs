using DahuaUserManager.Models.Schedules;
using Microsoft.Data.Sqlite;
using System.Globalization;

namespace DahuaUserManager.UI.Database;

public class ScheduleRepository
{
    private readonly AttendanceDatabase _database;

    public ScheduleRepository(
        AttendanceDatabase database)
    {
        _database = database;
    }

    /// <summary>
    /// Получить все графики работы.
    /// </summary>
    public async Task<List<WorkSchedule>> GetSchedulesAsync()
    {
        var result = new List<WorkSchedule>();

        await using var connection =
            new SqliteConnection(
                _database.ConnectionString);

        await connection.OpenAsync();

        await using var command =
            connection.CreateCommand();

        command.CommandText = """
            SELECT
                Id,
                Name,
                StartTime,
                EndTime,
                NormHours,
                IsActive
            FROM WorkSchedules
            ORDER BY Name;
            """;

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(
                new WorkSchedule
                {
                    Id = reader.GetInt64(0),

                    Name = reader.GetString(1),

                    StartTime = TimeSpan.Parse(
                        reader.GetString(2),
                        CultureInfo.InvariantCulture),

                    EndTime = TimeSpan.Parse(
                        reader.GetString(3),
                        CultureInfo.InvariantCulture),

                    NormHours = reader.GetDouble(4),

                    IsActive =
                        reader.GetInt64(5) != 0
                });
        }

        return result;
    }

    /// <summary>
    /// Создать новый график или сохранить изменения существующего.
    /// Возвращает ID графика.
    /// </summary>
    public async Task<long> SaveScheduleAsync(
        WorkSchedule schedule)
    {
        await using var connection =
            new SqliteConnection(
                _database.ConnectionString);

        await connection.OpenAsync();

        await using var command =
            connection.CreateCommand();

        if (schedule.Id <= 0)
        {
            command.CommandText = """
                INSERT INTO WorkSchedules
                (
                    Name,
                    StartTime,
                    EndTime,
                    NormHours,
                    IsActive
                )
                VALUES
                (
                    $Name,
                    $StartTime,
                    $EndTime,
                    $NormHours,
                    $IsActive
                );

                SELECT last_insert_rowid();
                """;
        }
        else
        {
            command.CommandText = """
                UPDATE WorkSchedules
                SET
                    Name = $Name,
                    StartTime = $StartTime,
                    EndTime = $EndTime,
                    NormHours = $NormHours,
                    IsActive = $IsActive
                WHERE Id = $Id;

                SELECT $Id;
                """;

            command.Parameters.AddWithValue(
                "$Id",
                schedule.Id);
        }

        command.Parameters.AddWithValue(
            "$Name",
            schedule.Name);

        command.Parameters.AddWithValue(
            "$StartTime",
            schedule.StartTime.ToString(
                @"hh\:mm"));

        command.Parameters.AddWithValue(
            "$EndTime",
            schedule.EndTime.ToString(
                @"hh\:mm"));

        command.Parameters.AddWithValue(
            "$NormHours",
            schedule.NormHours);

        command.Parameters.AddWithValue(
            "$IsActive",
            schedule.IsActive ? 1 : 0);

        object? value =
            await command.ExecuteScalarAsync();

        return Convert.ToInt64(value);
    }

    /// <summary>
    /// Удалить график работы.
    /// Если график назначен сотрудникам,
    /// SQLite не позволит удалить его из-за внешнего ключа.
    /// </summary>
    public async Task DeleteScheduleAsync(
        long scheduleId)
    {
        await using var connection =
            new SqliteConnection(
                _database.ConnectionString);

        await connection.OpenAsync();

        // В SQLite foreign_keys включается
        // отдельно для каждого соединения.
        await using (var pragma =
                     connection.CreateCommand())
        {
            pragma.CommandText =
                "PRAGMA foreign_keys = ON;";

            await pragma.ExecuteNonQueryAsync();
        }

        await using var command =
            connection.CreateCommand();

        command.CommandText = """
            DELETE FROM WorkSchedules
            WHERE Id = $Id;
            """;

        command.Parameters.AddWithValue(
            "$Id",
            scheduleId);

        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Назначить сотруднику график начиная с указанной даты.
    ///
    /// Если у сотрудника уже есть действующий график,
    /// его период автоматически заканчивается
    /// за день до начала нового графика.
    /// </summary>
    public async Task AssignScheduleAsync(
        string userId,
        long scheduleId,
        DateTime dateFrom,
        DateTime? dateTo = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException(
                "UserID сотрудника не указан.",
                nameof(userId));
        }

        if (scheduleId <= 0)
        {
            throw new ArgumentException(
                "Не выбран график работы.",
                nameof(scheduleId));
        }

        dateFrom = dateFrom.Date;

        if (dateTo.HasValue)
            dateTo = dateTo.Value.Date;

        if (dateTo.HasValue &&
            dateTo.Value < dateFrom)
        {
            throw new ArgumentException(
                "Дата окончания графика не может быть раньше даты начала.");
        }

        await using var connection =
            new SqliteConnection(
                _database.ConnectionString);

        await connection.OpenAsync();

        // Здесь используем именно SqliteTransaction.
        // Это устраняет ошибку DbTransaction -> SqliteTransaction.
        using SqliteTransaction transaction =
            connection.BeginTransaction();

        try
        {
            // --------------------------------------------------------
            // Закрываем предыдущее открытое назначение сотрудника.
            // --------------------------------------------------------

            await using (var closeCommand =
                         connection.CreateCommand())
            {
                closeCommand.Transaction =
                    transaction;

                closeCommand.CommandText = """
                    UPDATE EmployeeSchedules
                    SET DateTo = $PreviousDate
                    WHERE UserId = $UserId
                      AND DateTo IS NULL
                      AND DateFrom < $DateFrom;
                    """;

                closeCommand.Parameters.AddWithValue(
                    "$PreviousDate",
                    dateFrom
                        .AddDays(-1)
                        .ToString("yyyy-MM-dd"));

                closeCommand.Parameters.AddWithValue(
                    "$UserId",
                    userId);

                closeCommand.Parameters.AddWithValue(
                    "$DateFrom",
                    dateFrom.ToString(
                        "yyyy-MM-dd"));

                await closeCommand
                    .ExecuteNonQueryAsync();
            }

            // --------------------------------------------------------
            // Если на эту же дату уже было назначение,
            // удаляем его, чтобы не получить два графика одновременно.
            // --------------------------------------------------------

            await using (var deleteSameDateCommand =
                         connection.CreateCommand())
            {
                deleteSameDateCommand.Transaction =
                    transaction;

                deleteSameDateCommand.CommandText = """
                    DELETE FROM EmployeeSchedules
                    WHERE UserId = $UserId
                      AND DateFrom = $DateFrom;
                    """;

                deleteSameDateCommand.Parameters.AddWithValue(
                    "$UserId",
                    userId);

                deleteSameDateCommand.Parameters.AddWithValue(
                    "$DateFrom",
                    dateFrom.ToString(
                        "yyyy-MM-dd"));

                await deleteSameDateCommand
                    .ExecuteNonQueryAsync();
            }

            // --------------------------------------------------------
            // Добавляем новое назначение.
            // --------------------------------------------------------

            await using (var insertCommand =
                         connection.CreateCommand())
            {
                insertCommand.Transaction =
                    transaction;

                insertCommand.CommandText = """
                    INSERT INTO EmployeeSchedules
                    (
                        UserId,
                        ScheduleId,
                        DateFrom,
                        DateTo
                    )
                    VALUES
                    (
                        $UserId,
                        $ScheduleId,
                        $DateFrom,
                        $DateTo
                    );
                    """;

                insertCommand.Parameters.AddWithValue(
                    "$UserId",
                    userId);

                insertCommand.Parameters.AddWithValue(
                    "$ScheduleId",
                    scheduleId);

                insertCommand.Parameters.AddWithValue(
                    "$DateFrom",
                    dateFrom.ToString(
                        "yyyy-MM-dd"));

                insertCommand.Parameters.AddWithValue(
                    "$DateTo",
                    dateTo.HasValue
                        ? dateTo.Value.ToString(
                            "yyyy-MM-dd")
                        : DBNull.Value);

                await insertCommand
                    .ExecuteNonQueryAsync();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Получить график сотрудника,
    /// действующий на конкретную дату.
    /// </summary>
    public async Task<WorkSchedule?>
        GetScheduleForEmployeeAsync(
            string userId,
            DateTime date)
    {
        await using var connection =
            new SqliteConnection(
                _database.ConnectionString);

        await connection.OpenAsync();

        await using var command =
            connection.CreateCommand();

        command.CommandText = """
            SELECT
                w.Id,
                w.Name,
                w.StartTime,
                w.EndTime,
                w.NormHours,
                w.IsActive
            FROM EmployeeSchedules e

            INNER JOIN WorkSchedules w
                ON w.Id = e.ScheduleId

            WHERE e.UserId = $UserId

              AND e.DateFrom <= $Date

              AND
              (
                  e.DateTo IS NULL
                  OR e.DateTo >= $Date
              )

            ORDER BY e.DateFrom DESC

            LIMIT 1;
            """;

        command.Parameters.AddWithValue(
            "$UserId",
            userId);

        command.Parameters.AddWithValue(
            "$Date",
            date.Date.ToString(
                "yyyy-MM-dd"));

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return null;

        return new WorkSchedule
        {
            Id = reader.GetInt64(0),

            Name = reader.GetString(1),

            StartTime = TimeSpan.Parse(
                reader.GetString(2),
                CultureInfo.InvariantCulture),

            EndTime = TimeSpan.Parse(
                reader.GetString(3),
                CultureInfo.InvariantCulture),

            NormHours = reader.GetDouble(4),

            IsActive =
                reader.GetInt64(5) != 0
        };
    }

    /// <summary>
    /// Удалить конкретное назначение графика сотруднику.
    /// Удаляется только запись EmployeeSchedules.
    /// Сам график WorkSchedules не удаляется.
    /// </summary>
    public async Task<bool> DeleteEmployeeAssignmentAsync(
        long assignmentId)
    {
        if (assignmentId <= 0)
            return false;

        await using var connection =
            new SqliteConnection(
                _database.ConnectionString);

        await connection.OpenAsync();

        await using var command =
            connection.CreateCommand();

        command.CommandText = """
            DELETE FROM EmployeeSchedules
            WHERE Id = $Id;
            """;

        command.Parameters.AddWithValue(
            "$Id",
            assignmentId);

        int affected =
            await command.ExecuteNonQueryAsync();

        return affected > 0;
    }


    /// <summary>
    /// Получить историю назначений графиков сотрудника.
    /// </summary>
    public async Task<List<EmployeeSchedule>>
        GetEmployeeAssignmentsAsync(
            string userId)
    {
        var result =
            new List<EmployeeSchedule>();

        await using var connection =
            new SqliteConnection(
                _database.ConnectionString);

        await connection.OpenAsync();

        await using var command =
            connection.CreateCommand();

        command.CommandText = """
            SELECT
                Id,
                UserId,
                ScheduleId,
                DateFrom,
                DateTo
            FROM EmployeeSchedules
            WHERE UserId = $UserId
            ORDER BY DateFrom DESC;
            """;

        command.Parameters.AddWithValue(
            "$UserId",
            userId);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(
                new EmployeeSchedule
                {
                    Id =
                        reader.GetInt64(0),

                    UserId =
                        reader.GetString(1),

                    ScheduleId =
                        reader.GetInt64(2),

                    DateFrom =
                        DateTime.ParseExact(
                            reader.GetString(3),
                            "yyyy-MM-dd",
                            CultureInfo.InvariantCulture),

                    DateTo =
                        reader.IsDBNull(4)
                            ? null
                            : DateTime.ParseExact(
                                reader.GetString(4),
                                "yyyy-MM-dd",
                                CultureInfo.InvariantCulture)
                });
        }

        return result;
    }

    /// <summary>
    /// Получить все назначения графиков.
    /// Пригодится для будущего окна управления сотрудниками.
    /// </summary>
    public async Task<List<EmployeeSchedule>>
        GetAllEmployeeAssignmentsAsync()
    {
        var result =
            new List<EmployeeSchedule>();

        await using var connection =
            new SqliteConnection(
                _database.ConnectionString);

        await connection.OpenAsync();

        await using var command =
            connection.CreateCommand();

        command.CommandText = """
            SELECT
                Id,
                UserId,
                ScheduleId,
                DateFrom,
                DateTo
            FROM EmployeeSchedules
            ORDER BY UserId, DateFrom DESC;
            """;

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(
                new EmployeeSchedule
                {
                    Id =
                        reader.GetInt64(0),

                    UserId =
                        reader.GetString(1),

                    ScheduleId =
                        reader.GetInt64(2),

                    DateFrom =
                        DateTime.ParseExact(
                            reader.GetString(3),
                            "yyyy-MM-dd",
                            CultureInfo.InvariantCulture),

                    DateTo =
                        reader.IsDBNull(4)
                            ? null
                            : DateTime.ParseExact(
                                reader.GetString(4),
                                "yyyy-MM-dd",
                                CultureInfo.InvariantCulture)
                });
        }

        return result;
    }
}