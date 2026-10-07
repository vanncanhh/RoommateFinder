# KẾ HOẠCH TRIỂN KHAI DỰ ÁN
## Hệ thống quản lý đặt lịch spa và làm đẹp
**Stack:** C# · .NET MAUI Blazor Hybrid (frontend) · ASP.NET Core Web API (backend) · Mock in-memory → SQLite → SQL Server (data)

> Nguyên tắc xuyên suốt: Controller/Service **chỉ phụ thuộc interface** (`IBookingRepository`, `IUserRepository`...). Không được `new` trực tiếp lớp Mock hay `DbContext` ở tầng nghiệp vụ. Nhờ vậy Giai đoạn 6 (đổi sang SQL Server) chỉ sửa file đăng ký DI, không sửa code nghiệp vụ.

---

## GIAI ĐOẠN 0 — Khởi tạo dự án (0.5–1 ngày)

| # | Việc cần làm | Deliverable |
|---|---|---|
| 0.1 | Tạo solution 5 project: `Domain`, `Application`, `Infrastructure`, `Api`, `App` (MAUI Blazor Hybrid) | Solution build thành công, project rỗng chạy được |
| 0.2 | Cài đặt NuGet cơ bản: `Swashbuckle` (Swagger), `System.IdentityModel.Tokens.Jwt` (JWT) | `dotnet restore` không lỗi |
| 0.3 | Thiết lập Git repo, `.gitignore`, quy ước nhánh (`main`/`dev`/`feature/*`) | Repo có README, branch protection |
| 0.4 | Thống nhất coding convention (PascalCase entity, tên file theo usecase) | Tài liệu convention ngắn (1 trang) |

**Điều kiện hoàn thành:** cả 5 project reference đúng chiều (`Api` → `Application` → `Domain`; `Infrastructure` → `Application`+`Domain`; `App` → `Application` DTO only), chạy `dotnet build` toàn solution pass.

---

## GIAI ĐOẠN 1 — Domain & Application layer (2–3 ngày)

Dựng "bộ khung" dữ liệu và hợp đồng, chưa cần chạy được, chỉ cần biên dịch đúng.

| # | Việc cần làm | Căn cứ trong báo cáo |
|---|---|---|
| 1.1 | Viết 14 entity trong `Domain/Entities` (User, Spa, SpaBranch, ServiceCategory, Service, Staff, StaffSchedule, Booking, BookingDetail, Payment, Review, Promotion, Notification, Favorite) | Mục 3.7.1 — danh sách lớp; **cột dữ liệu đầy đủ lấy từ script T-SQL Mục 3.8.5** (Mục 3.8.3 chỉ đặc tả chi tiết 4 bảng trọng tâm làm ví dụ — Users, Spas, Bookings, Payments — không phải toàn bộ 14 bảng) |
| 1.2 | Viết DTO cho từng usecase (`CreateBookingDto`, `LoginDto`, `RegisterDto`, `SearchServiceDto`, `UpdateBookingStatusDto`, `CreatePaymentDto`, `CreateReviewDto`, `RevenueReportDto`...) | Mục 3.4 — 8 bảng đặc tả usecase |
| 1.3 | Viết interface Repository cho từng nhóm entity (`IUserRepository`, `ISpaRepository`, `IServiceRepository`, `IBookingRepository`, `IStaffScheduleRepository`, `IPaymentRepository`, `IReviewRepository`) | — |
| 1.4 | Viết interface hạ tầng phụ (`IPasswordHasher`, `IJwtTokenGenerator`, `INotificationSender`) | Mục 2.4, 2.6 |
| 1.5 | Viết class Service nghiệp vụ rỗng (constructor inject interface, method throw `NotImplementedException`) cho 8 usecase | Mục 3.5, 3.6 — biểu đồ hoạt động/tuần tự |

**Điều kiện hoàn thành:** build pass; review code — không entity/DTO nào tham chiếu tới EF Core hay lớp Mock.

---

## ⚠️ CHỐT QUYẾT ĐỊNH THIẾT KẾ TRƯỚC KHI SANG GIAI ĐOẠN 2

Các điểm dưới đây đã được đối chiếu giữa báo cáo và kế hoạch, cần thống nhất **trước khi dev bắt đầu viết Repository/Service**, tránh phải sửa lại giữa chừng.

**a) Ràng buộc 1–1 giữa Bookings↔Payments và Bookings↔Reviews**
Script T-SQL gốc thiếu `UNIQUE` trên `Payments.BookingId` và `Reviews.BookingId` — đã bổ sung `CONSTRAINT UQ_Payment_Booking UNIQUE (BookingId)` và `CONSTRAINT UQ_Review_Booking UNIQUE (BookingId)` (xem báo cáo Mục 3.8.5 bản cập nhật). Khi viết `EfPaymentRepository`/`EfReviewRepository` ở Giai đoạn 6, **không tự check-then-insert bằng LINQ rồi bỏ qua constraint** — phải để DB bắt lỗi trùng khóa và map thành `409 Conflict`, tương tự cách xử lý ở điểm (d) bên dưới.

