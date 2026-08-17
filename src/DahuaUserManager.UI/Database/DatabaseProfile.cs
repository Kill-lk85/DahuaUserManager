namespace DahuaUserManager.UI.Database;

public class DatabaseProfile
{
    /// <summary>
    /// Уникальный ID подключения.
    /// </summary>
    public string Id { get; set; } =
        Guid.NewGuid().ToString();

    /// <summary>
    /// Отображаемое имя объекта / базы.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Имя файла SQLite.
    /// Например Karaganda.db
    /// </summary>
    public string FileName { get; set; } = "";

    /// <summary>
    /// Пока SQLite.
    /// В дальнейшем здесь сможет быть Cloud / PostgreSQL / SQL Server.
    /// </summary>
    public string DatabaseType { get; set; } =
        "SQLite";
}


public class DatabaseConfiguration
{
    /// <summary>
    /// Последняя выбранная база.
    /// </summary>
    public string LastDatabaseId { get; set; } = "";

    /// <summary>
    /// Список доступных баз.
    /// </summary>
    public List<DatabaseProfile> Databases { get; set; } =
        new();
}