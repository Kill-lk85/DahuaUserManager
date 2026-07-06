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
        string passwordHash = Md5Upper(password);

        var request = new
        {
            method = "global.login",
            @params = new
            {
                userName = username,
                password = passwordHash,
                clientType = "Web3.0",
                authorityType = "Default",
                passwordType = "Default"
            },
            id = 1,
            session = ""
        };

        string json = JsonSerializer.Serialize(request);

        string result = await _rpc.PostAsync(
            ipAddress,
            "",
            json);

        using JsonDocument doc = JsonDocument.Parse(result);

        if (doc.RootElement.TryGetProperty("session", out JsonElement sessionElement))
            return sessionElement.GetString() ?? "";

        throw new Exception("RPC login не вернул session:\n" + result);
    }

    private static string Md5Upper(string value)
    {
        byte[] input = Encoding.UTF8.GetBytes(value);
        byte[] hash = MD5.HashData(input);

        StringBuilder sb = new();

        foreach (byte b in hash)
            sb.Append(b.ToString("X2"));

        return sb.ToString();
    }
}