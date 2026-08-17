namespace DahuaUserManager.Models.Database;

public class AttendanceEventEntity
{
    /// <summary>
    /// Внутренний ID записи в нашей SQLite базе.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Номер записи события на контроллере Dahua.
    /// </summary>
    public string RecNo { get; set; } = "";

    /// <summary>
    /// UserID сотрудника.
    /// </summary>
    public string UserId { get; set; } = "";

    /// <summary>
    /// Имя сотрудника.
    /// </summary>
    public string UserName { get; set; } = "";

    /// <summary>
    /// Имя контроллера.
    /// </summary>
    public string ControllerName { get; set; } = "";

    /// <summary>
    /// IP контроллера.
    /// Нужен в том числе для определения уникальности события.
    /// </summary>
    public string ControllerIp { get; set; } = "";

    /// <summary>
    /// Роль контроллера: Вход / Выход.
    /// </summary>
    public string Direction { get; set; } = "";

    /// <summary>
    /// Время события.
    /// </summary>
    public DateTime EventTime { get; set; }

    /// <summary>
    /// Исходный Unix timestamp от Dahua.
    /// Сохраняем на случай диагностики.
    /// </summary>
    public long EventUnixTime { get; set; }

    /// <summary>
    /// Тип события, например Вход / Выход.
    /// </summary>
    public string EventType { get; set; } = "";
}