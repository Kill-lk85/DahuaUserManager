using DahuaUserManager.UI.Database;
using DahuaUserManager.UI.Settings;
using System.Diagnostics;
using System.IO;

namespace DahuaUserManager.UI.Services;

public class DatabaseBackupService
{
    public async Task<string> CreateBackupAsync(
        ProgramSettings settings)
    {
        ValidateSettings(settings);

        string sourceFolder =
            DatabaseManager.Instance.DatabasesFolder;

        Directory.CreateDirectory(
            sourceFolder);

        Directory.CreateDirectory(
            settings.BackupDestinationFolder);

        string timestamp =
            DateTime.Now.ToString(
                "yyyy-MM-dd_HH-mm-ss");

        string archivePath =
            Path.Combine(
                settings.BackupDestinationFolder,
                $"DahuaBackup_{timestamp}.7z");

        var arguments =
            new List<string>
            {
                "a",
                "-t7z",
                archivePath,
                Path.Combine(sourceFolder, "*"),
                "-mx=5",
                "-ssw"
            };

        if (!string.IsNullOrEmpty(
                settings.BackupPassword))
        {
            arguments.Add(
                "-p" + settings.BackupPassword);

            if (settings.EncryptFileNames)
                arguments.Add("-mhe=on");
        }

        var startInfo =
            new ProcessStartInfo
            {
                FileName =
                    settings.SevenZipPath,

                UseShellExecute =
                    false,

                RedirectStandardOutput =
                    true,

                RedirectStandardError =
                    true,

                CreateNoWindow =
                    true
            };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(
                argument);
        }

        using Process process =
            Process.Start(startInfo)
            ?? throw new InvalidOperationException(
                "Не удалось запустить 7-Zip.");

        string standardOutput =
            await process.StandardOutput
                .ReadToEndAsync();

        string standardError =
            await process.StandardError
                .ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new Exception(
                "7-Zip завершился с ошибкой.\n\n" +
                standardError +
                "\n\n" +
                standardOutput);
        }

        DeleteOldBackups(
            settings.BackupDestinationFolder,
            settings.RetentionDays);

        return archivePath;
    }

    public void DeleteOldBackups(
        string destinationFolder,
        int retentionDays)
    {
        if (retentionDays <= 0)
            return;

        if (!Directory.Exists(
                destinationFolder))
        {
            return;
        }

        DateTime threshold =
            DateTime.Now.AddDays(
                -retentionDays);

        foreach (string file in
                 Directory.GetFiles(
                     destinationFolder,
                     "DahuaBackup_*.7z"))
        {
            try
            {
                DateTime created =
                    File.GetCreationTime(file);

                if (created < threshold)
                    File.Delete(file);
            }
            catch
            {
                // Ошибку удаления старого архива
                // не считаем ошибкой самого бэкапа.
            }
        }
    }

    private static void ValidateSettings(
        ProgramSettings settings)
    {
        if (string.IsNullOrWhiteSpace(
                settings.SevenZipPath) ||
            !File.Exists(
                settings.SevenZipPath))
        {
            throw new FileNotFoundException(
                "Не найден 7z.exe.",
                settings.SevenZipPath);
        }

        if (string.IsNullOrWhiteSpace(
                settings.BackupDestinationFolder))
        {
            throw new InvalidOperationException(
                "Не указана папка для сохранения архивов.");
        }

        if (settings.RetentionDays < 1)
        {
            throw new InvalidOperationException(
                "Срок хранения должен быть не меньше 1 дня.");
        }
    }
}