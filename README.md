# RoommateFinder — Ứng dụng web tìm người ở ghép

Đồ án tốt nghiệp. Blazor WebAssembly + Bootstrap 5 (frontend) · ASP.NET Core Web API .NET 8 (backend) · EF Core 8 + SQL Server · SignalR. Mở toàn bộ bằng `RoommateFinder.sln` trong Visual Studio 2022.

## Yêu cầu

| Công cụ | Phiên bản |
|---|---|
| .NET SDK | 8.0.x (repo ghim bằng `global.json`) |
| SQL Server | 2019+ (Express được), instance `.\SQLEXPRESS` |

## Chạy lần đầu

```powershell
# 1. Công cụ EF Core (local tool, bản 8)
dotnet tool restore

# 2. Bí mật cấu hình — không commit
cd backend/src/RoommateFinder.Api
dotnet user-secrets set "ConnectionStrings:Default" "Server=.\SQLEXPRESS;Database=RoommateFinderDB;Trusted_Connection=True;TrustServerCertificate=True"
dotnet user-secrets set "Jwt:Key" "<chuỗi ngẫu nhiên tối thiểu 32 ký tự>"

# 3. Chạy API — môi trường Development tự áp migration và tạo dữ liệu demo
dotnet run --launch-profile http        # http://localhost:5080/swagger

# 4. Frontend Blazor (cửa sổ khác)
cd web/RoommateFinder.Web
dotnet run --launch-profile http        # http://localhost:5173
```

Gửi email quên mật khẩu: khi chưa cấu hình `Smtp:Host`, nội dung email (kèm đường dẫn đặt lại) được ghi ra log của API.

## Tài khoản demo

| Vai trò | Email | Mật khẩu |
|---|---|---|
| Admin | `admin@roommate.test` | `Admin@123` |
| Moderator | `mod1@roommate.test`, `mod2@roommate.test` | `Mod@12345` |
| User | `user1@roommate.test` … `user6@roommate.test` | `User@1234` |

Muốn tạo lại dữ liệu demo từ đầu: xóa CSDL `RoommateFinderDB` rồi chạy lại API.

## Kiểm thử

```powershell
dotnet test RoommateFinder.sln
```

- `RoommateFinder.UnitTests` (60 test): quy tắc nghiệp vụ BR-02, BR-05…BR-09, khóa đăng nhập, quên mật khẩu, Haversine, kiểm tra ảnh, các lỗi phát hiện khi review (BR-07 đếm lại báo cáo đã bỏ qua, hạ cấp khóa vĩnh viễn, lộ email...) — dùng fake repository viết tay.
- `RoommateFinder.IntegrationTests` (6 test): chạy trên CSDL riêng `RoommateFinderDB_Test` (tự tạo và tự xóa) — 5 yêu cầu chấp nhận song song vào tin còn 2 chỗ thì đúng 2 thành công, 3 nhận 409 (NFR-08); filtered unique index chặn yêu cầu trùng; đóng tin và chấp nhận kết nối cùng lúc không deadlock; hai kiểm duyệt viên xử lý cùng một báo cáo thì chỉ một người thành công. Đổi chuỗi kết nối bằng biến môi trường `ROOMMATE_TEST_DB`.

## Cấu trúc

```
backend/
  src/RoommateFinder.Domain           entity (18 bảng), hằng số trạng thái
  src/RoommateFinder.Application      DTO, interface, Service nghiệp vụ (không phụ thuộc EF/ASP.NET)
  src/RoommateFinder.Infrastructure   EF Core DbContext, Fluent API, migration, repository, JWT, BCrypt, email, lưu file
  src/RoommateFinder.Api              Controller, SignalR hub, middleware, tác vụ nền
  tests/                              UnitTests, IntegrationTests
web/RoommateFinder.Web/               Blazor WebAssembly + Bootstrap (quy ước: web/CONVENTIONS.md)
frontend/                             bản React cũ — giữ tham khảo, sẽ xóa khi bản Blazor hoàn tất
docs/                                 báo cáo, kế hoạch, hợp đồng API (API.md), script SQL
```

- Hợp đồng API giữa backend và frontend: [docs/API.md](docs/API.md).
- Script tạo CSDL sinh từ migration: `docs/InitialCreate.sql` (đối chiếu với `docs/RoommateFinderDB_v2.sql`).
- Kế hoạch triển khai và các quyết định thiết kế (a)–(h): [docs/KeHoach_TrienKhai_RoommateFinder.md](docs/KeHoach_TrienKhai_RoommateFinder.md).

Trong Visual Studio 2022: chuột phải Solution → Configure Startup Projects → Multiple startup projects → chọn `RoommateFinder.Api` và `RoommateFinder.Web` là Start, rồi F5 để chạy cả hai.
