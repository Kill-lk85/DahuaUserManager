using DahuaUserManager.Models.Entities;
using Microsoft.Data.Sqlite;

namespace DahuaUserManager.UI.Database;

public class ControllerRepository
{
    private readonly AttendanceDatabase _database;

    public ControllerRepository(
        AttendanceDatabase database)
    {
        _database = database;
    }


    /// <summary>
    /// Получить все контроллеры текущего объекта.
    /// </summary>
    public async Task<List<ControllerInfo>>
        GetAllAsync()
    {
        var result =
            new List<ControllerInfo>();

        await using var connection =
            new SqliteConnection(
                _database.ConnectionString);

        await connection.OpenAsync();

        await using var command =
            connection.CreateCommand();

        command.CommandText = """
            SELECT
                Name,
                IpAddress,
                Username,
                Password,
                Model,
                Firmware,
                ApiType,
                UseByDefault,
                IsOnline,
                AttendanceRole
            FROM Controllers
            ORDER BY Name, IpAddress;
            """;

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var controller =
                new ControllerInfo
                {
                    Name =
                        reader.IsDBNull(0)
                            ? ""
                            : reader.GetString(0),

                    IpAddress =
                        reader.IsDBNull(1)
                            ? ""
                            : reader.GetString(1),

                    Username =
                        reader.IsDBNull(2)
                            ? ""
                            : reader.GetString(2),

                    Password =
                        reader.IsDBNull(3)
                            ? ""
                            : reader.GetString(3),

                    Model =
                        reader.IsDBNull(4)
                            ? ""
                            : reader.GetString(4),

                    Firmware =
                        reader.IsDBNull(5)
                            ? ""
                            : reader.GetString(5),

                    ApiType =
                        reader.IsDBNull(6)
                            ? ""
                            : reader.GetString(6),

                    UseByDefault =
                        !reader.IsDBNull(7) &&
                        reader.GetInt64(7) != 0,

                    IsOnline =
                        !reader.IsDBNull(8) &&
                        reader.GetInt64(8) != 0,

                    AttendanceRole =
                        reader.IsDBNull(9)
                            ? AttendanceRole.None
                            : (AttendanceRole)
                                reader.GetInt32(9)
                };

            result.Add(controller);
        }

