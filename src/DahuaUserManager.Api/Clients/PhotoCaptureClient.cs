using System.Net;
using System.Net.Http;

namespace DahuaUserManager.Api.Clients;

public class PhotoCaptureClient
{
    public async Task<byte[]> GetSnapshotBytesAsync(
        string ipAddress,
        string username,
        string password)
    {
        string url = $"http://{ipAddress}/cgi-bin/snapshot.cgi?channel=1";

        var credentialCache = new CredentialCache();

        credentialCache.Add(
            new Uri($"http://{ipAddress}/"),
            "Digest",
            new NetworkCredential(username, password));

        credentialCache.Add(
            new Uri($"http://{ipAddress}/"),
            "Basic",
            new NetworkCredential(username, password));

        using var handler = new HttpClientHandler
        {
            Credentials = credentialCache,
            PreAuthenticate = false,
            UseCookies = true
        };

        using var httpClient = new HttpClient(handler);

        using HttpResponseMessage response =
            await httpClient.GetAsync(url);

        byte[] bytes =
            await response.Content.ReadAsByteArrayAsync();

        if (!response.IsSuccessStatusCode)
        {
            string text = TryGetText(bytes);

            throw new Exception(
                $"Ошибка захвата фото HTTP {(int)response.StatusCode}\n\n{text}");
        }

        string contentType =
            response.Content.Headers.ContentType?.MediaType ?? "";

        if (!contentType.Contains("image", StringComparison.OrdinalIgnoreCase))
        {
            string text = TryGetText(bytes);

            throw new Exception(
                "Контроллер вернул не изображение.\n\n" +
                "Content-Type: " + contentType +
                "\n\nОтвет:\n" +
                text);
        }

        if (bytes.Length < 1000)
        {
            string text = TryGetText(bytes);

            throw new Exception(
                "Контроллер вернул слишком маленький ответ вместо фото:\n\n" +
                text);
        }

        return bytes;
    }

    public async Task<string> SaveSnapshotBytesAsync(
        byte[] bytes,
        string userId)
    {
        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            "DahuaUserManager",
            "Captured");

        Directory.CreateDirectory(folder);

        string fileName = Path.Combine(
            folder,
            $"User_{SafeFileName(userId)}_{DateTime.Now:yyyyMMdd_HHmmss}.jpg");

        await File.WriteAllBytesAsync(fileName, bytes);

        return fileName;
    }

    public async Task<string> CaptureSnapshotAsync(
        string ipAddress,
        string username,
        string password,
        string userId)
    {
        byte[] bytes = await GetSnapshotBytesAsync(
            ipAddress,
            username,
            password);

        return await SaveSnapshotBytesAsync(
            bytes,
            userId);
    }

    private static string SafeFileName(string value)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            value = value.Replace(c, '_');

        return string.IsNullOrWhiteSpace(value)
            ? "unknown"
            : value;
    }

    private static string TryGetText(byte[] bytes)
    {
        try
        {
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return "";
        }
    }
}