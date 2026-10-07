# KẾ HOẠCH TRIỂN KHAI DỰ ÁN
## Hệ thống tìm người ở ghép (RoommateFinder)
**Stack:** Blazor WebAssembly + Bootstrap 5 (frontend; đổi từ ReactJS ngày 07/10/2026 theo yêu cầu) · ASP.NET Core Web API .NET 8 (backend) · Entity Framework Core 8 + SQL Server 2022 Express (data) · SignalR (chat, thông báo realtime)

> Nguyên tắc xuyên suốt: Controller/Service **chỉ phụ thuộc interface** (`IPostRepository`, `IUnitOfWork`, `IPasswordHasher`...). Không được `new` trực tiếp `DbContext`, lớp Repository hay `HubContext` ở tầng nghiệp vụ. Mọi quy tắc nghiệp vụ (BR-xx) nằm ở Service, frontend chỉ ẩn/hiện giao diện — backend vẫn phải chặn.

**Tài liệu căn cứ:** báo cáo Chương 1–3 (`ĐỒ ÁN TỐT NGHIỆP.docx`), tài liệu "Bổ sung phân tích Chương 3" (UC mới, sơ đồ trạng thái, BR-01…BR-10, NFR-01…NFR-12), script `RoommateFinderDB_v2.sql` (18 bảng).

---

## GIAI ĐOẠN 0 — Khởi tạo dự án (1 ngày)

| # | Việc cần làm | Deliverable |
|---|---|---|
| 0.1 | Cài bản vá mới nhất của .NET 8 SDK (máy đang có 8.0.101 và 9.0.308, mặc định là 9); tạo `global.json` ghim SDK 8 (`rollForward: latestFeature`) | `dotnet --version` trong thư mục repo ra 8.0.x |
| 0.2 | Tạo solution 4 project + 2 project test: `RoommateFinder.Domain`, `RoommateFinder.Application`, `RoommateFinder.Infrastructure`, `RoommateFinder.Api`, `RoommateFinder.UnitTests`, `RoommateFinder.IntegrationTests` | Solution build thành công, Api rỗng chạy được |
| 0.3 | Cài NuGet cơ bản: `Microsoft.EntityFrameworkCore.SqlServer`, `.Design` (8.x) cho Infrastructure; `Microsoft.AspNetCore.Authentication.JwtBearer` (8.x), `Serilog.AspNetCore` cho Api; `BCrypt.Net-Next`, `MailKit` cho Infrastructure; `dotnet-ef` 8.x làm local tool | `dotnet restore` không lỗi; sửa `Version="8.*"` thành số cụ thể |
| 0.4 | Tạo frontend bằng Vite (`react-ts`), cài `react-router-dom`, `axios`, `@tanstack/react-query`, `antd`, `@microsoft/signalr`, `dayjs` | `npm run dev` mở được trang trắng |
| 0.5 | Thiết lập Git repo, `.gitignore` (thêm `wwwroot/uploads/`, `.env*.local`), quy ước nhánh (`main`/`develop`/`feature/*`), chuyển docx + script SQL vào `docs/` | Repo GitHub riêng tư có README |
| 0.6 | Cấu hình bí mật bằng `dotnet user-secrets`: `ConnectionStrings:Default` (`.\SQLEXPRESS`, Windows Authentication), `Jwt:Key`, `Smtp:*` | Không có bí mật nào trong Git |
| 0.7 | Thống nhất coding convention (PascalCase entity trùng tên bảng số ít, tên DTO theo usecase, hằng số trạng thái trong `Domain/Constants`) | Tài liệu convention ngắn (1 trang) |

