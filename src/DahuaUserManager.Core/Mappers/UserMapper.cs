using DahuaUserManager.Api.Clients;
using DahuaUserManager.Models.Entities;

namespace DahuaUserManager.Core.Mappers;

public static class UserMapper
{
    public static AccessUser ToAccessUser(AccessControlCard source)
    {
        return new AccessUser
        {
            RecNo = source.RecNo,
            UserId = source.UserId,
            FullName = source.CardName,
            CardNumber = source.CardNo,
            CardStatus = source.CardStatus.ToString(),
            IsValid = source.IsValid,
            ValidFrom = ParseDate(source.ValidDateStart),
            ValidTo = ParseDate(source.ValidDateEnd),
            HasFace = false
        };
    }

    public static AccessControlCard ToAccessControlCard(AccessUser source)
    {
        return new AccessControlCard
        {
            RecNo = source.RecNo,
            UserId = source.UserId,
            CardName = source.FullName,
            CardNo = source.CardNumber,
            CardStatus = int.TryParse(source.CardStatus, out int status) ? status : 0,
            IsValid = source.IsValid,
            ValidDateStart = source.ValidFrom?.ToString("yyyy-MM-dd HH:mm:ss") ?? "",
            ValidDateEnd = source.ValidTo?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""
        };
    }

    private static DateTime? ParseDate(string value)
    {
        return DateTime.TryParse(value, out DateTime result)
            ? result
            : null;
    }
}