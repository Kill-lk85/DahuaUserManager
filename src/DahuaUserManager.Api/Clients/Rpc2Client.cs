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
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"http://{ipAddress}/RPC2");

        request.Content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        // При login Cookie ещё нет.
        // После login используем полученную session.
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