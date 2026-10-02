#define MyAppName "Image Converter Pro"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Image Converter Pro"
#define MyAppExeName "ImageConverterPro.exe"

[Setup]
AppId={{4DD4A608-944E-4F63-8EC4-2D1336AA15EC}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=..\Release\Installer
OutputBaseFilename=ImageConverterProSetup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
SetupIconFile=..\Resources\AppIcon.ico

[Files]
Source: "..\Release\ImageConverterPro\ApplicationFiles\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent
