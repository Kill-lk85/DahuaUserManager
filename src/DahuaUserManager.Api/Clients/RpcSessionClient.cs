using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DahuaUserManager.Api.Clients;

public class RpcSessionClient
{
    private readonly Rpc2Client _rpc = new();

    public async Task<string> LoginAsync(
        string ipAddress,
        string username,
        string password)
    {
        var firstRequest = new
        {
            method = "global.login",
            @params = new
            {
                userName = username,
                password = "",
                clientType = "Web3.0"
            },
            id = 1,
            session = 0
        };

        string firstResponse = await _rpc.PostAsync(
            ipAddress,
            "",
            JsonSerializer.Serialize(firstRequest),
            "/RPC2_Login");

        using JsonDocument firstDoc = JsonDocument.Parse(firstResponse);

        string session = firstDoc.RootElement
            .GetProperty("session")
            .GetRawText()
            .Trim('"');

        JsonElement p = firstDoc.RootElement.GetProperty("params");

        string realm = p.GetProperty("realm").GetString() ?? "";
        string random = p.GetProperty("random").GetString() ?? "";
        string encryption = p.TryGetProperty("encryption", out var enc)
            ? enc.GetString() ?? "Default"
            : "Default";

        string loginPassword = CreateDefaultPassword(
            username,
            password,
            realm,
            random);

        var secondRequest = new
        {
            method = "global.login",
            @params = new
            {
                userName = username,
                password = loginPassword,
                clientType = "Web3.0",
                authorityType = "Default",
                passwordType = encryption
            },
            id = 2,
            session
        };

        string secondResponse = await _rpc.PostAsync(
            ipAddress,
            session,
            JsonSerializer.Serialize(secondRequest),
            "/RPC2_Login");

        using JsonDocument secondDoc = JsonDocument.Parse(secondResponse);

        if (secondDoc.RootElement.TryGetProperty("error", out JsonElement error))
            throw new Exception("RPC login ошибка:\n" + error);

        if (secondDoc.RootElement.TryGetProperty("session", out JsonElement finalSession))
            return finalSession.GetString() ?? session;

        return session;
    }

    private static string CreateDefaultPassword(
        string username,
        string password,
        string realm,
        string random)
    {
        string first = Md5Upper($"{username}:{realm}:{password}");
        string second = Md5Upper($"{username}:{random}:{first}");

        return second;
    }

    private static string Md5Upper(string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        byte[] hash = MD5.HashData(bytes);

        var sb = new StringBuilder();

        foreach (byte b in hash)
            sb.Append(b.ToString("X2"));

        return sb.ToString();
    }
}