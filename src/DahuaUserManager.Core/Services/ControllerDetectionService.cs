using System.Diagnostics;
using DahuaUserManager.Api.Clients;
using DahuaUserManager.Models.Entities;

namespace DahuaUserManager.Core.Services;

public class ControllerDetectionService
{
    private readonly DahuaClient _client = new();

    public async Task<ControllerInfo> DetectAsync(
        string ip,
        string username,
        string password)
    {
        ControllerInfo info = new()
        {
            IpAddress = ip,
            Username = username,
            Password = password,
            Name = ip,
            IsOnline = false
        };

        Stopwatch sw = Stopwatch.StartNew();

        try
        {
            string response = await _client.ExecuteAuthenticatedGetAsync(
                ip,
                username,
                password,
                "/cgi-bin/magicBox.cgi?action=getSystemInfo");

            sw.Stop();

            info.IsOnline = true;

            info.Model = GetValue(response, "deviceType");
            info.Firmware = GetValue(response, "version");

            if (string.IsNullOrWhiteSpace(info.Model))
                info.Model = GetValue(response, "model");

            if (string.IsNullOrWhiteSpace(info.Model))
                info.Model = "Не определена";

            info.ApiType = DetectApi(response);

            if (!string.IsNullOrWhiteSpace(info.Model))
                info.Name = info.Model;
        }
        catch
        {
            sw.Stop();

            info.IsOnline = false;
            info.Model = "Нет связи";
            info.ApiType = "";
        }

        return info;
    }

    private static string DetectApi(string response)
    {
        if (response.Contains("deviceType"))
            return "MagicBox";

        return "Unknown";
    }

    private static string GetValue(string text, string key)
    {
        foreach (string line in text.Split('\n'))
        {
            string s = line.Trim();

            if (!s.StartsWith(key + "="))
                continue;

            return s[(key.Length + 1)..].Trim();
        }

        return "";
    }
}