```powershell
dotnet new globaljson --sdk-version 8.0.100 --roll-forward latestFeature
dotnet new tool-manifest; dotnet tool install dotnet-ef --version "8.*"
dotnet new sln -n RoommateFinder
dotnet new classlib -n RoommateFinder.Domain -o backend/src/RoommateFinder.Domain -f net8.0
dotnet new classlib -n RoommateFinder.Application -o backend/src/RoommateFinder.Application -f net8.0
dotnet new classlib -n RoommateFinder.Infrastructure -o backend/src/RoommateFinder.Infrastructure -f net8.0
dotnet new webapi -n RoommateFinder.Api -o backend/src/RoommateFinder.Api --use-controllers -f net8.0
dotnet new xunit -n RoommateFinder.UnitTests -o backend/tests/RoommateFinder.UnitTests -f net8.0
dotnet new xunit -n RoommateFinder.IntegrationTests -o backend/tests/RoommateFinder.IntegrationTests -f net8.0
dotnet sln add (Get-ChildItem backend -Recurse -Filter *.csproj).FullName
dotnet add backend/src/RoommateFinder.Application reference backend/src/RoommateFinder.Domain
dotnet add backend/src/RoommateFinder.Infrastructure reference backend/src/RoommateFinder.Application backend/src/RoommateFinder.Domain
dotnet add backend/src/RoommateFinder.Api reference backend/src/RoommateFinder.Application backend/src/RoommateFinder.Infrastructure
dotnet add backend/tests/RoommateFinder.UnitTests reference backend/src/RoommateFinder.Application
dotnet add backend/tests/RoommateFinder.IntegrationTests reference backend/src/RoommateFinder.Api
npm create vite@latest frontend -- --template react-ts
```

**Điều kiện hoàn thành:** project reference đúng chiều (`Domain` không tham chiếu project nào; `Application` → `Domain`; `Infrastructure` → `Application` + `Domain`; `Api` → `Application` + `Infrastructure` — `Infrastructure` chỉ được dùng trong `Program.cs` để đăng ký DI); `dotnet build` toàn solution pass; `npm run build` pass.

---

## GIAI ĐOẠN 1 — Domain & Application layer (2–3 ngày)

Dựng "bộ khung" dữ liệu và hợp đồng, chưa cần chạy được, chỉ cần biên dịch đúng.

| # | Việc cần làm | Căn cứ trong báo cáo |
|---|---|---|
| 1.1 | Viết 18 entity trong `Domain/Entities`: User, Area, Landmark, Amenity, ReportReason, SystemConfig, PasswordResetToken, Post, PostImage, PostAmenity, SavedPost, ConnectionRequest, Conversation, Message, Review, Report, ViolationHistory, Notification | Mục 3.7.1 — danh sách lớp; **cột dữ liệu đầy đủ lấy từ `RoommateFinderDB_v2.sql`**, không lấy từ Mục 3.8.3/3.8.5 bản cũ (bản cũ chỉ có 16 bảng và còn lỗi ràng buộc) |
| 1.2 | Viết hằng số trạng thái trong `Domain/Constants`: `PostStatus`, `PostType`, `RequestStatus`, `ReportStatus`, `UserStatus`, `Roles`, `ViolationAction`, `ConfigKeys` | Sơ đồ trạng thái tin đăng, yêu cầu kết nối (tài liệu bổ sung) |
| 1.3 | Viết DTO cho từng usecase: `RegisterDto`, `LoginDto`, `ForgotPasswordDto`, `ResetPasswordDto`, `ChangePasswordDto`, `UpdateProfileDto`, `CreatePostDto`, `UpdatePostDto`, `PostSearchQuery`, `PostSummaryDto`, `PostDetailDto`, `RejectPostDto`, `CreateConnectionRequestDto`, `ConnectionRequestDto`, `MessageDto`, `CreateReviewDto`, `CreateReportDto`, `HandleReportDto`, `UpdateUserRoleDto`, `UpdateUserStatusDto`, `DashboardDto`, `PagedResult<T>` | Mục 3.4 — 8 bảng đặc tả + 7 UC bổ sung |
| 1.4 | Viết interface Repository: `IUserRepository`, `IPostRepository`, `ISavedPostRepository`, `ICatalogRepository` (Area, Landmark, Amenity, ReportReason), `IConnectionRequestRepository`, `IConversationRepository` (gồm Message), `IReviewRepository`, `IReportRepository`, `IViolationRepository`, `INotificationRepository`, `ISystemConfigRepository`, `IPasswordResetTokenRepository` và **`IUnitOfWork`** (mở/commit/rollback giao dịch) | — |
| 1.5 | Viết interface hạ tầng phụ: `IPasswordHasher`, `IJwtTokenGenerator`, `IEmailSender`, `IFileStorage`, `IRealtimeNotifier` (bọc SignalR), `IDateTimeProvider` (lấy giờ UTC — để test được các quy tắc theo thời gian) | Mục 2.7, 2.8 |
| 1.6 | Viết lỗi nghiệp vụ dùng chung: `BusinessException` với mã `Validation` (400), `Forbidden` (403), `NotFound` (404), `Conflict` (409), `TooManyRequests` (429) | Quy ước API trong kế hoạch IT |
| 1.7 | Viết class Service rỗng (constructor inject interface, method throw `NotImplementedException`): `AuthService`, `ProfileService`, `PostService`, `PostSearchService`, `ModerationService`, `ConnectionService`, `ChatService`, `ReviewService`, `ReportService`, `AdminService`, `DashboardService`, `NotificationService` | Mục 3.5, 3.6 — biểu đồ hoạt động/tuần tự |

