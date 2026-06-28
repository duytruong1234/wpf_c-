; Inno Setup 6 script for QuanLyKhoNguyenLieuPizza
; Build with Inno Setup Compiler (ISCC)

#define MyAppName "QuanLyKhoNguyenLieuPizza"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "QuanLyKhoPizza"
#define MyAppExeName "QuanLyKhoNguyenLieuPizza.exe"
#define PublishDir "D:\QuanLyKhoNguyenLieuPizza\Publish"
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
const
  SQL_SERVER_DOWNLOAD_URL = 'https://go.microsoft.com/fwlink/p/?linkid=2216019';

function IsSqlServerAvailable(): Boolean;
var
  ResultCode: Integer;
begin
  Result := Exec(ExpandConstant('{cmd}'),
    '/C sqlcmd -S {#SqlInstance} -E -Q "SELECT 1" -b',
    '', SW_HIDE, ewWaitUntilTerminated, ResultCode) and (ResultCode = 0);
end;

function IsDatabaseExists(): Boolean;
var
  ResultCode: Integer;
begin
  Result := Exec(ExpandConstant('{cmd}'),
    '/C sqlcmd -S {#SqlInstance} -E -b -Q "IF DB_ID(N''QLKNL_PIZZA'') IS NULL RAISERROR(''not found'', 16, 1)"',
    '', SW_HIDE, ewWaitUntilTerminated, ResultCode) and (ResultCode = 0);
end;

function CreateAndImportDatabase(): Boolean;
var
  ResultCode: Integer;
  SqlFilePath: string;
begin
  Result := False;
  SqlFilePath := ExpandConstant('{app}\Resources\Database\QLKNL_PIZZA.sql');

  { Kiểm tra file SQL có tồn tại không }
  if not FileExists(SqlFilePath) then
  begin
    MsgBox('Không tìm thấy file script cơ sở dữ liệu:' + #13#10 +
           SqlFilePath + #13#10 + #13#10 +
           'Database sẽ được tạo tự động khi bạn chạy ứng dụng lần đầu.',
           mbInformation, MB_OK);
    Result := True;
    Exit;
  end;

  { Bước 1: Tạo database nếu chưa tồn tại }
  if not Exec(ExpandConstant('{cmd}'),
    '/C sqlcmd -S {#SqlInstance} -E -Q "CREATE DATABASE [QLKNL_PIZZA]"',
    '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    MsgBox('Không thể tạo database.' + #13#10 +
           'Database sẽ được tạo tự động khi bạn chạy ứng dụng lần đầu.',
           mbInformation, MB_OK);
    Result := True;
    Exit;
  end;

  { Bước 2: Import dữ liệu từ file .sql vào database }
  if Exec(ExpandConstant('{cmd}'),
    '/C sqlcmd -S {#SqlInstance} -E -d QLKNL_PIZZA -i "' + SqlFilePath + '"',
    '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    Result := True;
    MsgBox('Đã tạo và nhập dữ liệu vào cơ sở dữ liệu "QLKNL_PIZZA" thành công!' + #13#10 + #13#10 +
           'Bạn có thể chạy ứng dụng ngay bây giờ.',
           mbInformation, MB_OK);
  end
  else
  begin
    Result := True;
    MsgBox('Đã tạo database nhưng có lỗi khi nhập dữ liệu.' + #13#10 +
           'Mã lỗi: ' + IntToStr(ResultCode) + #13#10 + #13#10 +
           'Ứng dụng sẽ thử nhập lại dữ liệu khi chạy lần đầu.',
           mbInformation, MB_OK);
  end;
end;

procedure CheckAndSetupDatabase();
var
  ErrorCode: Integer;
begin
  if IsSqlServerAvailable() then
  begin
    { SQL Server đang chạy → Kiểm tra database }
    if IsDatabaseExists() then
    begin
      { Database đã tồn tại }
      MsgBox('Phát hiện cơ sở dữ liệu "QLKNL_PIZZA" đã tồn tại.' + #13#10 + #13#10 +
             'Dữ liệu hiện tại được giữ nguyên.' + #13#10 +
             'Ứng dụng đã sẵn sàng sử dụng!',
             mbInformation, MB_OK);
    end
    else
    begin
      { Database chưa tồn tại → Tạo và import tự động }
      MsgBox('SQL Server đã được phát hiện!' + #13#10 + #13#10 +
             'Đang tiến hành tạo cơ sở dữ liệu "QLKNL_PIZZA"...' + #13#10 +
             'Vui lòng đợi trong giây lát.',
             mbInformation, MB_OK);
      CreateAndImportDatabase();
    end;
  end
  else
  begin
    { SQL Server chưa cài đặt hoặc chưa chạy }
    if MsgBox('Không tìm thấy SQL Server trên máy tính!' + #13#10 + #13#10 +
              'Ứng dụng cần SQL Server để lưu trữ dữ liệu.' + #13#10 +
              'Vui lòng cài đặt SQL Server Express (miễn phí) trước khi sử dụng.' + #13#10 + #13#10 +
              'Hướng dẫn:' + #13#10 +
              '1. Nhấn [Yes] để mở trang tải SQL Server Express' + #13#10 +
              '2. Tải và cài đặt SQL Server Express' + #13#10 +
              '3. Khởi động lại ứng dụng để tự động tạo database' + #13#10 + #13#10 +
              'Nhấn [No] để bỏ qua bước này.',
              mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open', SQL_SERVER_DOWNLOAD_URL, '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
      MsgBox('Trình duyệt đã được mở để tải SQL Server Express.' + #13#10 + #13#10 +
             'Sau khi cài đặt xong SQL Server:' + #13#10 +
             '1. Đảm bảo SQL Server service đang chạy' + #13#10 +
             '2. Khởi động ứng dụng QuanLyKhoNguyenLieuPizza' + #13#10 +
             '3. Database sẽ được tạo tự động',
             mbInformation, MB_OK);
    end;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    CheckAndSetupDatabase();
  end;
end;
