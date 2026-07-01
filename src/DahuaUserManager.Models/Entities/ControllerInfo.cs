namespace DahuaUserManager.Models.Entities;

public class ControllerInfo
{
    /// <summary>
    /// Имя контроллера.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// IP-адрес.
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Логин.
    /// </summary>
    public string Username { get; set; } = "admin";

    /// <summary>
    /// Пароль.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Модель контроллера.
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Версия прошивки.
    /// </summary>
    public string Firmware { get; set; } = string.Empty;

    /// <summary>
    /// Определенный API.
    /// </summary>
    public string ApiType { get; set; } = string.Empty;

    /// <summary>
    /// Контроллер доступен.
    /// </summary>
    public bool IsOnline { get; set; }
}