**Điều kiện hoàn thành:** build pass; review code — không entity/DTO/Service nào tham chiếu tới EF Core, SignalR hay `Microsoft.AspNetCore.*`.

---

## ⚠️ CHỐT QUYẾT ĐỊNH THIẾT KẾ TRƯỚC KHI SANG GIAI ĐOẠN 2

Các điểm dưới đây đã được đối chiếu giữa báo cáo, tài liệu bổ sung và kế hoạch, cần thống nhất **trước khi dev bắt đầu viết Repository/Service**, tránh phải sửa lại giữa chừng.

**a) Đi thẳng SQL Server, không làm giai đoạn Mock in-memory**
Máy dev đã có SQL Server 2022 Express đang chạy (`.\SQLEXPRESS`) và script v2 đã chạy thử thành công. Hai quy tắc quan trọng nhất của hệ thống — trừ chỗ có điều kiện (điểm c) và filtered unique index (điểm d) — **chỉ kiểm chứng được trên DB thật**, Mock sẽ cho kết quả sai lệch. Unit test Service vẫn dùng fake repository viết tay trong project `UnitTests`; test tranh chấp chạy trên CSDL riêng `RoommateFinderDB_Test` trong project `IntegrationTests`.

**b) Code-first, script SQL chỉ dùng để đối chiếu**
Không chạy tay `RoommateFinderDB_v2.sql` để tạo CSDL làm việc. Viết 18 `IEntityTypeConfiguration<T>` (khóa, độ dài, `HasPrecision`, `HasCheckConstraint`, `HasFilter`, `HasData` cho SystemConfigs/ReportReasons/Amenities) rồi sinh migration `InitialCreate`. Xuất `dotnet ef migrations script` và so với script v2: đủ 18 bảng, 2 filtered unique index, các CHECK constraint. Mọi khóa ngoại dùng `DeleteBehavior.Restrict` trừ `PostImages`, `PostAmenities` (cascade) — vì tin đăng và người dùng chỉ xóa mềm.

**c) Chấp nhận kết nối: UPDATE có điều kiện trong giao dịch, không đọc-rồi-ghi**
`ConnectionService.AcceptAsync` không được làm kiểu "đọc `NeededOccupants` → kiểm tra > 0 → trừ 1 → `SaveChanges`" vì hai chủ tin bấm gần như đồng thời sẽ cùng đọc thấy còn chỗ. Bắt buộc:
1. `IUnitOfWork.BeginTransactionAsync()`.
2. `IPostRepository.TryTakeSlotAsync(postId)` cài bằng `ExecuteUpdateAsync` với điều kiện `NeededOccupants > 0 && Status == approved && !IsDeleted`, trả về số dòng ảnh hưởng.
3. Bằng 0 → rollback, ném `Conflict` ("Tin đã đủ người"). Bằng 1 → cập nhật request `accepted`, tạo `Conversation`, tạo `Notification`; nếu `NeededOccupants` vừa về 0 thì tin sang `closed` và các request `pending` còn lại sang `expired`.
4. Commit, rồi mới gọi `IRealtimeNotifier` (không đẩy thông báo cho giao dịch chưa chắc thành công).

