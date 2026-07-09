using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DahuaUserManager.Api.Clients;

public class FaceUploadClient
{
    private const int WorkOrderCount = 134;

    public async Task<bool> UploadFaceAsync(
        string ipAddress,
        string username,
        string password,
        string userId,
        string photoPath,
        int departId = 1)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserID не указан.", nameof(userId));

        if (string.IsNullOrWhiteSpace(photoPath))
            return false;

        if (!File.Exists(photoPath))
            throw new FileNotFoundException("Файл фотографии не найден.", photoPath);

        byte[] photoBytes = await File.ReadAllBytesAsync(photoPath);

        if (photoBytes.Length < 1000)
            throw new Exception("Файл фотографии слишком маленький или повреждён: " + photoPath);

        string photoBase64 = Convert.ToBase64String(photoBytes);

        string logFile = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "photo_upload.log");

        File.WriteAllText(
            logFile,
            $"UserID={userId}{Environment.NewLine}" +
            $"DepartID={departId}{Environment.NewLine}" +
            $"Photo={photoPath}{Environment.NewLine}" +
            $"Size={photoBytes.Length}{Environment.NewLine}" +
            $"Base64Length={photoBase64.Length}{Environment.NewLine}");

        var cookieContainer = new CookieContainer();

        using var handler = new HttpClientHandler
        {
            CookieContainer = cookieContainer,
            UseCookies = true
        };

        using var httpClient = new HttpClient(handler);

        string session = await LoginAsync(
            httpClient,
            ipAddress,
            username,
            password,
            logFile);

        await InsertAttendanceAsync(
            httpClient,
            ipAddress,
            session,
            userId,
            departId,
            logFile);

        return await InsertFaceAsync(
            httpClient,
            ipAddress,
            session,
            userId,
            photoBase64,
            logFile);
    }

    private static async Task InsertAttendanceAsync(
        HttpClient httpClient,
        string ipAddress,
        string session,
        string userId,
        int departId,
        string logFile)
    {
        int[] workOrderIds = Enumerable
            .Repeat(0, WorkOrderCount)
            .ToArray();

        var request = new
        {
            method = "AccessAttendance.insertMulti",
            @params = new
            {
                AttendanceInfoList = new[]
                {
                    new
                    {
                        UserID = userId,
                        DepartID = departId,
                        AttendanceType = 0,
                        WorkOrderIDs = workOrderIds
                    }
                }
            },
            id = 128,
            session
        };

        string response = await PostJsonAsync(
            httpClient,
            ipAddress,
            "/RPC2",
            JsonSerializer.Serialize(request),
            logFile,
            "AccessAttendance.insertMulti");

        using JsonDocument document = JsonDocument.Parse(response);
        JsonElement root = document.RootElement;

        if (root.TryGetProperty("error", out JsonElement error))
        {
            throw new Exception(
                "AccessAttendance.insertMulti ошибка:\n\n" +
                error +
                "\n\nПолный ответ:\n" +
                response);
        }

        if (root.TryGetProperty("result", out JsonElement result) &&
            result.GetBoolean())
            return;

        throw new Exception(
            "AccessAttendance.insertMulti: неожиданный ответ:\n\n" +
            response);
    }

    private static async Task<bool> InsertFaceAsync(
        HttpClient httpClient,
        string ipAddress,
        string session,
        string userId,
        string photoBase64,
        string logFile)
    {
        var request = new
        {
            method = "AccessFace.insertMulti",
            @params = new
            {
                FaceList = new[]
                {
                    new
                    {
                        UserID = userId,
                        PhotoData = new[]
                        {
                            photoBase64
                        },
                        FaceData = new[]
                        {
                            ""
                        }
                    }
                }
            },
            id = 129,
            session
        };

        string response = await PostJsonAsync(
            httpClient,
            ipAddress,
            "/RPC2",
            JsonSerializer.Serialize(request),
            logFile,
            "AccessFace.insertMulti");

        using JsonDocument document = JsonDocument.Parse(response);
        JsonElement root = document.RootElement;

        if (root.TryGetProperty("error", out JsonElement error))
        {
            throw new Exception(
                "AccessFace.insertMulti ошибка:\n\n" +
                error +
                "\n\nПолный ответ:\n" +
                response);
        }

        if (root.TryGetProperty("result", out JsonElement result))
            return result.GetBoolean();

        throw new Exception(
            "AccessFace.insertMulti: неожиданный ответ:\n\n" +
            response);
    }

    private static async Task<string> LoginAsync(
        HttpClient httpClient,
        string ipAddress,
        string username,
        string password,
        string logFile)
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

        string firstResponse = await PostJsonAsync(
            httpClient,
            ipAddress,
            "/RPC2_Login",
            JsonSerializer.Serialize(firstRequest),
            logFile,
            "login 1");

        using JsonDocument firstDoc = JsonDocument.Parse(firstResponse);

        JsonElement firstRoot = firstDoc.RootElement;

        string session = firstRoot
            .GetProperty("session")
            .GetRawText()
            .Trim('"');

        JsonElement p = firstRoot.GetProperty("params");

        string realm = p.GetProperty("realm").GetString() ?? "";
        string random = p.GetProperty("random").GetString() ?? "";
        string encryption = p.TryGetProperty("encryption", out JsonElement enc)
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

        string secondResponse = await PostJsonAsync(
            httpClient,
            ipAddress,
            "/RPC2_Login",
            JsonSerializer.Serialize(secondRequest),
            logFile,
            "login 2");

        using JsonDocument secondDoc = JsonDocument.Parse(secondResponse);

        JsonElement secondRoot = secondDoc.RootElement;

        if (secondRoot.TryGetProperty("error", out JsonElement error))
            throw new Exception("RPC login ошибка:\n" + error);

        if (secondRoot.TryGetProperty("session", out JsonElement finalSession))
            return finalSession.GetString() ?? session;

        return session;
    }

    private static async Task<string> PostJsonAsync(
        HttpClient httpClient,
        string ipAddress,
        string path,
        string json,
        string logFile,
        string title)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"http://{ipAddress}{path}");

        request.Content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        request.Headers.TryAddWithoutValidation(
            "Accept",
            "application/json, text/plain, */*");

        request.Headers.Referrer =
            new Uri($"http://{ipAddress}/");

        using HttpResponseMessage response =
            await httpClient.SendAsync(request);

        string body = await response.Content.ReadAsStringAsync();

        File.AppendAllText(
            logFile,
            Environment.NewLine +
            "=== " + title + " ===" +
            Environment.NewLine +
            "HTTP " + (int)response.StatusCode +
            Environment.NewLine +
            body +
            Environment.NewLine);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                title + " HTTP " + (int)response.StatusCode +
                "\n\n" + body);
        }

        return body;
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