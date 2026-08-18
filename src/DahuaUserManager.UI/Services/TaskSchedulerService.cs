using DahuaUserManager.UI.Database;
using DahuaUserManager.UI.Settings;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace DahuaUserManager.UI.Services;

public class TaskSchedulerService
{
    private const string TaskName =
        "DahuaUserManager Backup";

    private readonly string _serviceFolder;
    private readonly string _scriptPath;

    public TaskSchedulerService()
    {
        _serviceFolder =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "DahuaUserManager",
                "Backup");

        _scriptPath =
            Path.Combine(
                _serviceFolder,
                "backup.ps1");
    }

    public async Task ApplyAsync(
        ProgramSettings settings)
    {
        if (!settings.AutomaticBackupEnabled)
        {
            await DeleteTaskAsync();
            return;
        }

        ValidateSchedule(settings);

        Directory.CreateDirectory(
            _serviceFolder);

        CreatePowerShellScript(
            settings);

        await CreateOrUpdateTaskAsync(
            settings.BackupTime);
    }

    private void CreatePowerShellScript(
        ProgramSettings settings)
    {
        string sourceFolder =
            DatabaseManager.Instance.DatabasesFolder;

        string sevenZip =
            EscapePowerShellLiteral(
                settings.SevenZipPath);

        string source =
            EscapePowerShellLiteral(
                sourceFolder);

        string destination =
            EscapePowerShellLiteral(
                settings.BackupDestinationFolder);

        string password =
            EscapePowerShellLiteral(
                settings.BackupPassword);

        string encryptNames =
            settings.EncryptFileNames
                ? "$true"
                : "$false";

        string script =
$@"$ErrorActionPreference = 'Stop'

$SevenZip = '{sevenZip}'
$Source = '{source}'
$Destination = '{destination}'
$Password = '{password}'
$EncryptFileNames = {encryptNames}
$RetentionDays = {settings.RetentionDays}

if (-not (Test-Path -LiteralPath $SevenZip)) {{
    throw ""7z.exe not found: $SevenZip""
}}

New-Item -ItemType Directory -Force -Path $Destination | Out-Null

$Timestamp = Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'
$Archive = Join-Path $Destination (""DahuaBackup_"" + $Timestamp + "".7z"")

$Arguments = @(
    'a',
    '-t7z',
    $Archive,
    (Join-Path $Source '*'),
    '-mx=5',
    '-ssw'
)

if ($Password -ne '') {{
    $Arguments += ('-p' + $Password)

    if ($EncryptFileNames) {{
        $Arguments += '-mhe=on'
    }}
}}

& $SevenZip @Arguments

if ($LASTEXITCODE -ne 0) {{
    throw ""7-Zip exit code: $LASTEXITCODE""
}}

$Limit = (Get-Date).AddDays(-$RetentionDays)

Get-ChildItem -LiteralPath $Destination -Filter 'DahuaBackup_*.7z' -File |
    Where-Object {{ $_.CreationTime -lt $Limit }} |
    Remove-Item -Force
";

        File.WriteAllText(
            _scriptPath,
            script,
            new UTF8Encoding(false));
    }

    private async Task CreateOrUpdateTaskAsync(
        string time)
    {
        string taskRun =
            $"powershell.exe -NoProfile -ExecutionPolicy Bypass -File \"{_scriptPath}\"";

        var startInfo =
            new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

        startInfo.ArgumentList.Add("/Create");
        startInfo.ArgumentList.Add("/TN");
        startInfo.ArgumentList.Add(TaskName);
        startInfo.ArgumentList.Add("/TR");
        startInfo.ArgumentList.Add(taskRun);
        startInfo.ArgumentList.Add("/SC");
        startInfo.ArgumentList.Add("DAILY");
        startInfo.ArgumentList.Add("/ST");
        startInfo.ArgumentList.Add(time);
        startInfo.ArgumentList.Add("/F");

        using Process process =
            Process.Start(startInfo)
            ?? throw new InvalidOperationException(
                "Не удалось запустить schtasks.exe.");

        string output =
            await process.StandardOutput
                .ReadToEndAsync();

        string error =
            await process.StandardError
                .ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new Exception(
                "Не удалось создать задачу Планировщика Windows.\n\n" +
                error +
                "\n\n" +
                output);
        }
    }

    public async Task DeleteTaskAsync()
    {
        var startInfo =
            new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

        startInfo.ArgumentList.Add("/Delete");
        startInfo.ArgumentList.Add("/TN");
        startInfo.ArgumentList.Add(TaskName);
        startInfo.ArgumentList.Add("/F");

        using Process process =
            Process.Start(startInfo)
            ?? throw new InvalidOperationException(
                "Не удалось запустить schtasks.exe.");

        await process.WaitForExitAsync();

        // Если задачи ещё нет, это не ошибка настройки.
    }

    private static void ValidateSchedule(
        ProgramSettings settings)
    {
        if (!TimeOnly.TryParseExact(
                settings.BackupTime,
                "HH:mm",
                out _))
        {
            throw new InvalidOperationException(
                "Время автобэкапа должно быть в формате HH:mm.");
        }

        if (string.IsNullOrWhiteSpace(
                settings.BackupDestinationFolder))
        {
            throw new InvalidOperationException(
                "Не указана папка для сохранения архивов.");
        }

        if (string.IsNullOrWhiteSpace(
                settings.SevenZipPath) ||
            !File.Exists(
                settings.SevenZipPath))
        {
            throw new FileNotFoundException(
                "Не найден 7z.exe.",
                settings.SevenZipPath);
        }

        if (settings.RetentionDays < 1)
        {
            throw new InvalidOperationException(
                "Срок хранения должен быть не меньше 1 дня.");
        }
    }

    private static string EscapePowerShellLiteral(
        string value)
    {
        return value.Replace(
            "'",
            "''");
    }
}