using System.Net;
using System.Text;

namespace DahuaUserManager.Api.Clients;

public class DahuaClient
{
    private readonly HttpClient _httpClient;

    public DahuaClient()
    {
        var handler = new HttpClientHandler
        {
            UseCookies = true,
            AllowAutoRedirect = true,
            Credentials = new NetworkCredential("admin", "admin123!")
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
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> GetAuthInfoAsync(string ipAddress)
    {
        try
        {
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"http://{ipAddress}/cgi-bin/magicBox.cgi?action=getSystemInfo");

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead);

            var sb = new StringBuilder();

            sb.AppendLine($"HTTP {(int)response.StatusCode} {response.StatusCode}");
            sb.AppendLine();

            sb.AppendLine("=== Headers ===");

            foreach (var header in response.Headers)
            {
                sb.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }

            sb.AppendLine();

            sb.AppendLine("=== WWW-Authenticate ===");

            if (response.Headers.WwwAuthenticate.Any())
            {
                foreach (var auth in response.Headers.WwwAuthenticate)
                {
                    sb.AppendLine(auth.ToString());
                }
            }
            else
            {
                sb.AppendLine("Отсутствует");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return ex.ToString();
        }
    }
}