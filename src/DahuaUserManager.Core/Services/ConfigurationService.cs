using DahuaUserManager.Models.Entities;
using System.IO;
using System.Text.Json;

namespace DahuaUserManager.Core.Services;

public class ConfigurationService
{
    private readonly string _configFolder;
    private readonly string _configFile;

    public ConfigurationService()
    {
        _configFolder = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Config");

        _configFile = Path.Combine(
            _configFolder,
            "appsettings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (!Directory.Exists(_configFolder))
                Directory.CreateDirectory(_configFolder);

            if (!File.Exists(_configFile))
            {
                AppSettings defaultSettings = CreateDefaultSettings();

                Save(defaultSettings);

                return defaultSettings;
            }

            string json = File.ReadAllText(_configFile);

            AppSettings? loadedSettings =
                JsonSerializer.Deserialize<AppSettings>(json);

            return loadedSettings ?? CreateDefaultSettings();
        }
        catch
        {
            return CreateDefaultSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        if (!Directory.Exists(_configFolder))
            Directory.CreateDirectory(_configFolder);

        string json = JsonSerializer.Serialize(
            settings,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        File.WriteAllText(_configFile, json);
    }

    private static AppSettings CreateDefaultSettings()
    {
        return new AppSettings
        {
            Controllers = new List<ControllerInfo>()
        };
    }
}