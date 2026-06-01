; Inno Setup script for Package Manager installer
; All critical paths are injected by Cake Build-Inno-Installer task.

#ifndef MyAppVersion
  #error MyAppVersion is not defined
#endif

#ifndef MyAppName
  #error MyAppName is not defined
#endif

#ifndef MyAppPublisher
  #error MyAppPublisher is not defined
#endif

#ifndef CompanyDirectoryName
  #error CompanyDirectoryName is not defined
#endif

#ifndef ProductDirectoryName
  #error ProductDirectoryName is not defined
#endif

#ifndef AppId
  #error AppId is not defined
#endif

#ifndef GuiTargetDir
  #error GuiTargetDir is not defined
#endif

#ifndef GuiCliTargetDir
  #error GuiCliTargetDir is not defined
#endif

#ifndef CleanupScriptPath
  #error CleanupScriptPath is not defined
#endif

#ifndef BrandingConfigPath
  #error BrandingConfigPath is not defined
#endif

#ifndef OutputDir
  #error OutputDir is not defined
#endif

#ifndef OutputBaseFilename
  #error OutputBaseFilename is not defined
#endif

#ifndef MyAppExeName
  #error MyAppExeName is not defined
#endif

#ifndef MyCliExeName
  #error MyCliExeName is not defined
#endif

#ifndef MyAppMutexName
  #error MyAppMutexName is not defined
#endif

#ifndef MyCliMutexName
  #error MyCliMutexName is not defined
#endif

[Setup]
AppId={#AppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppMutex={#MyAppMutexName},{#MyCliMutexName}
DefaultDirName={pf}\{#CompanyDirectoryName}\{#ProductDirectoryName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
OutputDir={#OutputDir}
OutputBaseFilename={#OutputBaseFilename}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
CloseApplications=yes
RestartApplications=no
CloseApplicationsFilter={#MyCliExeName},{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "chinesesimplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"

[CustomMessages]
english.LaunchApp=Launch {#MyAppName}
english.RequireNet48OrHigher=This application requires .NET Framework 4.8 or later.
english.RequireChocolateyInstalled=Chocolatey must already be installed.

chinesesimplified.LaunchApp=启动 {#MyAppName}
chinesesimplified.RequireNet48OrHigher=此应用程序需要 .NET Framework 4.8 或更高版本。
chinesesimplified.RequireChocolateyInstalled=必须先安装 Chocolatey。

[Files]
Source: "{#GuiTargetDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#GuiCliTargetDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#GuiTargetDir}\ChocolateyGui.exe"; DestDir: "{app}"; DestName: "{#MyAppExeName}"; Flags: ignoreversion
Source: "{#GuiTargetDir}\ChocolateyGui.exe.config"; DestDir: "{app}"; DestName: "{#MyAppExeName}.config"; Flags: ignoreversion
Source: "{#GuiCliTargetDir}\ChocolateyGuiCli.exe"; DestDir: "{app}"; DestName: "{#MyCliExeName}"; Flags: ignoreversion
Source: "{#CleanupScriptPath}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BrandingConfigPath}"; DestDir: "{app}"; DestName: "branding.config"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"

[Dirs]
Name: "{commonappdata}\{#CompanyDirectoryName}"
Name: "{commonappdata}\{#CompanyDirectoryName}\{#ProductDirectoryName}"
Name: "{commonappdata}\{#CompanyDirectoryName}\{#ProductDirectoryName}\Config"; Permissions: admins-full users-readexec
Name: "{localappdata}\{#CompanyDirectoryName}\{#ProductDirectoryName}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchApp}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\CleanupProvidedPackages.ps1"" -Mode ""{code:GetCleanupMode}"""; RunOnceId: "RunPackageCleanup"; Flags: runhidden

[Code]
function IsNet48OrHigherInstalled: Boolean;
var
  Release: Cardinal;
begin
  Result :=
    RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) and (Release >= 528040);
end;

function IsChocolateyInstalled: Boolean;
begin
  Result := DirExists(ExpandConstant('{commonappdata}\chocolatey'));
end;

function InitializeSetup(): Boolean;
begin
  if not IsNet48OrHigherInstalled() then
  begin
    MsgBox(CustomMessage('RequireNet48OrHigher'), mbCriticalError, MB_OK);
    Result := False;
    exit;
  end;

  if not IsChocolateyInstalled() then
  begin
    MsgBox(CustomMessage('RequireChocolateyInstalled'), mbCriticalError, MB_OK);
    Result := False;
    exit;
  end;

  Result := True;
end;

function InitializeUninstall(): Boolean;
begin
  Result := True;
end;

function HasUninstallSwitch(const SwitchName: string): Boolean;
var
  CommandLine: string;
  NormalizedSwitch: string;
begin
  CommandLine := ' ' + Uppercase(GetCmdTail) + ' ';
  NormalizedSwitch := '/' + Uppercase(SwitchName);

  Result :=
    (Pos(' ' + NormalizedSwitch + ' ', CommandLine) > 0) or
    (Pos(' ' + NormalizedSwitch + '=', CommandLine) > 0) or
    (Pos(' ' + NormalizedSwitch + ':', CommandLine) > 0);
end;

function GetCleanupMode(Value: string): string;
var
  EnvMode: string;
  DebugMode: string;
  IsDebugEnabled: Boolean;
begin
  EnvMode := Uppercase(Trim(GetEnv('INSTR_PKGMGR_UNINSTALL_MODE')));
  DebugMode := Uppercase(Trim(GetEnv('INSTR_PKGMGR_UNINSTALL_DEBUG')));
  IsDebugEnabled :=
    (DebugMode = '1') or
    (DebugMode = 'TRUE') or
    (DebugMode = 'YES') or
    (DebugMode = 'ON');

  if IsDebugEnabled then
  begin
    Log('Cleanup mode detection. Env INSTR_PKGMGR_UNINSTALL_MODE=' + EnvMode);
    Log('Cleanup mode detection. CmdTail=' + GetCmdTail);
  end;

  if EnvMode = 'SELF' then
  begin
    if IsDebugEnabled then
      Log('Cleanup mode selected: Self (environment override).');
    Result := 'Self';
    exit;
  end;

  if EnvMode = 'PURGE' then
  begin
    if IsDebugEnabled then
      Log('Cleanup mode selected: Purge (environment override).');
    Result := 'Purge';
    exit;
  end;

  if HasUninstallSwitch('SELFUNINSTALL') then
  begin
    if IsDebugEnabled then
      Log('Cleanup mode selected: Self (SELFUNINSTALL detected).');
    Result := 'Self';
    exit;
  end;

  if HasUninstallSwitch('PURGE') then
  begin
    if IsDebugEnabled then
      Log('Cleanup mode selected: Purge (PURGE detected).');
    Result := 'Purge';
    exit;
  end;

  if IsDebugEnabled then
    Log('Cleanup mode selected: Purge (default).');
  Result := 'Purge';
end;