**b) Cách xác định "khung giờ trống" khi Booking không có EndTime**
Bảng `Bookings` chỉ có `BookingDate` + `BookingTime`, không có thời lượng. **Quyết định:** `StaffSchedule` là nguồn sự thật duy nhất về khung giờ (mỗi dòng = 1 slot cố định, ví dụ 30 phút/slot). Khi tạo Booking:
1. Tính tổng thời lượng = `SUM(BookingDetails → Services.DurationMinute)`.
2. Tính số slot cần = `CEILING(tổng thời lượng / độ dài 1 slot)`.
3. Kiểm tra và khóa **đủ số slot liên tiếp** của `StaffId` bắt đầu từ `BookingTime` (không chỉ 1 slot).
→ `IsSlotAvailableAsync` phải nhận thêm tham số `slotsNeeded`, không chỉ `(staffId, date, time)` như bản phác thảo ban đầu — cập nhật lại interface `IStaffScheduleRepository` ở Giai đoạn 1.3 cho khớp.

**c) Promotions / Favorites / Notifications — 3 bảng có trong CSDL nhưng chưa có usecase**
Ba bảng này nằm trong 14 bảng CSDL (Mục 3.8.1) nhưng **không thuộc 8 usecase chính** đã phân tích, và `Bookings.TotalAmount` hiện chưa có chỗ trừ mã giảm giá. **Quyết định phạm vi:** đưa vào **Giai đoạn 3.9 (mở rộng — không bắt buộc cho bản demo đầu tiên)**:
   - Nếu làm: `CreateBookingDto` cần thêm field `PromotionCode` (nullable); `BookingService` tính `TotalAmount = tổng tiền dịch vụ − giảm giá (nếu mã hợp lệ và còn hạn)`.
   - Nếu không kịp làm: bỏ qua Promotions/Favorites, chỉ giữ `Notifications` ở mức tối thiểu (gọi `INotificationSender` dạng log ra console, chưa cần bảng thật).
   - Việc bỏ qua phần này **không ảnh hưởng** 8 usecase chính đã cam kết trong báo cáo.

**d) Bookings không có SpaId — Reviews lại bắt buộc có SpaId**
Khi viết `ReviewService.CreateReviewAsync`, không lấy `SpaId` trực tiếp từ Booking. Phải suy ra qua: `Booking.BranchId → SpaBranches.SpaId`. Thêm bước `JOIN` hoặc gọi `ISpaBranchRepository.GetSpaIdAsync(branchId)` trước khi tạo `Review`.

**e) Chống trùng lịch: dựa vào ràng buộc DB, không chỉ check-then-insert**
`UQ_Staff_Slot UNIQUE (StaffId, WorkDate, StartTime)` trong `StaffSchedule` là hàng phòng thủ cuối cùng chống race condition khi 2 khách đặt cùng lúc. Ở Giai đoạn 3.4 và 7.1, `BookingService.CreateBookingAsync` phải:
1. Check khả dụng trước (để trả lỗi sớm, thân thiện).
2. **Bắt buộc** bọc thao tác `UPDATE StaffSchedule SET Status='booked'` trong `try/catch` bắt lỗi vi phạm UNIQUE (SQL Server: `SqlException.Number == 2627`), map thành `409 Conflict` — không tin tưởng hoàn toàn bước check ở (1) vì giữa lúc check và lúc ghi vẫn có thể bị race condition.

---



| # | Việc cần làm |
|---|---|
| 2.1 | Viết `MockDataStore` (static list) với dữ liệu mẫu: ≥2 spa, ≥2 chi nhánh, ≥3 dịch vụ, ≥2 kỹ thuật viên, ≥5 khung giờ, ≥2 user mỗi vai trò |
| 2.2 | Viết Mock Repository cho từng interface ở Giai đoạn 1 (thao tác trực tiếp trên `MockDataStore`) |
| 2.3 | Viết `MockPasswordHasher` (băm đơn giản/plain so sánh — chỉ để chạy demo) và `MockJwtTokenGenerator` |
| 2.4 | Tạo `Api/Program.cs`: đăng ký toàn bộ Mock repo qua DI (`AddSingleton<Interface, Mock...>`), cấu hình Swagger, CORS (cho phép App gọi tới) |
| 2.5 | Tạo Controller rỗng cho từng nhóm: `AuthController`, `ServicesController`, `BookingsController`, `PaymentsController`, `ReviewsController`, `ReportsController` — mỗi action gọi Service tương ứng |

