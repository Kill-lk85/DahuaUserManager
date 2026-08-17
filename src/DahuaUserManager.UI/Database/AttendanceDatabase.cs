using Microsoft.Data.Sqlite;
using System.IO;

namespace DahuaUserManager.UI.Database;

public class AttendanceDatabase
{
    private readonly DatabaseManager
        _databaseManager;

    public AttendanceDatabase()
    {
        _databaseManager =
            DatabaseManager.Instance;

        _databaseManager.Load();
    }


    /// <summary>
    /// Текущая выбранная база.
    ///
    /// ВАЖНО:
    /// путь НЕ сохраняется в поле.
    ///
    /// Поэтому если пользователь позже
    /// переключит объект, существующие
    /// репозитории автоматически начнут
    /// работать с новой БД.
    /// </summary>
    public string DatabasePath =>
        _databaseManager.CurrentDatabasePath;


    public string ConnectionString =>
        $"Data Source={DatabasePath}";


    public DatabaseProfile CurrentDatabase =>
        _databaseManager.CurrentDatabase;


    public async Task InitializeAsync()
    {
        // Папка уже создаётся DatabaseManager,
        // но дополнительная проверка не повредит.
        string? directory =
            Path.GetDirectoryName(
                DatabasePath);

        if (!string.IsNullOrWhiteSpace(
                directory))
        {
            Directory.CreateDirectory(
                directory);
        }


        await using var connection =
            new SqliteConnection(
                ConnectionString);

        await connection.OpenAsync();


        await using (var pragmaCommand =
                     connection.CreateCommand())
        {
            pragmaCommand.CommandText =
                "PRAGMA foreign_keys = ON;";

            await pragmaCommand
                .ExecuteNonQueryAsync();
        }


        string sql = """
            ------------------------------------------------------------
            -- СОБЫТИЯ ПОСЕЩАЕМОСТИ
            ------------------------------------------------------------

            CREATE TABLE IF NOT EXISTS AttendanceEvents
            (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,

                RecNo TEXT NOT NULL,

                UserId TEXT NOT NULL,

                UserName TEXT NOT NULL,

                ControllerName TEXT NOT NULL,

                ControllerIp TEXT NOT NULL,

                Direction TEXT NOT NULL,

                EventTime TEXT NOT NULL,

                EventUnixTime INTEGER NOT NULL,

                EventType TEXT NOT NULL
            );


            CREATE UNIQUE INDEX IF NOT EXISTS
                UX_AttendanceEvents_ControllerIp_RecNo
            ON AttendanceEvents
            (
                ControllerIp,
                RecNo
            );


            CREATE INDEX IF NOT EXISTS
                IX_AttendanceEvents_EventUnixTime
            ON AttendanceEvents
            (
                EventUnixTime
            );


            CREATE INDEX IF NOT EXISTS
                IX_AttendanceEvents_UserId
            ON AttendanceEvents
            (
                UserId
            );


            ------------------------------------------------------------
            -- ГРАФИКИ РАБОТЫ
            ------------------------------------------------------------

            CREATE TABLE IF NOT EXISTS WorkSchedules
            (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,

                Name TEXT NOT NULL,

                StartTime TEXT NOT NULL,

                EndTime TEXT NOT NULL,

                NormHours REAL NOT NULL DEFAULT 8,

                IsActive INTEGER NOT NULL DEFAULT 1
            );


            CREATE UNIQUE INDEX IF NOT EXISTS
                UX_WorkSchedules_Name
            ON WorkSchedules
            (
                Name
            );


            ------------------------------------------------------------
            -- НАЗНАЧЕНИЯ ГРАФИКОВ
            ------------------------------------------------------------

            CREATE TABLE IF NOT EXISTS EmployeeSchedules
            (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,

                UserId TEXT NOT NULL,

                ScheduleId INTEGER NOT NULL,

                DateFrom TEXT NOT NULL,

                DateTo TEXT,

                FOREIGN KEY (ScheduleId)
                    REFERENCES WorkSchedules(Id)
                    ON DELETE RESTRICT
            );


            CREATE INDEX IF NOT EXISTS
                IX_EmployeeSchedules_UserId
            ON EmployeeSchedules
            (
                UserId
            );


            CREATE INDEX IF NOT EXISTS
                IX_EmployeeSchedules_UserId_DateFrom
            ON EmployeeSchedules
            (
                UserId,
                DateFrom
            );
            """;


        await using var command =
            connection.CreateCommand();

        command.CommandText =
            sql;

        await command
            .ExecuteNonQueryAsync();
    }
}