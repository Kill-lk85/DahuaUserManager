namespace DahuaUserManager.Models.Entities;

/// <summary>
/// Пользователь системы доступа.
/// Внутренняя модель программы.
/// Не зависит от API Dahua.
/// </summary>
public class AccessUser
{
    /// <summary>
    /// Внутренний номер записи контроллера.
    /// </summary>
    public int RecNo { get; set; }

    /// <summary>
    /// Идентификатор пользователя.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Имя пользователя.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Номер карты.
    /// </summary>
    public string CardNumber { get; set; } = string.Empty;

    /// <summary>
    /// Статус карты.
    /// </summary>
    public string CardStatus { get; set; } = string.Empty;

    /// <summary>
    /// Дата начала действия.
    /// </summary>
    public DateTime? ValidFrom { get; set; }

    /// <summary>
    /// Дата окончания действия.
    /// </summary>
    public DateTime? ValidTo { get; set; }

    /// <summary>
    /// Пользователь активен.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Есть ли фотография.
    /// </summary>
    public bool HasFace { get; set; }

    /// <summary>
    /// Номер отдела (на будущее).
    /// </summary>
    public string Department { get; set; } = string.Empty;

    /// <summary>
    /// Должность.
    /// </summary>
    public string Position { get; set; } = string.Empty;

    /// <summary>
    /// Телефон.
    /// </summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Электронная почта.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Комментарий.
    /// </summary>
    public string Comment { get; set; } = string.Empty;
}