using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DahuaUserManager.Api.Clients;

public class Rpc2AuthClient
{
    private readonly Rpc2Client _rpc2Client = new();

    public async Task<string> LoginAsync(
        string ipAddress,
        string username,
        string password)
    {
        string firstJson = $$"""
        {
            "method": "global.login",
            "params": {
                "userName": "{{username}}",
                "password": "",
                "clientType": "Web3.0",
                "loginType": "Direct"
            },
            "id": 1
        }
        """;

        string firstResponse = await _rpc2Client.PostAsync(
            ipAddress,
            "",
            firstJson);

        using JsonDocument firstDocument =
            JsonDocument.Parse(firstResponse);

        JsonElement firstRoot =
            firstDocument.RootElement;

        if (!firstRoot.TryGetProperty("session", out JsonElement sessionElement))
        {
            throw new Exception(
                "RPC2 login: в первом ответе нет session\n\n" +
                firstResponse);
        }

        string session =
            sessionElement.ToString();

        if (!firstRoot.TryGetProperty("params", out JsonElement paramsElement))
        {
            throw new Exception(
                "RPC2 login: в первом ответе нет params\n\n" +
                firstResponse);
        }

        string realm =
            paramsElement.GetProperty("realm").GetString() ?? "";

        string random =
            paramsElement.GetProperty("random").GetString() ?? "";

        string passwordHash = DahuaPasswordHash(
            username,
            password,
            realm,
            random);

        string secondJson = $$"""
        {
            "method": "global.login",
            "params": {
                "userName": "{{username}}",
                "password": "{{passwordHash}}",
                "clientType": "Web3.0",
                "loginType": "Direct",
                "authorityType": "Default"
            },
            "session": "{{session}}",
            "id": 2
        }
        """;

        string secondResponse = await _rpc2Client.PostAsync(
            ipAddress,
            session,
            secondJson);

        using JsonDocument secondDocument =
            JsonDocument.Parse(secondResponse);

        JsonElement secondRoot =
            secondDocument.RootElement;

        bool result =
            secondRoot.TryGetProperty("result", out JsonElement resultElement)
            && resultElement.GetBoolean();

        if (!result)
        {
            throw new Exception(
                "RPC2 login failed\n\n" +
                "Первый ответ:\n" +
                firstResponse +
                "\n\nВторой ответ:\n" +
                secondResponse);
        }

        return session;
    }

    private static string DahuaPasswordHash(
        string username,
        string password,
        string realm,
        string random)
    {
        string firstHash =
            Md5($"{username}:{realm}:{password}");

        string secondHash =
            Md5($"{username}:{random}:{firstHash}");

        return secondHash.ToUpperInvariant();
    }

    private static string Md5(string input)
    {
        byte[] bytes =
            Encoding.UTF8.GetBytes(input);

        byte[] hash =
            MD5.HashData(bytes);

        StringBuilder sb = new();

        foreach (byte b in hash)
        {
            sb.Append(b.ToString("x2"));
        }

        return sb.ToString();
    }
}