        return result;
    }


    /// <summary>
    /// Количество контроллеров
    /// в текущей выбранной БД.
    /// </summary>
    public async Task<int> GetCountAsync()
    {
        await using var connection =
            new SqliteConnection(
                _database.ConnectionString);

        await connection.OpenAsync();

        await using var command =
            connection.CreateCommand();

        command.CommandText =
            "SELECT COUNT(*) FROM Controllers;";

        object? value =
            await command.ExecuteScalarAsync();

        return Convert.ToInt32(value);
    }


    /// <summary>
    /// Добавить новый контроллер.
    /// </summary>
    public async Task<bool> AddAsync(
        ControllerInfo controller)
    {
        if (string.IsNullOrWhiteSpace(
                controller.IpAddress))
        {
            return false;
        }

        await using var connection =
            new SqliteConnection(
                _database.ConnectionString);

        await connection.OpenAsync();

        await using var command =
            connection.CreateCommand();

        command.CommandText = """
            INSERT OR IGNORE INTO Controllers
            (
                Name,
                IpAddress,
                Username,
                Password,
                Model,
                Firmware,
                ApiType,
                UseByDefault,
                IsOnline,
                AttendanceRole
            )
            VALUES
            (
                $Name,
                $IpAddress,
                $Username,
                $Password,
                $Model,
                $Firmware,
                $ApiType,
                $UseByDefault,
                $IsOnline,
                $AttendanceRole
            );
            """;

        AddParameters(
            command,
            controller);

        int affected =
            await command.ExecuteNonQueryAsync();

        return affected > 0;
    }


    /// <summary>
    /// Сохранить существующий контроллер.
    /// Поиск выполняется по IP.
    /// </summary>
    public async Task<bool> UpdateAsync(
        ControllerInfo controller)
    {
        if (string.IsNullOrWhiteSpace(
                controller.IpAddress))
        {
            return false;
        }

        await using var connection =
            new SqliteConnection(
                _database.ConnectionString);

        await connection.OpenAsync();

        await using var command =
            connection.CreateCommand();

        command.CommandText = """
            UPDATE Controllers
            SET
                Name = $Name,
                Username = $Username,
                Password = $Password,
                Model = $Model,
                Firmware = $Firmware,
                ApiType = $ApiType,
                UseByDefault = $UseByDefault,
                IsOnline = $IsOnline,
                AttendanceRole = $AttendanceRole
            WHERE IpAddress = $IpAddress;
            """;

        AddParameters(
            command,
            controller);

        int affected =
            await command.ExecuteNonQueryAsync();

        return affected > 0;
    }


    /// <summary>
    /// Добавить контроллер или обновить существующий.
    /// </summary>
    public async Task SaveAsync(
        ControllerInfo controller)
    {
        if (string.IsNullOrWhiteSpace(
                controller.IpAddress))
        {
            return;
        }

        await using var connection =
            new SqliteConnection(
                _database.ConnectionString);

        await connection.OpenAsync();

        await using var command =
            connection.CreateCommand();

        command.CommandText = """
            INSERT INTO Controllers
            (
                Name,
                IpAddress,
                Username,
                Password,
                Model,
                Firmware,
                ApiType,
                UseByDefault,
                IsOnline,
                AttendanceRole
            )
            VALUES
            (
                $Name,
                $IpAddress,
                $Username,
                $Password,
                $Model,
                $Firmware,
                $ApiType,
                $UseByDefault,
                $IsOnline,
                $AttendanceRole
            )

            ON CONFLICT(IpAddress)
            DO UPDATE SET

                Name = excluded.Name,
                Username = excluded.Username,
                Password = excluded.Password,
                Model = excluded.Model,
                Firmware = excluded.Firmware,
                ApiType = excluded.ApiType,
                UseByDefault = excluded.UseByDefault,
                IsOnline = excluded.IsOnline,
                AttendanceRole = excluded.AttendanceRole;
            """;

        AddParameters(
            command,
            controller);

        await command.ExecuteNonQueryAsync();
    }


    /// <summary>
    /// Сохранить сразу весь список.
    ///
    /// ВАЖНО:
    /// этот метод НЕ удаляет контроллеры,
    /// которых нет в переданном списке.
    /// </summary>
    public async Task SaveAllAsync(
        IEnumerable<ControllerInfo> controllers)
    {
        foreach (ControllerInfo controller
                 in controllers)
        {
            await SaveAsync(controller);
        }
    }


    /// <summary>
    /// Удалить контроллер по IP.
    /// </summary>
    public async Task<bool> DeleteAsync(
        string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(
                ipAddress))
        {
            return false;
        }

        await using var connection =
            new SqliteConnection(
                _database.ConnectionString);

        await connection.OpenAsync();

        await using var command =
            connection.CreateCommand();

        command.CommandText = """
            DELETE FROM Controllers
            WHERE IpAddress = $IpAddress;
            """;

        command.Parameters.AddWithValue(
            "$IpAddress",
            ipAddress.Trim());

        int affected =
            await command.ExecuteNonQueryAsync();

        return affected > 0;
    }


    /// <summary>
    /// Проверить наличие контроллера по IP.
    /// </summary>
    public async Task<bool> ExistsAsync(
        string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(
                ipAddress))
        {
            return false;
        }

        await using var connection =
            new SqliteConnection(
                _database.ConnectionString);

        await connection.OpenAsync();

        await using var command =
            connection.CreateCommand();

        command.CommandText = """
            SELECT COUNT(*)
            FROM Controllers
            WHERE IpAddress = $IpAddress;
            """;

        command.Parameters.AddWithValue(
            "$IpAddress",
            ipAddress.Trim());

        object? value =
            await command.ExecuteScalarAsync();

        return Convert.ToInt32(value) > 0;
    }


    private static void AddParameters(
        SqliteCommand command,
        ControllerInfo controller)
    {
        command.Parameters.AddWithValue(
            "$Name",
            controller.Name ?? "");

        command.Parameters.AddWithValue(
            "$IpAddress",
            controller.IpAddress?.Trim() ?? "");

        command.Parameters.AddWithValue(
            "$Username",
            controller.Username ?? "");

        command.Parameters.AddWithValue(
            "$Password",
            controller.Password ?? "");

        command.Parameters.AddWithValue(
            "$Model",
            controller.Model ?? "");

        command.Parameters.AddWithValue(
            "$Firmware",
            controller.Firmware ?? "");

        command.Parameters.AddWithValue(
            "$ApiType",
            controller.ApiType ?? "");

        command.Parameters.AddWithValue(
            "$UseByDefault",
            controller.UseByDefault ? 1 : 0);

        command.Parameters.AddWithValue(
            "$IsOnline",
            controller.IsOnline ? 1 : 0);

        command.Parameters.AddWithValue(
            "$AttendanceRole",
            (int)controller.AttendanceRole);
    }
}