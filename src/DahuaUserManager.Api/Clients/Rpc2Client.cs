using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace DahuaUserManager.Api.Clients;

public class Rpc2Client
{
    private readonly HttpClient _httpClient = new();

    public async Task<string> PostAsync(
        string ipAddress,
        string session,
        string json)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"http://{ipAddress}/RPC2");

        request.Content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        request.Headers.Add(
            "Cookie",
            $"WebClientHttpSessionID={session}");

        using HttpResponseMessage response =
            await _httpClient.SendAsync(request);

        string body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"RPC2 ошибка HTTP {(int)response.StatusCode}:\n{body}");

        return body;
    }

    public async Task<bool> InsertUserAsync(
        string ipAddress,
        string session,
        string userId,
        string name,
        string validFrom,
        string validTo)
    {
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
                        Name = name,
                        UserType = 0,
                        Authority = 2,
                        Password = "",
                        Doors = new[] { 0 },
                        TimeSections = new[] { 255 },
                        ValidFrom = validFrom,
                        ValidTo = validTo
                    }
                }
            },
            id = 900,
            session
        };

        string json = JsonSerializer.Serialize(request);

        string result = await PostAsync(ipAddress, session, json);

        using JsonDocument doc = JsonDocument.Parse(result);

        if (doc.RootElement.TryGetProperty("result", out JsonElement resultElement))
            return resultElement.GetBoolean();

        return false;
    }
}