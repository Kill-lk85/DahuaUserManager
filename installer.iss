#define MyAppName "Dahua User Manager"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Kill_l"
#define MyAppExeName "DahuaUserManager.exe"
#define PublishDir "D:\DahuaUserManager\src\DahuaUserManager.UI\bin\Release\net10.0-windows\win-x64\publish"
#define IconFile "D:\DahuaUserManager\src\DahuaUserManager.UI\Resources\App.ico"

[Setup]
AppId={{7E9C1B6A-7D2A-4B61-9A55-DAHUAUSERMANAGER}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\Dahua User Manager
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=D:\DahuaUserManager\InstallerOutput
OutputBaseFilename=DahuaUserManagerSetup_1.0.0
SetupIconFile={#IconFile}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "Создать ярлык на рабочем столе"; GroupDescription: "Дополнительные задачи:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\{#MyAppExeName}"; IconIndex: 0
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\{#MyAppExeName}"; IconIndex: 0; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Запустить {#MyAppName}"; Flags: nowait postinstall skipifsilent