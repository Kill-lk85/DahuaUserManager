namespace DahuaUserManager.Api.Clients;

public class AccessControlCard
{
    public int RecNo { get; set; }
    public string UserId { get; set; } = "";
    public string CardName { get; set; } = "";
    public string CardNo { get; set; } = "";
    public int CardStatus { get; set; }
    public bool IsValid { get; set; }
    public string ValidDateStart { get; set; } = "";
    public string ValidDateEnd { get; set; } = "";
}