**d) Lỗi trùng khóa: để DB bắt, map thành 409**
`UQ_Request_SenderPost_Active` và `UQ_Report_ReporterPost_Pending` là **filtered unique index** — SQL Server ném `SqlException.Number == 2601` (không phải 2627 như UNIQUE constraint). Các UNIQUE constraint khác (`UQ_Review_ReviewerRequest`, `UQ_Saved_UserPost`, `Users.Email`) ném 2627. Middleware xử lý lỗi phải bắt `DbUpdateException` có inner `SqlException` số **2601 hoặc 2627** và trả `409 Conflict`. Service vẫn kiểm tra trước để trả lỗi thân thiện, nhưng không tin hoàn toàn bước kiểm tra đó.

**e) Lọc theo khoảng cách: bounding box trong SQL + Haversine trong C#**
Không dùng kiểu `geography` (ngoài phạm vi báo cáo). `PostSearchService` lọc thô bằng khung vĩ độ/kinh độ quanh địa điểm mốc (± bán kính quy đổi ra độ) ngay trong truy vấn SQL, sau đó tính Haversine chính xác trên tập đã thu hẹp, sắp xếp theo khoảng cách rồi mới phân trang. Tin không có tọa độ dùng tọa độ tâm khu vực (BR-01). Với quy mô demo (~1.000 tin) cách này đủ đáp ứng NFR-01; ghi rõ giới hạn này ở Chương 4.

**f) Thời gian luôn lưu UTC, mọi quy tắc theo thời gian đi qua `IDateTimeProvider`**
`LockedUntil`, `ExpiredAt`, `SuspendedUntil`, điều kiện đánh giá sau 7 ngày (BR-05), ngưỡng báo cáo 24 giờ (BR-07) đều so với `IDateTimeProvider.UtcNow`, không gọi `DateTime.Now` trong Service. Frontend đổi sang GMT+7 khi hiển thị; dashboard nhóm theo ngày GMT+7.

**g) Giá trị cấu hình đọc từ `SystemConfigs`, không hard-code**
Hạn tin (30 ngày), số lần gia hạn (3), ngưỡng tự ẩn (5 báo cáo/24 giờ), khóa đăng nhập (5 lần/15 phút)... đọc qua `ISystemConfigRepository` có cache, xóa cache khi Admin sửa. Các giá trị này vẫn là **đề xuất chờ chốt** — chốt khác chỉ cần sửa dữ liệu, không sửa code.

**h) Phạm vi realtime**
Chat và thông báo dùng SignalR (`ChatHub`, `NotificationHub`, xác thực JWT qua `access_token` trên query string). Nếu SignalR trễ tiến độ, **phương án dự phòng** là tải lại định kỳ 10 giây — đã ghi trong bảng rủi ro của kế hoạch IT; không ảnh hưởng các usecase còn lại.

---

## GIAI ĐOẠN 2 — Infrastructure & khung API (3–4 ngày)

| # | Việc cần làm |
|---|---|
| 2.1 | Viết `AppDbContext` + 18 file cấu hình Fluent API trong `Infrastructure/Data/Configurations` — xem quyết định (b) |
| 2.2 | `dotnet ef migrations add InitialCreate` → xuất script đối chiếu với `RoommateFinderDB_v2.sql` → `dotnet ef database update` |
| 2.3 | Viết `Ef*Repository` cho mọi interface ở 1.4 và `EfUnitOfWork` (truy vấn chỉ đọc dùng `AsNoTracking`) |
| 2.4 | Viết `BcryptPasswordHasher`, `JwtTokenGenerator` (chứa `UserId`, `Role`, hạn `JWT_ACCESS_MINUTES`), `SmtpEmailSender` (môi trường dev: ghi email ra log), `LocalFileStorage` (`wwwroot/uploads`, tên file GUID), `SystemDateTimeProvider` |
| 2.5 | Tạo `Api/Program.cs`: đăng ký DI toàn bộ repository/service, JWT Bearer, phân quyền theo Role, Swagger có nút nhập Bearer token, CORS cho `http://localhost:5173`, Serilog (console + file `logs/`) |
| 2.6 | Viết middleware xử lý lỗi tập trung: `BusinessException` → mã tương ứng, lỗi trùng khóa → 409 (quyết định d), lỗi khác → 500 không lộ stack trace; định dạng `{ code, message }` tiếng Việt |
| 2.7 | Viết middleware kiểm tra `Users.Status` mỗi request (tài khoản bị khóa giữa phiên bị chặn ngay) |
| 2.8 | Tạo Controller rỗng cho từng nhóm: `AuthController`, `ProfileController`, `PostsController`, `ModerationController`, `ConnectionsController`, `ConversationsController`, `ReviewsController`, `ReportsController`, `NotificationsController`, `CatalogController`, `AdminController`, `HealthController` — mỗi action gọi Service tương ứng |
| 2.9 | Seed dữ liệu demo (chỉ môi trường Development): 1 thành phố, ~10 quận, ~20 trường/công ty có tọa độ, 1 admin, 2 moderator, 5 user |

