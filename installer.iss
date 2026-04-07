; Inno Setup 6 script for QuanLyKhoNguyenLieuPizza
; Build with Inno Setup Compiler (ISCC)

#define MyAppName "QuanLyKhoNguyenLieuPizza"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "QuanLyKhoPizza"
#define MyAppExeName "QuanLyKhoNguyenLieuPizza.exe"
#define PublishDir "D:\EXE"
#define SqlInstance "."  ; Đổi thành .\SQLEXPRESS nếu bạn dùng SQLEXPRESS

[Setup]
AppId={{8BFB8CB4-5A3C-4E49-B13C-C6DA5E97F4A1}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName=C:\Program Files\QuanLyKhoPizza
DefaultGroupName=QuanLyKhoPizza
OutputDir=D:\Setup
OutputBaseFilename=PizzaInn_Installer
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{group}\QuanLyKhoNguyenLieuPizza"; Filename: "{app}\{#MyAppExeName}"
Name: "{commondesktop}\QuanLyKhoNguyenLieuPizza"; Filename: "{app}\{#MyAppExeName}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Chạy {#MyAppName}"; Flags: nowait postinstall skipifsilent

[Code]
function IsSqlServerAvailable(): Boolean;
var
  ResultCode: Integer;
begin
  Result := Exec(ExpandConstant('{cmd}'), '/C sqlcmd -S {#SqlInstance} -E -Q "SELECT 1" -b', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) and (ResultCode = 0);
end;

function RestoreDatabase(): Boolean;
var
  SqlCmdExe, SqlParams, BackupPath, SqlQuery: string;
  ResultCode: Integer;
begin
  Result := False;

  { Kiểm tra SQL Server có sẵn không }
  if not IsSqlServerAvailable() then
  begin
    MsgBox('Không tìm thấy SQL Server instance "{#SqlInstance}".' + #13#10 +
           'Hãy đảm bảo SQL Server đã được cài đặt và đang chạy.' + #13#10 +
           'Bạn có thể restore database thủ công sau khi cài SQL Server.', mbError, MB_OK);
    Exit;
  end;

  SqlCmdExe := ExpandConstant('{cmd}');
  BackupPath := ExpandConstant('{app}\db\QuanLyKhoNguyenLieuPizza.bak');

  { Kiểm tra file backup có tồn tại không }
  if not FileExists(BackupPath) then
  begin
    MsgBox('Không tìm thấy file backup database tại:' + #13#10 + BackupPath + #13#10 +
           'Hãy kiểm tra lại thư mục db trong thư mục cài đặt.', mbError, MB_OK);
    Exit;
  end;

  SqlQuery :=
    'IF DB_ID(N''QuanLyKhoNguyenLieuPizza'') IS NULL ' +
    'BEGIN CREATE DATABASE [QuanLyKhoNguyenLieuPizza] END; ' +
    'ALTER DATABASE [QuanLyKhoNguyenLieuPizza] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; ' +
    'RESTORE DATABASE [QuanLyKhoNguyenLieuPizza] FROM DISK = N''' + BackupPath + ''' WITH REPLACE; ' +
    'ALTER DATABASE [QuanLyKhoNguyenLieuPizza] SET MULTI_USER;';

  SqlParams := '/C sqlcmd -S {#SqlInstance} -E -b -Q "' + SqlQuery + '"';

  if Exec(SqlCmdExe, SqlParams, '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    if ResultCode = 0 then
    begin
      Result := True;
      MsgBox('Khôi phục database thành công!', mbInformation, MB_OK);
    end
    else
      MsgBox('Khôi phục database thất bại. Mã lỗi: ' + IntToStr(ResultCode) + #13#10 +
             'Hãy kiểm tra SQL Server instance và quyền truy cập.', mbError, MB_OK);
  end
  else
    MsgBox('Không thể chạy sqlcmd. Hãy chắc chắn SQL Server Command Line Tools đã được cài đặt và sqlcmd có trong PATH.', mbError, MB_OK);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    if not RestoreDatabase() then
    begin
      { Không hủy cài đặt nếu restore lỗi, chỉ thông báo cho người dùng }
    end;
  end;
end;