**Điều kiện hoàn thành:** chạy `dotnet run` project Api, mở Swagger UI thấy đủ endpoint, gọi thử `GET /api/services` trả về danh sách mock (chưa cần logic phức tạp).

---

## GIAI ĐOẠN 3 — Cài đặt nghiệp vụ backend theo 8 usecase (5–7 ngày)

Làm lần lượt theo đúng thứ tự phụ thuộc (đăng nhập/đăng ký trước, vì các usecase sau cần JWT).

| # | Usecase | Việc cần làm | Tham chiếu |
|---|---|---|---|
| 3.1 | Đăng nhập | `AuthService.LoginAsync`: kiểm tra Users, sinh JWT theo `Role` | Hình 3-2, 3-10, 3-18 |
| 3.2 | Đăng ký | `AuthService.RegisterAsync`: kiểm tra trùng email/SĐT, lưu user mới | Hình 3-3, 3-11, 3-19 |
| 3.3 | Tìm kiếm dịch vụ | `ServiceService.SearchAsync`: lọc theo từ khóa/danh mục/khu vực | Hình 3-4, 3-12, 3-20 |
| 3.4 | Đặt lịch hẹn | `BookingService.CreateBookingAsync`: tính số slot cần từ tổng `DurationMinute`, kiểm tra `IStaffScheduleRepository.IsSlotAvailableAsync(staffId, date, time, slotsNeeded)` — xem quyết định (b), tạo Booking + BookingDetail, khóa đủ số slot | Hình 3-5, 3-13, 3-21 |
| 3.5 | Xác nhận/Từ chối lịch | `BookingService.UpdateStatusAsync`: đổi trạng thái, mở lại khung giờ nếu từ chối, gọi `INotificationSender` | Hình 3-6, 3-14, 3-22 |
| 3.6 | Thanh toán online | `PaymentService.CreatePaymentAsync` + endpoint callback giả lập (mock luôn trả "thành công" sau 2 giây) | Hình 3-7, 3-15, 3-23 |
| 3.7 | Đánh giá dịch vụ | `ReviewService.CreateReviewAsync`: chỉ cho phép khi Booking `completed` và **chưa có Review nào cho Booking đó** (dựa vào `UQ_Review_Booking`); suy ra `SpaId` qua `Booking.BranchId → SpaBranches.SpaId` — xem quyết định (d); cập nhật `RatingAvg` | Hình 3-8, 3-16, 3-24 |
| 3.8 | Thống kê doanh thu | `ReportService.GetRevenueAsync`: tổng hợp theo kỳ từ Bookings/Payments trong Mock | Hình 3-9, 3-17, 3-25 |
| 3.9 *(mở rộng, không bắt buộc)* | Khuyến mãi / Yêu thích / Thông báo | `PromotionService`, `FavoriteService` cơ bản; xem quyết định (c) về phạm vi và cách áp mã giảm giá vào `TotalAmount` | — |

**Điều kiện hoàn thành mỗi usecase:** có thể test bằng Swagger/Postman đúng luồng cơ bản **và** luồng thay thế nêu trong bảng đặc tả (ví dụ: đặt trùng khung giờ phải trả lỗi 409, không phải lỗi 500).

**Gợi ý phân công:** nếu có 2 dev, 1 người làm 3.1–3.4 (auth + booking), 1 người làm 3.5–3.8 (spa-owner side + payment + report) — ít phụ thuộc chéo.

---

## GIAI ĐOẠN 4 — Frontend .NET MAUI Blazor Hybrid (5–7 ngày, chạy song song Giai đoạn 3)

| # | Việc cần làm |
|---|---|
| 4.1 | Thiết lập `HttpClient` (BaseAddress theo platform), `AuthStateProvider` lưu JWT (SecureStorage) |
| 4.2 | Trang `Login.razor`, `Register.razor` |
| 4.3 | Trang `Home.razor` (danh sách spa nổi bật), `ServiceSearch.razor` |
| 4.4 | Trang `SpaDetail.razor` (chi tiết spa/dịch vụ + đánh giá) |
| 4.5 | Trang `Booking.razor` (chọn dịch vụ → chọn khung giờ → xác nhận) |
| 4.6 | Trang `Payment.razor` (chọn phương thức, mô phỏng redirect) |
| 4.7 | Trang `MyBookings.razor` (lịch sử, hủy/đổi lịch, đánh giá sau hoàn thành) |
| 4.8 | Trang phía Chủ spa: `SpaOwnerDashboard.razor`, `PendingBookings.razor` (xác nhận/từ chối), `RevenueReport.razor` |
| 4.9 | Xử lý lỗi & loading state dùng chung (component `ErrorAlert`, `LoadingSpinner`) |

**Điều kiện hoàn thành:** chạy được trên Android emulator + Windows, đi hết luồng "khách hàng tìm dịch vụ → đặt lịch → thanh toán → xem lịch sử → đánh giá" và luồng "chủ spa xác nhận lịch → xem thống kê" bằng dữ liệu Mock.