**Điều kiện hoàn thành:** chạy `dotnet run` project Api, mở Swagger UI thấy đủ endpoint; `GET /api/health` trả 200; `GET /api/catalog/areas` trả dữ liệu seed đọc từ SQL Server; frontend gọi được `/api/health`.

---

## GIAI ĐOẠN 3 — Cài đặt nghiệp vụ backend theo usecase (10–12 ngày)

Làm lần lượt theo đúng thứ tự phụ thuộc (tài khoản trước vì mọi usecase sau cần JWT; tin đăng và duyệt tin trước kết nối vì chỉ tin `approved` mới nhận yêu cầu).

| # | Usecase | Việc cần làm | Tham chiếu |
|---|---|---|---|
| 3.1 | Đăng ký | `AuthService.RegisterAsync`: chuẩn hóa email chữ thường, kiểm tra trùng, kiểm tra mật khẩu mạnh (≥ 8 ký tự, hoa, thường, số), băm BCrypt, Role mặc định `user` | Hình 3-2, 3-10, 3-18 |
| 3.2 | Đăng nhập | `AuthService.LoginAsync`: chặn `suspended`/`banned`, kiểm tra `LockedUntil`, sai thì tăng `FailedLoginAttempts` và khóa 15 phút khi đủ 5 lần, đúng thì reset bộ đếm và cấp JWT; thông báo không tiết lộ email có tồn tại | Hình 3-3, 3-11, 3-19 |
| 3.3 | Quên / Đổi mật khẩu | `AuthService.ForgotPasswordAsync`, `ResetPasswordAsync`, `ChangePasswordAsync`: token lưu bản băm, dùng 1 lần, hạn 30 phút; giới hạn 3 yêu cầu/15 phút; luôn trả cùng một thông báo | UC bổ sung |
| 3.4 | Hồ sơ cá nhân | `ProfileService`: xem/sửa hồ sơ, thông tin sinh hoạt, chọn địa điểm mốc; DTO không chứa `Role`, `Status`, `AvgRating` | Mục 3.2.3 |
| 3.5 | Đăng tin | `PostService.CreateAsync`: kiểm tra theo `PostType`, `Price > 0`, khu vực tồn tại, `POST_MAX_PER_DAY`; lưu `pending`; upload ảnh qua `IFileStorage` (kiểm tra định dạng bằng nội dung file, dung lượng, số ảnh — BR-10); thông báo cho moderator | Hình 3-4, 3-12, 3-20 |
| 3.6 | Quản lý tin của tôi | `PostService.UpdateAsync` (sửa tin `approved`/`rejected` → `pending`), `ExtendAsync` (`ExtendCount` ≤ `POST_MAX_EXTEND`), `CloseAsync`, `DeleteAsync` (xóa mềm); đóng/xóa thì request `pending` → `expired` | UC bổ sung; sơ đồ trạng thái tin |
| 3.7 | Tìm kiếm & lọc tin | `PostSearchService.SearchAsync`: chỉ `approved`, `IsDeleted = 0`, chưa hết hạn; lọc khu vực, giá, giới tính, số người, loại tin, khoảng cách — xem quyết định (e); sắp xếp; phân trang (`pageSize` tối đa 50). Kèm chi tiết tin (`Phone` chỉ trả khi có request `accepted` — NFR-05), tăng `ViewCount`, lưu tin | Hình 3-5, 3-13, 3-21 |
| 3.8 | Duyệt / Từ chối tin | `ModerationService.ApproveAsync` (đặt `ExpiredAt` lúc duyệt — BR-02), `RejectAsync` (bắt buộc `RejectReason`); ghi `ModeratedBy/At`; không duyệt tin của chính mình (BR-09); tạo Notification cho người đăng | Hình 3-7, 3-15, 3-23 |
| 3.9 | Gửi / Rút yêu cầu kết nối | `ConnectionService.SendAsync`: tin `approved`, còn chỗ, không phải tin của mình (BR-04); trùng thì 409 — xem quyết định (d). `CancelAsync` cho người gửi | Hình 3-6, 3-14, 3-22 |
| 3.10 | Xử lý yêu cầu kết nối | `ConnectionService.AcceptAsync` / `RejectAsync` — **làm đúng quyết định (c)** | UC bổ sung; sơ đồ trạng thái yêu cầu |
| 3.11 | Trò chuyện | `ChatService` + `ChatHub`: kiểm tra thành viên ở cả Hub và API, nội dung 1–1000 ký tự, cập nhật `LastMessageAt`, đánh dấu đã đọc, lịch sử phân trang theo `before` | UC bổ sung |
| 3.12 | Thông báo | `NotificationService` + `NotificationHub`: tạo, liệt kê, đánh dấu đã đọc; đẩy realtime sau khi commit | Mục 2.8 |
| 3.13 | Đánh giá người đăng | `ReviewService.CreateAsync`: request `accepted` đủ `REVIEW_MIN_DAYS` ngày (BR-05), mỗi bên 1 lần/lượt kết nối (`UQ_Review_ReviewerRequest`); tính lại `AvgRating`, `ReviewCount` trong cùng giao dịch (BR-06) | Hình 3-9, 3-17, 3-25 |
| 3.14 | Báo cáo vi phạm | `ReportService.CreateAsync`: nhắm đúng 1 đối tượng, không tự báo cáo mình; đủ 5 người khác nhau trong 24 giờ thì tin tự sang `hidden` và báo chủ tin (BR-07) | Hình 3-1 (usecase tổng quát) |
| 3.15 | Xử lý báo cáo vi phạm | `ReportService.HandleAsync`: bỏ qua (`dismissed`) hoặc xử lý 3 mức — cảnh cáo / ẩn tin / khóa `SuspendDays` ngày hoặc vĩnh viễn; khóa thì ẩn mọi tin `approved` (BR-08); ghi `ViolationHistory`, `HandledBy` | Hình 3-8, 3-16, 3-24 |
| 3.16 | Quản lý người dùng & phân quyền | `AdminService`: tìm kiếm, gán Role, khóa/mở khóa; không tự hạ quyền/tự khóa, không hạ quyền Admin cuối cùng | UC bổ sung |
| 3.17 | Danh mục & cấu hình | `AdminService`: CRUD khu vực, địa điểm mốc, tiện ích, lý do báo cáo (ẩn bằng `IsActive`, không xóa cứng); sửa `SystemConfigs` có kiểm tra miền giá trị và xóa cache | UC bổ sung |
| 3.18 | Dashboard thống kê | `DashboardService`: user mới, tin mới theo loại, tỷ lệ duyệt, thời gian duyệt trung bình, kết nối thành công, báo cáo chờ, top 5 khu vực — gom nhóm trong SQL | UC bổ sung |
| 3.19 | Tác vụ nền | `BackgroundService` chạy mỗi giờ: tin quá hạn → `expired` (kèm request `pending`), tài khoản hết hạn khóa → `active` (BR-03); tạo scope `DbContext` mới mỗi lần chạy; chạy lặp lại không sai dữ liệu | Mục 3.8.2 |

