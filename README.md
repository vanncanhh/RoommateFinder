# RoommateFinder — Ứng dụng web tìm người ở ghép

Đồ án tốt nghiệp. Nền tảng kết nối người **đang có phòng cần thêm người** với người **đang tìm phòng ở ghép**: đăng tin, tìm kiếm theo khu vực / giá / khoảng cách tới trường–công ty, gửi yêu cầu kết nối, nhắn tin realtime, đánh giá sau khi ở ghép, báo cáo vi phạm, kiểm duyệt và quản trị.

| Thành phần | Công nghệ |
|---|---|
| Frontend | Blazor WebAssembly (.NET 8) + Bootstrap 5, font Be Vietnam Pro |
| Backend | ASP.NET Core Web API (.NET 8), JWT, SignalR (chat & thông báo realtime) |
| Dữ liệu | Entity Framework Core 8 + Microsoft SQL Server |
| Kiểm thử | xUnit — 60 unit test + 6 integration test trên SQL Server thật |

---

## 1. Cần cài trước

| Phần mềm | Ghi chú |
|---|---|
| [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0) | Bắt buộc. Kiểm tra: `dotnet --list-sdks` có dòng `8.0.x` |
| SQL Server 2019+ | **SQL Server Express** (khuyên dùng) hoặc **LocalDB** đi kèm Visual Studio |
| Visual Studio 2022 (17.8+) | Không bắt buộc; chọn workload *ASP.NET and web development* |
| Git | Để clone mã nguồn |

## 2. Lấy mã nguồn và cấu hình (một lần)

```powershell
git clone https://github.com/vanncanhh/RoommateFinder.git
cd RoommateFinder
.\setup-dev.ps1
```

`setup-dev.ps1` kiểm tra .NET 8, cài công cụ `dotnet-ef`, rồi lưu **chuỗi kết nối SQL Server** và **khóa JWT sinh ngẫu nhiên** vào *User Secrets* (lưu ngoài thư mục dự án, không bao giờ lên Git).

- Dùng LocalDB thay cho SQL Server Express: `.\setup-dev.ps1 -Server "(localdb)\MSSQLLocalDB"`
- Đăng nhập SQL bằng tài khoản: `.\setup-dev.ps1 -ConnectionString "Server=.;Database=RoommateFinderDB;User Id=sa;Password=<mật khẩu>;TrustServerCertificate=True"`
- Nếu PowerShell chặn chạy script: `powershell -ExecutionPolicy Bypass -File .\setup-dev.ps1`

**Không cần tạo CSDL bằng tay**: lần chạy API đầu tiên (môi trường Development) tự áp migration, tạo CSDL `RoommateFinderDB` và nạp dữ liệu demo.

## 3. Chạy ứng dụng

### Cách A — Visual Studio 2022

1. Mở `RoommateFinder.sln`.
2. Chuột phải **Solution** → **Configure Startup Projects…** → chọn **Multiple startup projects** → đặt `RoommateFinder.Api` và `RoommateFinder.Web` là **Start** (các project còn lại để *None*). Ở cột *Launch profile* chọn **http** cho cả hai.
3. Nhấn **F5**. Trình duyệt mở `http://localhost:5173` (giao diện) và `http://localhost:5080/swagger` (tài liệu API).

### Cách B — dòng lệnh (2 cửa sổ)

```powershell
dotnet run --project backend/src/RoommateFinder.Api --launch-profile http   # API:  http://localhost:5080
dotnet run --project web/RoommateFinder.Web --launch-profile http           # Web:  http://localhost:5173
```

Mở **http://localhost:5173**. API phải chạy trước hoặc cùng lúc với Web.

## 4. Tài khoản demo

| Vai trò | Email | Mật khẩu |
|---|---|---|
| Quản trị viên | `admin@roommate.test` | `Admin@123` |
| Kiểm duyệt viên | `mod1@roommate.test`, `mod2@roommate.test` | `Mod@12345` |
| Người dùng | `user1@roommate.test` … `user6@roommate.test` | `User@1234` |

Trang đăng nhập có sẵn mục *Tài khoản demo* để điền nhanh. Gợi ý trải nghiệm: đăng nhập `user1` ở một trình duyệt và `user3` ở cửa sổ ẩn danh, mở **Tin nhắn** để thấy chat realtime.

