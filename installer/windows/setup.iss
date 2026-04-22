; BattleTank — Inno Setup installer
; Requires: Inno Download Plugin (IDP) — https://mitrichsoftware.wordpress.com/inno-setup-tools/inno-download-plugin/
; Build: iscc /DAppVersion=0.0.12 setup.iss
; Output: Output/BattleTank-Setup.exe

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif

#define AppName      "BattleTank"
#define AppPublisher "randy"
#define AppURL       "https://github.com/randy/battle_tank"
#define GithubRepo   "randy/battle_tank"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}/issues
DefaultDirName={localappdata}\{#AppName}
DefaultGroupName={#AppName}
AllowNoIcons=yes
OutputBaseFilename=BattleTank-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
DisableProgramGroupPage=yes
PrivilegesRequired=lowest

[InstallDelete]
Type: files; Name: "{app}\*.dll"
Type: files; Name: "{app}\*.pck"
Type: files; Name: "{app}\*.exe"; Excludes: "unins*"

[Languages]
Name: "french";   MessagesFile: "compiler:Languages\French.isl"
Name: "english";  MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Icons]
Name: "{group}\{#AppName}";              Filename: "{app}\BattleTank.exe"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}";        Filename: "{app}\BattleTank.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\BattleTank.exe"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[Code]
var
  DownloadPage: TDownloadWizardPage;
  DownloadURL: String;

function GetLatestReleaseURL(): String;
var
  WinHTTP: Variant;
  Response, JSON: String;
  AssetStart, NameStart, NameEnd, URLStart, URLEnd: Integer;
begin
  Result := '';
  try
    WinHTTP := CreateOleObject('WinHttp.WinHttpRequest.5.1');
    WinHTTP.Open('GET', 'https://api.github.com/repos/{#GithubRepo}/releases/latest', False);
    WinHTTP.SetRequestHeader('User-Agent', 'InnoSetup/{#AppVersion}');
    WinHTTP.Send('');
    JSON := WinHTTP.ResponseText;

    // Cherche "client-windows" dans les assets pour extraire browser_download_url
    AssetStart := Pos('"client-windows', JSON);
    if AssetStart = 0 then
      AssetStart := Pos('"BattleTank-windows', JSON);
    if AssetStart = 0 then
      AssetStart := Pos('"client_windows', JSON);

    if AssetStart > 0 then
    begin
      URLStart := PosEx('"browser_download_url": "', JSON, AssetStart);
      if URLStart > 0 then
      begin
        URLStart := URLStart + Length('"browser_download_url": "');
        URLEnd := PosEx('"', JSON, URLStart);
        if URLEnd > URLStart then
          Result := Copy(JSON, URLStart, URLEnd - URLStart);
      end;
    end;
  except
    // Laisser Result vide, sera géré dans InitializeWizard
  end;
end;

procedure InitializeWizard();
begin
  DownloadPage := CreateDownloadPage(
    SetupMessage(msgWizardPreparing),
    SetupMessage(msgPreparingDesc),
    nil
  );
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  if CurPageID = wpReady then
  begin
    DownloadURL := GetLatestReleaseURL();

    if DownloadURL = '' then
    begin
      MsgBox(
        'Impossible de récupérer la dernière version depuis GitHub.' + #13#10 +
        'Vérifiez votre connexion internet et réessayez.',
        mbError, MB_OK
      );
      Result := False;
      Exit;
    end;

    DownloadPage.Clear();
    DownloadPage.Add(DownloadURL, 'client-windows.zip', '');
    DownloadPage.Show;

    try
      try
        DownloadPage.Download();
        Result := True;
      except
        MsgBox('Erreur lors du téléchargement : ' + GetExceptionMessage(), mbError, MB_OK);
        Result := False;
      end;
    finally
      DownloadPage.Hide;
    end;
  end else
    Result := True;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ZipFile, DestDir: String;
  Shell: Variant;
begin
  if CurStep = ssInstall then
  begin
    ZipFile := ExpandConstant('{tmp}\client-windows.zip');
    DestDir := ExpandConstant('{app}');
    ForceDirectories(DestDir);

    Shell := CreateOleObject('Shell.Application');
    Shell.NameSpace(DestDir).CopyHere(
      Shell.NameSpace(ZipFile).Items(),
      4 or 16  // FOF_SILENT | FOF_NOCONFIRMATION
    );
  end;
end;
