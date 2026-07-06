namespace DahuaUserManager.Api.Clients.RpcModels;

public class RpcRequest
{
    public string method { get; set; } = "";

    public object? @params { get; set; }

    public int id { get; set; }

    public string session { get; set; } = "";
}