**Điều kiện hoàn thành mỗi usecase:** test được bằng Swagger/Postman đúng luồng cơ bản **và** luồng thay thế nêu trong bảng đặc tả (ví dụ: gửi trùng yêu cầu kết nối phải trả 409, không phải 500; gọi API sửa tin bằng token người khác phải trả 403).

**Gợi ý phân công:** nếu có 2 dev, 1 người làm 3.1–3.8 (tài khoản + tin đăng + duyệt tin), 1 người làm 3.9–3.15 (kết nối + chat + đánh giá + báo cáo); 3.16–3.19 chia đôi sau cùng. Người thứ hai bắt đầu khi 3.2 và 3.5 đã merge.

---

## GIAI ĐOẠN 4 — Frontend (10–12 ngày, chạy song song Giai đoạn 3)

> **Cập nhật 07/10/2026:** frontend chuyển sang **Blazor WebAssembly + Bootstrap 5** (`web/RoommateFinder.Web`, quy ước trong `web/CONVENTIONS.md`), dùng chung DTO C# với backend. Các hạng mục 4.1–4.11 dưới đây giữ nguyên về chức năng; phần công nghệ (Axios, React Query, antd) thay bằng `HttpClient` + lớp `*Api`, `AuthenticationStateProvider`, `Microsoft.AspNetCore.SignalR.Client`.

