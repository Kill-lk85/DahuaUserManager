using System.Net.Http;
using System.Text;

namespace DahuaUserManager.Api.Clients;

public class Rpc2Client
{
    private readonly HttpClient _httpClient = new();

    public async Task<string> PostAsync(
        string ipAddress,
        string session,
        string json)
    {
        return await PostAsync(
            ipAddress,
            session,
            json,
            "/RPC2");
    }

    public async Task<string> PostAsync(
        string ipAddress,
        string session,
        string json,
        string path)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"http://{ipAddress}{path}");

        request.Content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        if (!string.IsNullOrWhiteSpace(session))
        {
            request.Headers.Add(
                "Cookie",
                $"WebClientHttpSessionID={session}");
        }

        using HttpResponseMessage response =
            await _httpClient.SendAsync(request);

        string body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                $"RPC2 HTTP {(int)response.StatusCode}\n\n{body}");
        }

        return body;
    }
}