---

## GIAI ĐOẠN 5 — Kiểm thử tích hợp (2–3 ngày)

| # | Việc cần làm |
|---|---|
| 5.1 | Unit test cho Service nghiệp vụ (dùng Mock repo có sẵn, không cần mock framework phức tạp) — tối thiểu 1 test luồng chính + 1 test luồng thay thế / usecase |
| 5.2 | Test tích hợp API bằng Postman collection (export kèm dự án) — phủ đủ 8 usecase |
| 5.3 | UAT thủ công theo kịch bản trong bảng đặc tả Chương 3.4 (checklist tick từng luồng thay thế) |
| 5.4 | Ghi nhận lỗi vào issue tracker, phân loại mức độ (blocker/major/minor) |

**Điều kiện hoàn thành:** không còn issue mức "blocker"; checklist UAT đạt ≥ 90%.

---

## GIAI ĐOẠN 6 — Chuyển dữ liệu sang cơ sở dữ liệu thật (2–3 ngày)

> Chỉ thực hiện sau khi Giai đoạn 3–5 đã chạy ổn định trên Mock. Có thể chọn 1 trong 2 hướng:

**Hướng A — SQLite trước (khuyến nghị nếu máy dev chưa cài được SQL Server):**
| # | Việc cần làm |
|---|---|
| 6A.1 | Cài `Microsoft.EntityFrameworkCore.Sqlite` |
| 6A.2 | Viết `SpaBookingDbContext` + cấu hình `OnModelCreating` khớp 14 bảng (đối chiếu Mục 3.8.3, 3.8.5) |
| 6A.3 | Viết `Ef*Repository` implement lại các interface ở Giai đoạn 1 (song song tồn tại với Mock, KHÔNG xóa Mock) |
| 6A.4 | Đổi DI trong `Program.cs` sang `Ef*Repository` + `UseSqlite(...)` |
| 6A.5 | `dotnet ef migrations add InitialCreate` → `dotnet ef database update` |
| 6A.6 | Chạy lại toàn bộ test Giai đoạn 5 trên SQLite |

**Hướng B — Chuyển thẳng SQL Server (khi có máy/server cài được):**
| # | Việc cần làm |
|---|---|
| 6B.1 | Cài `Microsoft.EntityFrameworkCore.SqlServer` |
| 6B.2 | Chạy thẳng script T-SQL đã có sẵn trong báo cáo (Mục 3.8.5) để tạo `SpaBookingDB` |
| 6B.3 | Viết `Ef*Repository` (giống 6A.3) |
| 6B.4 | Đổi DI sang `UseSqlServer(connectionString)` |
| 6B.5 | Scaffold hoặc map thủ công entity đúng tên cột đã có trong script |

**Điều kiện hoàn thành:** ứng dụng chạy đúng hành vi như khi dùng Mock, dữ liệu persist sau khi restart API.

---

## GIAI ĐOẠN 7 — Hoàn thiện & đóng gói (2 ngày)

| # | Việc cần làm |
|---|---|
| 7.1 | Rà soát bảo mật cơ bản: JWT hết hạn; xác nhận `UQ_Staff_Slot` đã bắt lỗi và map đúng `409 Conflict` khi 2 request đặt trùng lịch cùng lúc — xem quyết định (e) |
| 7.2 | Đóng gói ứng dụng MAUI (APK debug tối thiểu để demo) |
| 7.3 | Viết README hướng dẫn chạy: cách khởi động Api, đổi connection string, seed dữ liệu mẫu |
| 7.4 | Chụp ảnh màn hình / quay video demo phục vụ báo cáo Chương 4 (Kết quả thực nghiệm) |
| 7.5 | Cập nhật Chương 4 của báo cáo đồ án với kết quả cài đặt thực tế |

---

## TÓM TẮT MỐC THỜI GIAN (tổng ~20–25 ngày làm việc, 1 dev)

| Giai đoạn | Nội dung | Thời gian |
|---|---|---|
| 0 | Khởi tạo dự án | 0.5–1 ngày |
| 1 | Domain & Application | 2–3 ngày |
| 2 | Mock + khung API | 2 ngày |
| 3 | Nghiệp vụ backend (8 usecase) | 5–7 ngày |
| 4 | Frontend Blazor Hybrid | 5–7 ngày (song song GĐ3) |
| 5 | Kiểm thử tích hợp | 2–3 ngày |
| 6 | Chuyển sang DB thật | 2–3 ngày |
| 7 | Hoàn thiện & đóng gói | 2 ngày |

*Nếu có 2 dev làm song song (1 backend, 1 frontend từ Giai đoạn 2), tổng thời gian có thể rút xuống còn khoảng 15–18 ngày.*
