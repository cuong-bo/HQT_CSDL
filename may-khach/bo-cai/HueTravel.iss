; Inno Setup script for the Windows client. Build with:
;   ISCC.exe installer\HueTravel.iss /DAppVersion=1.0.0
; Needs: a Release build of Hue_travel, and the MapWinGIS folder (the one the MapWinGIS installer creates).
; Optional: bo-cai/redist/vc_redist.x86.exe (Microsoft Visual C++ Redistributable, x86) is run when present.

#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#ifndef SourceDir
  #define SourceDir "../ung-dung/bin/Release"
#endif
#ifndef MapWinGisDir
  #define MapWinGisDir "C:\Program Files\MapWinGIS"
#endif
#ifndef DefaultServer
  #define DefaultServer ""
#endif
#ifndef DefaultPublicKey
  #define DefaultPublicKey ""
#endif
#define AppName "Hue Travel"

[Setup]
AppId={{6F1B7C1A-5B52-4D9B-9E0C-3F0E2A8B7D41}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=Hue Travel
DefaultDirName={autopf}\HueTravel
DefaultGroupName={#AppName}
PrivilegesRequired=admin
OutputDir=output
OutputBaseFilename=HueTravel-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
SetupIconFile=..\ung-dung\Resources\hue-travel.ico
UninstallDisplayIcon={app}\Hue_travel.exe
; 32-bit installer on purpose: the app and MapWinGIS are 32-bit, so {sys} is SysWOW64 and regsvr32 registers the 32-bit control.

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Excludes: "*.pdb,*.vshost.*,mwtiles.db3,NutMangGiaoThong.*,*.lock"; Flags: recursesubdirs ignoreversion
Source: "{#MapWinGisDir}\*"; DestDir: "{app}\MapWinGIS"; Excludes: "unins000.*,*.zip,*.pas,*.XML,regMapWinGIS.cmd,unregMapWinGIS.cmd"; Flags: recursesubdirs ignoreversion
Source: "redist\vc_redist.x86.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall skipifsourcedoesntexist

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\Hue_travel.exe"
Name: "{commondesktop}\{#AppName}"; Filename: "{app}\Hue_travel.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Tạo biểu tượng trên màn hình"; GroupDescription: "Biểu tượng:"

[Run]
Filename: "{tmp}\vc_redist.x86.exe"; Parameters: "/install /quiet /norestart"; StatusMsg: "Đang cài Visual C++ Runtime..."; Flags: skipifdoesntexist
Filename: "{sys}\regsvr32.exe"; Parameters: "/s ""{app}\MapWinGIS\MapWinGIS.ocx"""; StatusMsg: "Đang đăng ký MapWinGIS..."; Flags: runhidden
Filename: "{app}\Hue_travel.exe"; Description: "Mở {#AppName}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{sys}\regsvr32.exe"; Parameters: "/u /s ""{app}\MapWinGIS\MapWinGIS.ocx"""; RunOnceId: "UnregisterMapWinGIS"; Flags: runhidden

[Code]
var
  ServerPage: TInputQueryWizardPage;

function IsDotNet472Installed: Boolean;
var
  Release: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release)
            and (Release >= 461808);
end;

function InitializeSetup: Boolean;
begin
  Result := IsDotNet472Installed;
  if not Result then
    MsgBox('Cần .NET Framework 4.7.2 trở lên. Hãy cài bản cập nhật Windows hoặc .NET Framework rồi chạy lại bộ cài.', mbError, MB_OK);
end;

procedure InitializeWizard;
var
  Legacy: AnsiString;
begin
  ServerPage := CreateInputQueryPage(wpSelectDir, 'Máy chủ dữ liệu',
    'Địa chỉ máy chủ Hue Travel',
    'Nhập địa chỉ do quản trị viên cung cấp, ví dụ http://203.0.113.10 . Để trống để chỉ dùng dữ liệu ngoại tuyến.');
  ServerPage.Add('Địa chỉ:', False);
  ServerPage.Add('Khóa công khai của máy chủ (không bắt buộc, dùng để kiểm tra dữ liệu không bị sửa):', False);
  ServerPage.Values[0] := '{#DefaultServer}';
  ServerPage.Values[1] := '{#DefaultPublicKey}';
  // A machine set up with an earlier version keeps its server: that version's folder was called CoDoGIS.
  if (ServerPage.Values[0] = '') and LoadStringFromFile(ExpandConstant('{commonappdata}\CoDoGIS\server.txt'), Legacy) then
    ServerPage.Values[0] := Trim(Legacy);
  if (ServerPage.Values[1] = '') and LoadStringFromFile(ExpandConstant('{commonappdata}\CoDoGIS\public.key'), Legacy) then
    ServerPage.Values[1] := Trim(Legacy);
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  Dir: string;
begin
  if CurStep = ssPostInstall then
  begin
    Dir := ExpandConstant('{commonappdata}\HueTravel');
    ForceDirectories(Dir);
    SaveStringToFile(Dir + '\server.txt', Trim(ServerPage.Values[0]), False);
    SaveStringToFile(Dir + '\public.key', Trim(ServerPage.Values[1]), False);
  end;
end;
