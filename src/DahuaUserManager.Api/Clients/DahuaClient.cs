using System.Net;

namespace DahuaUserManager.Api.Clients;

public class DahuaClient
{
    private readonly HttpClient _httpClient;

    public DahuaClient()
    {
        var handler = new HttpClientHandler
        {
            UseCookies = true,
            AllowAutoRedirect = true
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
    }

    public async Task<bool> IsOnlineAsync(string ipAddress)
    {
        try
        {
            using var response = await _httpClient.GetAsync($"http://{ipAddress}");

            // Любой ответ от контроллера означает, что он доступен.
            return true;
        }
        catch
        {
            return false;
        }
    }
}