Muốn **làm mới dữ liệu demo**: xóa CSDL `RoommateFinderDB` (SSMS hoặc `sqlcmd -S .\SQLEXPRESS -E -Q "DROP DATABASE RoommateFinderDB"`) rồi chạy lại API.

## 5. Xử lý lỗi thường gặp

| Hiện tượng | Cách xử lý |
|---|---|
| API báo *Thiếu ConnectionStrings:Default* hoặc *Thiếu Jwt:Key* | Chưa chạy `.\setup-dev.ps1` (mục 2) |
| *A network-related or instance-specific error… / Cannot open database* | Sai tên instance SQL Server. Kiểm tra dịch vụ `SQL Server (SQLEXPRESS)` đang chạy, hoặc chạy lại `setup-dev.ps1 -Server "<tên instance>"` |
| Giao diện báo *Không kết nối được máy chủ* | API chưa chạy ở `http://localhost:5080`; nếu đổi cổng API thì sửa `web/RoommateFinder.Web/wwwroot/appsettings.json` (`ApiBaseUrl`) |
| *Address already in use* (cổng 5080 / 5173) | Tắt tiến trình đang chiếm cổng hoặc đổi cổng trong `Properties/launchSettings.json` (đổi cổng Web thì thêm vào `Cors:Origins` trong `appsettings.json` của Api) |
| Đăng nhập bị khóa 15 phút | Do nhập sai mật khẩu 5 lần (quy tắc chống dò mật khẩu) — chờ hết thời gian hoặc làm mới dữ liệu demo |
| Đăng tin báo *chỉ được đăng tối đa 3 tin trong 24 giờ* | Quy tắc chống spam; dùng tài khoản demo khác hoặc Admin sửa `POST_MAX_PER_DAY` ở trang **Quản trị → Cấu hình** |
| Email quên mật khẩu không tới | Chưa cấu hình SMTP: nội dung email (kèm đường dẫn đặt lại) được ghi ra cửa sổ log của API |

## 6. Kiểm thử

```powershell
dotnet test RoommateFinder.sln
```

- `RoommateFinder.UnitTests` (60 test): quy tắc nghiệp vụ BR-02, BR-05…BR-09, khóa đăng nhập, quên/đổi mật khẩu, tính khoảng cách Haversine, kiểm tra định dạng ảnh, các lỗi phát hiện khi review.
- `RoommateFinder.IntegrationTests` (6 test): chạy trên CSDL riêng `RoommateFinderDB_Test` (tự tạo, tự xóa) — 5 lần chấp nhận song song vào tin còn 2 chỗ thì đúng 2 thành công (NFR-08); chặn yêu cầu trùng bằng filtered unique index; đóng tin và chấp nhận cùng lúc không deadlock; hai kiểm duyệt viên xử lý cùng báo cáo chỉ một người thành công. Đổi chuỗi kết nối bằng biến môi trường `ROOMMATE_TEST_DB`.

## 7. Cấu trúc mã nguồn

```
backend/
  src/RoommateFinder.Domain           thực thể (18 bảng), hằng số trạng thái
  src/RoommateFinder.Application      DTO, interface, service nghiệp vụ (không phụ thuộc EF/ASP.NET)
  src/RoommateFinder.Infrastructure   EF Core DbContext, Fluent API, migration, repository, JWT, BCrypt, email, lưu ảnh
  src/RoommateFinder.Api              controller, SignalR hub, middleware, tác vụ nền
  tests/                              UnitTests, IntegrationTests
web/RoommateFinder.Web/               Blazor WebAssembly + Bootstrap (quy ước: web/CONVENTIONS.md)
frontend/                             bản React cũ — giữ để tham khảo, không dùng khi chạy
docs/                                 báo cáo, kế hoạch triển khai, hợp đồng API, script SQL
setup-dev.ps1                         cấu hình môi trường chạy thử (mục 2)
```

Tài liệu liên quan:
- Hợp đồng API giữa frontend và backend: [docs/API.md](docs/API.md) (Swagger: `http://localhost:5080/swagger`).
- Script CSDL sinh từ migration: `docs/InitialCreate.sql` (đối chiếu thiết kế `docs/RoommateFinderDB_v2.sql`).
- Kế hoạch triển khai và các quyết định thiết kế: [docs/KeHoach_TrienKhai_RoommateFinder.md](docs/KeHoach_TrienKhai_RoommateFinder.md).
