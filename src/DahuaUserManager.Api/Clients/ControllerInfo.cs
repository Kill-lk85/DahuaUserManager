namespace DahuaUserManager.Api.Clients;

public class ControllerInfo
{
    public string Name { get; set; } = "";

    public string IpAddress { get; set; } = "";

    public string Username { get; set; } = "admin";

    public string Password { get; set; } = "";

    public bool IsOnline { get; set; }

    public string DeviceType { get; set; } = "";

    public string HardwareVersion { get; set; } = "";

    public string SerialNumber { get; set; } = "";
}