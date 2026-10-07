<#
.SYNOPSIS
    Cấu hình môi trường chạy thử RoommateFinder trên máy mới (chạy MỘT lần sau khi clone).

.DESCRIPTION
    - Kiểm tra .NET SDK 8.
    - Cài công cụ dotnet-ef (local tool, bản 8).
    - Lưu chuỗi kết nối SQL Server và khóa JWT ngẫu nhiên vào User Secrets của project Api
      (lưu ngoài thư mục dự án, KHÔNG nằm trong Git).
    CSDL và dữ liệu demo được tạo tự động ở lần chạy API đầu tiên (môi trường Development).

.EXAMPLE
    .\setup-dev.ps1
    .\setup-dev.ps1 -Server "(localdb)\MSSQLLocalDB"
    .\setup-dev.ps1 -ConnectionString "Server=.;Database=RoommateFinderDB;User Id=sa;Password=...;TrustServerCertificate=True"
#>
param(
    # Tên instance SQL Server. Mặc định SQL Server Express; dùng "(localdb)\MSSQLLocalDB" nếu chỉ có LocalDB đi kèm Visual Studio.
    [string]$Server = ".\SQLEXPRESS",
    [string]$Database = "RoommateFinderDB",
    # Chuỗi kết nối đầy đủ (ví dụ đăng nhập SQL bằng tài khoản sa) — nếu có thì bỏ qua -Server/-Database.
    [string]$ConnectionString
)

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot
$api = "backend/src/RoommateFinder.Api"

Write-Host "1/3 Kiểm tra .NET SDK 8..." -ForegroundColor Cyan
$sdk = (dotnet --version) 2>$null
if (-not $sdk -or -not $sdk.StartsWith("8.")) {
    throw "Cần .NET SDK 8.0 (đang có: '$sdk'). Tải tại https://dotnet.microsoft.com/download/dotnet/8.0"
}
Write-Host "    .NET SDK $sdk"

Write-Host "2/3 Cài công cụ dotnet-ef..." -ForegroundColor Cyan
dotnet tool restore | Out-Null

Write-Host "3/3 Lưu cấu hình vào User Secrets (ngoài thư mục dự án)..." -ForegroundColor Cyan
if (-not $ConnectionString) {
    $ConnectionString = "Server=$Server;Database=$Database;Trusted_Connection=True;TrustServerCertificate=True"
}
$bytes = New-Object byte[] 48
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$jwtKey = [Convert]::ToBase64String($bytes)

dotnet user-secrets set "ConnectionStrings:Default" $ConnectionString --project $api | Out-Null
dotnet user-secrets set "Jwt:Key" $jwtKey --project $api | Out-Null
Write-Host "    ConnectionStrings:Default = $ConnectionString"
Write-Host "    Jwt:Key = (đã sinh ngẫu nhiên, 64 ký tự)"

Write-Host ""
Write-Host "Xong! Chạy dự án:" -ForegroundColor Green
Write-Host "  - Visual Studio 2022: mở RoommateFinder.sln, chọn Multiple startup projects (Api + Web), nhấn F5."
Write-Host "  - Hoặc 2 cửa sổ dòng lệnh:"
Write-Host "      dotnet run --project backend/src/RoommateFinder.Api --launch-profile http   (API  http://localhost:5080)"
Write-Host "      dotnet run --project web/RoommateFinder.Web --launch-profile http           (Web  http://localhost:5173)"
