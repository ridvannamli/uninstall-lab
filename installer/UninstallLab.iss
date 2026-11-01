; Inno Setup betiği. Derleme: ISCC /DAppVersion=0.1.0 installer\UninstallLab.iss
; Girdi: ..\publish (dotnet publish çıktısı). Çıktı: ..\dist\UninstallLab-Setup-<sürüm>.exe
; Not: Dosya yalnızca ASCII karakter içerir (Inno ANSI okur).

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif

#define AppName "Uninstall Lab"
#define AppExe "UninstallLab.exe"

[Setup]
AppId={{B7C2D1E0-4F1A-4C55-9E55-3A1D7C0F2A11}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=Ridvan Namli
DefaultDirName={autopf}\UninstallLab
DefaultGroupName={#AppName}
UninstallDisplayIcon={app}\{#AppExe}
OutputDir=..\dist
OutputBaseFilename=UninstallLab-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent
