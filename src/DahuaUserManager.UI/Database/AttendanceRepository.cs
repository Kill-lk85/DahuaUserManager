using DahuaUserManager.Models.Database;
using Microsoft.Data.Sqlite;

namespace DahuaUserManager.UI.Database;

public class AttendanceRepository
{
    private readonly AttendanceDatabase _database;

    public AttendanceRepository(
        AttendanceDatabase database)
    {
        _database = database;
    }

    public async Task<bool> InsertIfNotExistsAsync(
        AttendanceEventEntity item)
    {
        await using var connection =
            new SqliteConnection(
                _database.ConnectionString);

        await connection.OpenAsync();

        string sql = """
            INSERT OR IGNORE INTO AttendanceEvents
            (
                RecNo,
                UserId,
                UserName,
                ControllerName,
                ControllerIp,
                Direction,
                EventTime,
                EventUnixTime,
                EventType
            )
            VALUES
            (
                $RecNo,
                $UserId,
                $UserName,
                $ControllerName,
                $ControllerIp,
                $Direction,
                $EventTime,
                $EventUnixTime,
                $EventType
            );
            """;

        await using var command =
            connection.CreateCommand();

        command.CommandText = sql;

        command.Parameters.AddWithValue(
            "$RecNo",
            item.RecNo);

        command.Parameters.AddWithValue(
            "$UserId",
            item.UserId);

        command.Parameters.AddWithValue(
            "$UserName",
            item.UserName);

        command.Parameters.AddWithValue(
            "$ControllerName",
            item.ControllerName);

        command.Parameters.AddWithValue(
            "$ControllerIp",
            item.ControllerIp);

        command.Parameters.AddWithValue(
            "$Direction",
            item.Direction);

        command.Parameters.AddWithValue(
            "$EventTime",
            item.EventTime.ToString("O"));

        command.Parameters.AddWithValue(
            "$EventUnixTime",
            item.EventUnixTime);

        command.Parameters.AddWithValue(
            "$EventType",
            item.EventType);

        int affected =
            await command.ExecuteNonQueryAsync();

        return affected > 0;
    }

    public async Task<List<AttendanceEventEntity>>
        GetByPeriodAsync(
            DateTime dateFrom,
            DateTime dateTo)
    {
        var result =
            new List<AttendanceEventEntity>();

        await using var connection =
            new SqliteConnection(
                _database.ConnectionString);

        await connection.OpenAsync();

        // Фильтруем по Unix timestamp.
        // Это надёжнее, чем сравнивать даты,
        // сохранённые в SQLite как текст.
        long unixFrom =
            new DateTimeOffset(
                DateTime.SpecifyKind(
                    dateFrom,
                    DateTimeKind.Local))
            .ToUnixTimeSeconds();

        long unixTo =
            new DateTimeOffset(
                DateTime.SpecifyKind(
                    dateTo,
                    DateTimeKind.Local))
            .ToUnixTimeSeconds();

        string sql = """
            SELECT
                Id,
                RecNo,
                UserId,
                UserName,
                ControllerName,
                ControllerIp,
                Direction,
                EventTime,
                EventUnixTime,
                EventType
            FROM AttendanceEvents
            WHERE EventUnixTime >= $UnixFrom
              AND EventUnixTime <= $UnixTo
            ORDER BY EventUnixTime;
            """;

        await using var command =
            connection.CreateCommand();

        command.CommandText = sql;

        command.Parameters.AddWithValue(
            "$UnixFrom",
            unixFrom);

        command.Parameters.AddWithValue(
            "$UnixTo",
            unixTo);

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var item =
                new AttendanceEventEntity
                {
                    Id =
                        reader.GetInt64(0),

                    RecNo =
                        reader.GetString(1),

                    UserId =
                        reader.GetString(2),

                    UserName =
                        reader.GetString(3),

                    ControllerName =
                        reader.GetString(4),

                    ControllerIp =
                        reader.GetString(5),

                    Direction =
                        reader.GetString(6),

                    EventTime =
                        DateTime.Parse(
                            reader.GetString(7),
                            null,
                            System.Globalization.DateTimeStyles.RoundtripKind),

                    EventUnixTime =
                        reader.GetInt64(8),

                    EventType =
                        reader.GetString(9)
                };

            result.Add(item);
        }

        return result;
    }

    public async Task<int> GetCountAsync()
    {
        await using var connection =
            new SqliteConnection(
                _database.ConnectionString);

        await connection.OpenAsync();

        await using var command =
            connection.CreateCommand();

        command.CommandText =
            "SELECT COUNT(*) FROM AttendanceEvents;";

        object? value =
            await command.ExecuteScalarAsync();

        return Convert.ToInt32(value);
    }
}