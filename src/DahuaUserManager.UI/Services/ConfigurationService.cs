using System.Text.Json;
using System.IO;
using DahuaUserManager.Api.Clients;

namespace DahuaUserManager.UI.Services;

public class ConfigurationService
{
    public List<ControllerInfo> LoadControllers()
    {
        string fileName = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Config",
            "appsettings.json");

        if (!File.Exists(fileName))
            return [];

        string json = File.ReadAllText(fileName);

        var settings = JsonSerializer.Deserialize<AppSettings>(json);

        return settings?.Controllers ?? [];
    }

    private class AppSettings
    {
        public List<ControllerInfo> Controllers { get; set; } = [];
    }
}