| # | Việc cần làm |
|---|---|
| 4.1 | Thiết lập `src/api/client.ts` (Axios, `baseURL` từ `VITE_API_URL`, interceptor gắn JWT và bắt 401), `AuthContext` lưu token, React Query, `ConfigProvider locale={viVN}`, layout chung |
| 4.2 | Route bảo vệ theo Role: `ProtectedRoute` cho user, `/moderator/*`, `/admin/*` |
| 4.3 | Trang `LoginPage`, `RegisterPage`, `ForgotPasswordPage`, `ResetPasswordPage` |
| 4.4 | Trang `HomePage` (tin mới nhất), `SearchPage` (bộ lọc lưu trên URL query, chọn địa điểm mốc + bán kính), `PostDetailPage` (ảnh, tiện ích, đánh giá người đăng, nút gửi yêu cầu / lưu tin / báo cáo) |
| 4.5 | Trang `CreatePostPage` / `EditPostPage` (form đổi trường theo loại tin, upload nhiều ảnh, chọn tiện ích; tùy chọn chọn tọa độ bằng Leaflet + OpenStreetMap) |
| 4.6 | Trang `MyPostsPage` (tab theo trạng thái, gia hạn / đóng / xóa), `SavedPostsPage`, `ProfilePage` (hồ sơ, thông tin sinh hoạt, đổi mật khẩu) |
| 4.7 | Trang `ConnectionsPage` (yêu cầu đến/đi, chấp nhận/từ chối/rút lại), `ChatPage` (danh sách hội thoại + cửa sổ chat realtime, tự kết nối lại, đóng kết nối khi rời trang) |
| 4.8 | Form đánh giá (chỉ hiện khi đủ điều kiện), form báo cáo (lý do lọc theo `AppliesTo`), chuông thông báo realtime |
| 4.9 | Trang phía Moderator: `PendingPostsPage` (duyệt/từ chối kèm lý do), `ReportsPage` (xử lý 3 mức), `ViolationHistoryPage` |
| 4.10 | Trang phía Admin: `DashboardPage`, `UsersPage`, `CatalogPage` (khu vực, địa điểm mốc, tiện ích, lý do báo cáo), `ConfigsPage` |
| 4.11 | Xử lý lỗi & loading dùng chung (hiển thị `message` tiếng Việt từ API, skeleton khi tải), định dạng tiền `3.500.000 đ`, giờ GMT+7, giao diện dùng được ở màn hình 360 px |

**Điều kiện hoàn thành:** chạy được trên Chrome, Edge, Firefox (máy tính và chế độ điện thoại của DevTools); đi hết luồng "người dùng đăng ký → đăng tin → moderator duyệt → người khác tìm thấy → gửi yêu cầu → chủ tin chấp nhận → chat → đánh giá" và luồng "người dùng báo cáo → moderator xử lý → admin xem thống kê".

