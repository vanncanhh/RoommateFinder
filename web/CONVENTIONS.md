# Quy ước frontend Blazor (RoommateFinder.Web)

Blazor WebAssembly standalone, .NET 8, **Bootstrap 5.3 thuần + Bootstrap Icons** (không thư viện UI khác, không JS ngoài `wwwroot/js/app.js`). Giao diện, thông báo, nhãn: **tiếng Việt**.

## Phần nền có sẵn — dùng lại, KHÔNG sửa (trừ khi được giao)
- `Services/Api.cs`: các lớp gọi API theo nhóm — `AuthApi`, `ProfileApi`, `CatalogApi`, `PostsApi`, `ModerationApi`, `ConnectionsApi`, `ChatApi`, `FeedbackApi`, `NotificationsApi`, `AdminApi`. Inject trực tiếp (`@inject PostsApi PostsApi`). Không dùng `HttpClient` trong trang.
- DTO dùng chung với backend: `RoommateFinder.Application.Dtos.*`, `PagedResult<T>` (`RoommateFinder.Application.Common`), hằng số `RoommateFinder.Domain.Constants` (`PostStatus`, `PostType`, `RequestStatus`, `ReportStatus`, `UserStatus`, `Roles`, `Gender`, `Occupation`, `SleepSchedule`, `LandmarkType`, `ReasonAppliesTo`, `NotificationType`), `ReportDecision` (trong Dtos). Đã có trong `_Imports.razor`.
- Lỗi: mọi lời gọi API ném `ApiException` (`.Message` tiếng Việt hiển thị trực tiếp, `.Status`, `.Code`). Bắt `ApiException` → hiện bằng `<ErrorAlert Message=...>` (lỗi tải trang) hoặc `Toast.Error(ex)` (lỗi thao tác). 401/khóa tài khoản đã tự xử lý (đăng xuất + chuyển trang) trong `ApiClient`.
- `AuthSession` (`Session.IsAuthenticated`, `UserId`, `User`, `IsModerator`, `IsAdmin`, `RefreshUserAsync()`, `LogoutAsync()`, sự kiện `Changed`).
- `ToastService` (`Toast.Success/Error/Warning/Info`).
- `RealtimeService`: sự kiện `NotificationReceived`, `MessageReceived`, `MessagesRead(conversationId, readerId)`, `ChatConnectionChanged(bool)`; `SendMessageAsync`, `MarkReadAsync`, `IsChatConnected`. **Hủy đăng ký sự kiện trong `Dispose`** (`@implements IDisposable`), cập nhật UI bằng `InvokeAsync(StateHasChanged)`.
- `Fmt`: `Money`, `MoneyShort`, `DateAndTime`, `Date`, `Relative`, `ChatTime`, `ToVn`, `Initial` — API trả giờ UTC, luôn hiển thị qua `Fmt` (GMT+7).
- `Labels`: nhãn tiếng Việt + màu badge cho mọi enum (`Labels.PostStatus(s)`, `Labels.PostStatusColor(s)`, ...).
- `PostRules`: `IsPublic`, `CanEdit`, `CanExtend`, `CanClose`, `PriceLabel`, `MaxImages`, `MaxImageBytes` — dùng để quyết định hiện nút.
- `ApiClient.Url(relative)`: đổi `/uploads/...` thành URL đầy đủ của ảnh (inject `ApiClient Api`).
- Component `Shared/`: `Loading`, `ErrorAlert`, `EmptyState`, `Pager` (Page/TotalPages/PageChanged), `UserAvatar`, `Stars` (Editable cho chọn 1–5), `Badge`, `Modal` (Show/Title/OnClose/Footer/SizeClass), `PostCard` (Post/ShowStatus/Actions), `RedirectToLogin`.
- Layout: trang thường dùng `MainLayout` mặc định; trang kiểm duyệt `@layout ModeratorLayout` + `@attribute [Authorize(Roles = Roles.ModeratorOrAdmin)]`; trang quản trị `@layout AdminLayout` + `@attribute [Authorize(Roles = Roles.Admin)]`; trang cần đăng nhập `@attribute [Authorize]`.

## Quy chuẩn giao diện (bắt buộc)
- **Một font duy nhất: Be Vietnam Pro** (`wwwroot/lib/be-vietnam-pro`, tải sẵn — chạy được khi không có mạng). Không thêm font khác, không đặt `font-family` trong trang. Icon dùng Bootstrap Icons.
- Màu, bo góc, bóng đổ, cỡ chữ của nút/ô nhập/bảng/thẻ định nghĩa **một lần** ở đầu `wwwroot/css/app.css` (biến `--rf-*`, ghi đè biến Bootstrap). Trang chỉ dùng class Bootstrap (`btn-primary`, `text-secondary`, `card`, `badge text-bg-*`...) — **không** viết mã màu hex hay `font-size` cố định trong `.razor` (ngoại lệ: chữ chú thích rất nhỏ như giờ gửi tin).
- Tiêu đề trang: `<h1 class="h4 mb-…">` kèm icon Bootstrap Icons; mô tả phụ dùng `text-secondary small`.
- Màu trạng thái lấy từ `Labels.*Color(...)` để cùng một trạng thái luôn cùng một màu ở mọi trang.
- Nút chính của trang dùng `btn-primary` (mỗi vùng chỉ một nút chính); thao tác phụ `btn-outline-secondary`; phá hủy `btn-outline-danger` + xác nhận bằng `Modal`.

## Quy ước viết trang
- Mẫu tham khảo: `Pages/Auth/Login.razor`.
- Mỗi trang có `<PageTitle>... – RoommateFinder</PageTitle>`.
- Bộ lọc / tab / số trang lưu trên URL bằng `[SupplyParameterFromQuery]` + `Nav.NavigateTo(...)` để F5 và nút Back hoạt động; tải lại dữ liệu trong `OnParametersSetAsync`.
- Trạng thái tải: `Loading` khi đang tải lần đầu, `ErrorAlert` (có OnRetry) khi lỗi, `EmptyState` khi rỗng. Nút đang xử lý thì `disabled` + spinner.
- Thao tác phá hủy (xóa, từ chối, khóa) phải xác nhận bằng `Modal`.
- Form dùng `EditForm` + DataAnnotations hoặc kiểm tra thủ công; ràng buộc khớp backend (xem `docs/API.md`). Backend vẫn là nơi kiểm tra cuối — luôn hiển thị lỗi backend trả về.
- Responsive tới 360px: dùng grid Bootstrap (`row`, `col-12 col-md-6 ...`), bảng bọc `table-responsive`, không đặt width cố định lớn.
- Không `MarkupString` với nội dung người dùng (XSS). Văn bản nhiều dòng dùng CSS `white-space: pre-wrap`.
- Không hiện nút mà backend chắc chắn từ chối (BR-09: kiểm duyệt viên không thao tác trên tin/báo cáo/đánh giá liên quan chính mình; dùng `PostRules` cho nút tin đăng).
