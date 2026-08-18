using System.IO;
using System.Text.Json;

namespace DahuaUserManager.UI.Settings;

public class ProgramSettingsService
{
    private readonly string _settingsFolder;
    private readonly string _settingsFile;

    public ProgramSettingsService()
    {
        _settingsFolder =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "DahuaUserManager");

        _settingsFile =
            Path.Combine(
                _settingsFolder,
                "programsettings.json");
    }

    public ProgramSettings Load()
    {
        try
        {
            Directory.CreateDirectory(
                _settingsFolder);

            if (!File.Exists(_settingsFile))
                return new ProgramSettings();

            string json =
                File.ReadAllText(
                    _settingsFile);

            return JsonSerializer.Deserialize<ProgramSettings>(
                       json)
                   ?? new ProgramSettings();
        }
        catch
        {
            return new ProgramSettings();
        }
    }

    public void Save(
        ProgramSettings settings)
    {
        Directory.CreateDirectory(
            _settingsFolder);

        string json =
            JsonSerializer.Serialize(
                settings,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

        File.WriteAllText(
            _settingsFile,
            json);
    }
}