---

## GIAI ĐOẠN 5 — Kiểm thử tích hợp (3 ngày)

| # | Việc cần làm |
|---|---|
| 5.1 | Unit test cho Service nghiệp vụ (fake repository viết tay + `IDateTimeProvider` giả) — tối thiểu 1 test luồng chính + 1 test luồng thay thế / usecase; bắt buộc có test cho BR-02, BR-05, BR-07, BR-08 và hàm Haversine (2 tọa độ biết trước khoảng cách) |
| 5.2 | Integration test trên `RoommateFinderDB_Test`: gửi 5 yêu cầu chấp nhận song song vào tin còn 2 chỗ, kỳ vọng đúng 2 thành công, 3 nhận 409, `NeededOccupants = 0`, tin `closed` (NFR-08); gửi trùng yêu cầu kết nối trả 409 |
| 5.3 | Test API bằng Postman collection (export vào `docs/`) — phủ đủ usecase, có test phân quyền (token người khác → 403, user gọi API admin → 403) |
| 5.4 | UAT thủ công theo kịch bản trong bảng đặc tả Chương 3.4 và UC bổ sung (checklist tick từng luồng thay thế) + kiểm tra 12 yêu cầu phi chức năng NFR-01…NFR-12 |
| 5.5 | Ghi nhận lỗi vào GitHub Issues, phân loại mức độ (blocker/major/minor) |

**Điều kiện hoàn thành:** không còn issue mức "blocker"; test 5.2 pass; checklist UAT đạt ≥ 90%.

---

## GIAI ĐOẠN 6 — Hoàn thiện & đóng gói (2–3 ngày)

| # | Việc cần làm |
|---|---|
| 6.1 | Rà soát bảo mật cơ bản: JWT hết hạn đúng cấu hình; không endpoint ghi dữ liệu nào thiếu `[Authorize]`; không response nào lộ `PasswordHash`; `Phone` chỉ hiện đúng điều kiện; upload từ chối file giả mạo đuôi; xác nhận lỗi 2601/2627 map đúng `409 Conflict` — xem quyết định (d) |
| 6.2 | Seed bộ dữ liệu demo đầy đủ (~50 tin cả 2 loại, đủ các trạng thái, vài cuộc trò chuyện, đánh giá, báo cáo) phục vụ bảo vệ |
| 6.3 | Viết README hướng dẫn chạy từ máy sạch: cài SDK/Node, user-secrets, `dotnet ef database update`, chạy Api và frontend, tài khoản demo từng vai trò |
| 6.4 | Chụp ảnh màn hình / quay video demo phục vụ báo cáo Chương 4 (Kết quả thực nghiệm) |
| 6.5 | Cập nhật báo cáo: Chương 3 theo tài liệu bổ sung (18 bảng, UC mới, sơ đồ trạng thái), Chương 4 với kết quả cài đặt và số liệu kiểm thử thực tế |

---

## TÓM TẮT MỐC THỜI GIAN (tổng ~31–38 ngày làm việc, 1 dev)

| Giai đoạn | Nội dung | Thời gian |
|---|---|---|
| 0 | Khởi tạo dự án | 1 ngày |
| 1 | Domain & Application | 2–3 ngày |
| 2 | Infrastructure & khung API | 3–4 ngày |
| 3 | Nghiệp vụ backend (19 hạng mục) | 10–12 ngày |
| 4 | Frontend Blazor WebAssembly | 10–12 ngày (song song GĐ3) |
| 5 | Kiểm thử tích hợp | 3 ngày |
| 6 | Hoàn thiện & đóng gói | 2–3 ngày |

*1 dev làm tuần tự cả GĐ3 và GĐ4 mất khoảng 31–38 ngày làm việc (~7–8 tuần). Nếu có 2 dev làm song song (1 backend, 1 frontend từ Giai đoạn 2), tổng thời gian có thể rút xuống còn khoảng 21–25 ngày. Kế hoạch IT bản trước dùng 6 sprint × 2 tuần + 1 tuần (~13 tuần) — dư khoảng 5 tuần dự phòng cho sửa lỗi và viết báo cáo.*
