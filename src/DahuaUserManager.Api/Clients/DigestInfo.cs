namespace DahuaUserManager.Api.Clients;

public class DigestInfo
{
    public string Realm { get; set; } = "";
    public string Nonce { get; set; } = "";
    public string Opaque { get; set; } = "";
    public string Qop { get; set; } = "auth";
    public string Algorithm { get; set; } = "MD5";
}