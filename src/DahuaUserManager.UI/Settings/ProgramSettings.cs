namespace DahuaUserManager.UI.Settings;

public class ProgramSettings
{
    public string BackupDestinationFolder { get; set; } = "";

    public string SevenZipPath { get; set; } =
        @"C:\Program Files\7-Zip\7z.exe";

    public string BackupPassword { get; set; } = "";

    public bool EncryptFileNames { get; set; } = true;

    public bool AutomaticBackupEnabled { get; set; }

    public string BackupTime { get; set; } = "23:00";

    public int RetentionDays { get; set; } = 30;
}