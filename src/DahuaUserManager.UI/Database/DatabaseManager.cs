using System.IO;
using System.Text.Json;

namespace DahuaUserManager.UI.Database;

public class DatabaseManager
{
    private static readonly Lazy<DatabaseManager>
        _instance =
            new(() => new DatabaseManager());

    public static DatabaseManager Instance =>
        _instance.Value;


    private DatabaseConfiguration _configuration =
        new();

    private bool _loaded;


    private DatabaseManager()
    {
    }


    /// <summary>
    /// Общая папка программы.
    /// </summary>
    public string AppFolder =>
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "DahuaUserManager");


    /// <summary>
    /// Папка со всеми SQLite-базами объектов.
    /// </summary>
    public string DatabasesFolder =>
        Path.Combine(
            AppFolder,
            "Databases");


    /// <summary>
    /// Локальный файл со списком подключений.
    ///
    /// Это НЕ база объекта.
    /// Здесь хранится только список доступных БД
    /// и последняя выбранная.
    /// </summary>
    public string ConfigurationFile =>
        Path.Combine(
            AppFolder,
            "databases.json");


    public IReadOnlyList<DatabaseProfile> Databases
    {
        get
        {
            EnsureLoaded();

            return _configuration.Databases;
        }
    }


    /// <summary>
    /// Текущая выбранная база.
    /// </summary>
    public DatabaseProfile CurrentDatabase
    {
        get
        {
            EnsureLoaded();

            DatabaseProfile? database =
                _configuration.Databases
                    .FirstOrDefault(x =>
                        x.Id ==
                        _configuration.LastDatabaseId);

            if (database != null)
                return database;

            if (_configuration.Databases.Count == 0)
            {
                throw new InvalidOperationException(
                    "Нет доступных баз данных.");
            }

            database =
                _configuration.Databases[0];

            _configuration.LastDatabaseId =
                database.Id;

            Save();

            return database;
        }
    }


    /// <summary>
    /// Полный путь к текущей SQLite базе.
    /// </summary>
    public string CurrentDatabasePath
    {
        get
        {
            DatabaseProfile database =
                CurrentDatabase;

            return GetDatabasePath(database);
        }
    }


    public void Load()
    {
        if (_loaded)
            return;

        Directory.CreateDirectory(
            AppFolder);

        Directory.CreateDirectory(
            DatabasesFolder);

        if (File.Exists(ConfigurationFile))
        {
            try
            {
                string json =
                    File.ReadAllText(
                        ConfigurationFile);

                DatabaseConfiguration? config =
                    JsonSerializer.Deserialize<
                        DatabaseConfiguration>(
                        json);

                if (config != null)
                    _configuration = config;
            }
            catch
            {
                _configuration =
                    new DatabaseConfiguration();
            }
        }


        // --------------------------------------------------------
        // Миграция нашей старой attendance.db
        // --------------------------------------------------------

        MigrateLegacyDatabase();


        // Если вообще нет баз -
        // создаём первую базу автоматически.
        if (_configuration.Databases.Count == 0)
        {
            DatabaseProfile database =
                CreateProfile(
                    "Основная база",
                    "main.db");

            _configuration.Databases.Add(
                database);

            _configuration.LastDatabaseId =
                database.Id;
        }


        // Проверяем последнюю выбранную БД.
        if (string.IsNullOrWhiteSpace(
                _configuration.LastDatabaseId) ||
            !_configuration.Databases.Any(x =>
                x.Id ==
                _configuration.LastDatabaseId))
        {
            _configuration.LastDatabaseId =
                _configuration.Databases[0].Id;
        }


        _loaded = true;

        Save();
    }


    /// <summary>
    /// Создать новую SQLite базу объекта.
    /// </summary>
    public DatabaseProfile AddDatabase(
        string name)
    {
        EnsureLoaded();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Не указано имя базы.",
                nameof(name));
        }

        string fileName =
            CreateUniqueFileName(name);

        DatabaseProfile database =
            CreateProfile(
                name.Trim(),
                fileName);

        _configuration.Databases.Add(
            database);

        Save();

        return database;
    }


    /// <summary>
    /// Выбрать рабочую базу.
    /// </summary>
    public void SelectDatabase(
        DatabaseProfile database)
    {
        EnsureLoaded();

        if (!_configuration.Databases.Any(x =>
                x.Id == database.Id))
        {
            throw new InvalidOperationException(
                "База отсутствует в списке подключений.");
        }

        _configuration.LastDatabaseId =
            database.Id;

        Save();
    }


    public void SelectDatabase(
        string databaseId)
    {
        EnsureLoaded();

        DatabaseProfile? database =
            _configuration.Databases
                .FirstOrDefault(x =>
                    x.Id == databaseId);

        if (database == null)
        {
            throw new InvalidOperationException(
                "База данных не найдена.");
        }

        SelectDatabase(database);
    }


    /// <summary>
    /// Получить физический путь SQLite файла.
    /// </summary>
    public string GetDatabasePath(
        DatabaseProfile database)
    {
        return Path.Combine(
            DatabasesFolder,
            database.FileName);
    }


    public void Save()
    {
        Directory.CreateDirectory(
            AppFolder);

        Directory.CreateDirectory(
            DatabasesFolder);

        string json =
            JsonSerializer.Serialize(
                _configuration,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

        File.WriteAllText(
            ConfigurationFile,
            json);
    }


    private void EnsureLoaded()
    {
        if (!_loaded)
            Load();
    }


    private void MigrateLegacyDatabase()
    {
        string oldDatabase =
            Path.Combine(
                AppFolder,
                "attendance.db");

        if (!File.Exists(oldDatabase))
            return;


        // Если уже зарегистрирована база,
        // которая является перенесённой старой базой -
        // второй раз ничего не делаем.
        bool alreadyMigrated =
            _configuration.Databases.Any(x =>
                x.FileName.Equals(
                    "Основная.db",
                    StringComparison.OrdinalIgnoreCase));

        if (alreadyMigrated)
            return;


        string target =
            Path.Combine(
                DatabasesFolder,
                "Основная.db");


        // ВАЖНО:
        // старую БД не удаляем.
        //
        // Сначала просто копируем её.
        // Когда убедимся, что всё работает,
        // старый attendance.db можно будет удалить вручную.
        if (!File.Exists(target))
        {
            File.Copy(
                oldDatabase,
                target,
                false);
        }


        DatabaseProfile database =
            CreateProfile(
                "Основная база",
                "Основная.db");

        _configuration.Databases.Add(
            database);

        _configuration.LastDatabaseId =
            database.Id;
    }


    private static DatabaseProfile CreateProfile(
        string name,
        string fileName)
    {
        return new DatabaseProfile
        {
            Id =
                Guid.NewGuid().ToString(),

            Name =
                name,

            FileName =
                fileName,

            DatabaseType =
                "SQLite"
        };
    }


    private string CreateUniqueFileName(
        string name)
    {
        string safeName =
            SanitizeFileName(name);

        if (string.IsNullOrWhiteSpace(
                safeName))
        {
            safeName =
                "Database";
        }

        string fileName =
            safeName + ".db";

        int number = 2;

        while (File.Exists(
                   Path.Combine(
                       DatabasesFolder,
                       fileName)) ||
               _configuration.Databases.Any(x =>
                   x.FileName.Equals(
                       fileName,
                       StringComparison.OrdinalIgnoreCase)))
        {
            fileName =
                $"{safeName}_{number}.db";

            number++;
        }

        return fileName;
    }


    private static string SanitizeFileName(
        string value)
    {
        foreach (char c in
                 Path.GetInvalidFileNameChars())
        {
            value =
                value.Replace(
                    c,
                    '_');
        }

        return value.Trim();
    }
}