namespace DahuaUserManager.Api.Clients.RpcModels;

public class RpcResponse<T>
{
    public int id { get; set; }

    public string session { get; set; } = "";

    public T? result { get; set; }

    public RpcError? error { get; set; }
}

public class RpcError
{
    public int code { get; set; }

    public string message { get; set; } = "";
}