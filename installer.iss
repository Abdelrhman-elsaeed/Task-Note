[Setup]
AppId={{D8C84F3B-6B74-4C59-B4EE-99FA20DE1BE4}}
AppName=TaskNote
AppVersion=1.0.0
AppPublisher=Antigravity
DefaultDirName={localappdata}\Programs\TaskNote
DefaultGroupName=TaskNote
AllowNoIcons=yes
OutputDir=.\installer
OutputBaseFilename=TaskNote_Setup_v13
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
SetupIconFile=d:\Task Note\Resources\app_icon.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: ".\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\TaskNote"; Filename: "{app}\TaskNote.exe"
Name: "{group}\{cm:UninstallProgram,TaskNote}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\TaskNote"; Filename: "{app}\TaskNote.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\TaskNote.exe"; Description: "{cm:LaunchProgram,TaskNote}"; Flags: nowait postinstall skipifsilent
