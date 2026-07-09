using System.Text.Json;

namespace DahuaUserManager.Api.Clients;

public class AccessUserRpcClient
{
    private readonly Rpc2Client _rpc = new();
    private readonly RpcSessionClient _sessionClient = new();

    public async Task<bool> CreateUserAsync(
        string ipAddress,
        string username,
        string password,
        string userId,
        string fullName,
        DateTime validFrom,
        DateTime validTo)
    {
        string session = await _sessionClient.LoginAsync(
            ipAddress,
            username,
            password);

        var request = new
        {
            method = "AccessUser.insertMulti",
            @params = new
            {
                UserList = new[]
                {
                    new
                    {
                        UserID = userId,
                        UserName = fullName,
                        UserType = 0,
                        Authority = 2,
                        Password = "",
                        Doors = new[] { 0 },
                        TimeSections = new[] { 255 },
                        ValidFrom = validFrom.ToString("yyyy-MM-dd HH:mm:ss"),
                        ValidTo = validTo.ToString("yyyy-MM-dd HH:mm:ss")
                    }
                }
            },
            id = 100,
            session = session
        };

        string json = JsonSerializer.Serialize(request);

        string response = await _rpc.PostAsync(
            ipAddress,
            session,
            json,
            "/RPC2");

        using JsonDocument document = JsonDocument.Parse(response);

        JsonElement root = document.RootElement;

        if (root.TryGetProperty("error", out JsonElement error))
        {
            throw new Exception(
                "AccessUser.insertMulti ошибка:\n\n" +
                error +
                "\n\nПолный ответ:\n" +
                response);
        }

        if (root.TryGetProperty("result", out JsonElement result))
        {
            return result.GetBoolean();
        }

        throw new Exception(
            "AccessUser.insertMulti: неожиданный ответ контроллера:\n\n" +
            response);
    }
}