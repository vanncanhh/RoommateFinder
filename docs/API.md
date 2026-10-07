# Hợp đồng API — RoommateFinder

Backend: ASP.NET Core Web API .NET 8, chạy tại `http://localhost:5080` (Swagger: `/swagger`).
Frontend: Blazor WebAssembly (`web/RoommateFinder.Web`) chạy tại `http://localhost:5173`, gọi thẳng backend theo `ApiBaseUrl` trong `wwwroot/appsettings.json`; backend bật CORS cho origin này. URL ảnh trong DTO là tương đối (`/uploads/...`) → client ghép với `ApiBaseUrl`. DTO C# dùng chung (project Application) nên client và server không thể lệch tên trường.

## Quy ước chung

- JSON **camelCase**. Kiểu dữ liệu từng DTO xem trực tiếp trong `backend/src/RoommateFinder.Application/Dtos/*.cs` (tên thuộc tính C# PascalCase → JSON camelCase).
- `DateTime` trả về dạng ISO 8601 UTC có hậu tố `Z` (ví dụ `2026-10-06T03:15:00Z`); frontend hiển thị theo GMT+7. `DateOnly` dạng `yyyy-MM-dd`.
- `long`/`int`/`decimal` là `number` trong JSON.
- Xác thực: header `Authorization: Bearer <accessToken>`. Token hết hạn hoặc sai → **401 không có body** → frontend xóa token, chuyển về trang đăng nhập.
- Lỗi nghiệp vụ trả JSON `{ "code": string, "message": string }`, `message` là tiếng Việt để hiển thị trực tiếp:

| HTTP | code | Khi nào |
|---|---|---|
| 400 | `VALIDATION` | Dữ liệu không hợp lệ |
| 401 | `UNAUTHORIZED` | Sai email/mật khẩu khi đăng nhập |
| 403 | `FORBIDDEN` | Không đủ quyền / không phải chủ sở hữu |
| 403 | `ACCOUNT_LOCKED` | Tài khoản bị khóa (đăng nhập bị chặn hoặc bị khóa giữa phiên → frontend đăng xuất) |
| 404 | `NOT_FOUND` | Không tìm thấy |
| 409 | `CONFLICT` | Trạng thái không cho phép, dữ liệu trùng (gửi trùng yêu cầu, tin đã đủ người...) |
| 423 | `LOGIN_LOCKED` | Khóa tạm do đăng nhập sai 5 lần (message có thời gian còn lại) |
| 429 | `TOO_MANY_REQUESTS` | Vượt giới hạn (số tin/ngày, yêu cầu quên mật khẩu) |
| 500 | `SERVER_ERROR` | Lỗi hệ thống |

- Danh sách phân trang trả `PagedResult<T>`: `{ items: T[], page, pageSize, totalCount, totalPages }`. Query `page` (từ 1), `pageSize` (tối đa 50).

## Giá trị liệt kê

| Trường | Giá trị |
|---|---|
| Role | `user`, `moderator`, `admin` |
| User status | `active`, `suspended`, `banned` |
| PostType | `has_room` (có phòng, cần thêm người), `seeking` (đang tìm phòng ở ghép) |
| Post status | `pending` (chờ duyệt), `approved` (đang hiển thị), `rejected` (bị từ chối), `hidden` (bị ẩn), `expired` (hết hạn), `closed` (đã đóng) |
| Request status | `pending`, `accepted`, `rejected`, `cancelled`, `expired` |
| Report status | `pending`, `resolved`, `dismissed` |
| Gender (user) | `male`, `female`, `other` · PreferredGender (tin): `male`, `female`, `any` |
| Occupation | `student`, `worker` |
| SleepSchedule | `early`, `late`, `flexible` · CleanlinessLevel 1–5 |
| Landmark type | `school`, `company`, `other` |
| ReportReason appliesTo | `post`, `user`, `both` |
| HandleReport decision | `dismiss`, `warning`, `hide_post` (chỉ báo cáo tin), `suspend` (cần `suspendDays`), `ban` |
| Sort tìm kiếm | `newest` (mặc định), `price_asc`, `price_desc`, `distance` (cần landmarkId) |

Notification `type`: `post_submitted`, `post_approved`, `post_rejected`, `post_hidden`, `post_restored`, `post_expired`, `request_new`, `request_accepted`, `request_rejected`, `request_expired`, `request_cancelled`, `review_new`, `report_new`, `violation`, `account_status`, `role_changed`. Mỗi thông báo có `link` (đường dẫn frontend, ví dụ `/posts/12`, `/connections`, `/chat/5`) để mở khi bấm.

## Endpoint

Ký hiệu: 🔓 công khai · 🔑 cần đăng nhập · 🛡 moderator hoặc admin · 👑 admin.

### Auth — `/api/auth`
| Method | Path | Body / Query | Trả về |
|---|---|---|---|
| POST | `/register` 🔓 | `RegisterRequest` | 201 `MessageResponse` |
| POST | `/login` 🔓 | `LoginRequest` | `AuthResponse` |
| POST | `/forgot-password` 🔓 | `ForgotPasswordRequest` | `MessageResponse` (luôn cùng thông điệp) |
| POST | `/reset-password` 🔓 | `ResetPasswordRequest` | `MessageResponse` |
| PUT | `/change-password` 🔑 | `ChangePasswordRequest` | `MessageResponse` → frontend đăng xuất; mọi phiên khác bị thu hồi (security stamp) |
| GET | `/me` 🔑 | — | `CurrentUserDto` |

### Hồ sơ — `/api/profile`, `/api/users`
| Method | Path | Body / Query | Trả về |
|---|---|---|---|
| GET | `/api/profile` 🔑 | — | `ProfileDto` |
| PUT | `/api/profile` 🔑 | `UpdateProfileRequest` | `ProfileDto` |
| POST | `/api/profile/avatar` 🔑 | multipart, field `file` (jpg/png/webp) | `ProfileDto` |
| GET | `/api/users/{id}` 🔓 | — | `PublicProfileDto` |
| GET | `/api/users/{id}/reviews` 🔓 | `page`, `pageSize` | `PagedResult<ReviewDto>` |
| GET | `/api/users/{id}/posts` 🔓 | — | `PostSummaryDto[]` (tin đang hiển thị) |

### Danh mục công khai — `/api/catalog` 🔓
| Method | Path | Query | Trả về |
|---|---|---|---|
| GET | `/areas` | — | `AreaDto[]` (đang hoạt động) |
| GET | `/landmarks` | `areaId?` | `LandmarkDto[]` |
| GET | `/amenities` | — | `AmenityDto[]` |
| GET | `/report-reasons` | `appliesTo?` = `post` \| `user` | `ReportReasonDto[]` |

### Tin đăng — `/api/posts`
| Method | Path | Body / Query | Trả về |
|---|---|---|---|
| GET | `/search` 🔓 | `PostSearchQuery` qua query string; `amenityIds` lặp lại (`amenityIds=1&amenityIds=3`) | `PagedResult<PostSummaryDto>` (`distanceKm` có khi truyền `landmarkId`) |
| GET | `/latest` 🔓 | `count` (mặc định 8) | `PostSummaryDto[]` |
| GET | `/{id}` 🔓 (gửi token nếu có) | — | `PostDetailDto` — tin chưa duyệt chỉ chủ tin/moderator xem được |
| POST | `/` 🔑 | multipart: các trường `PostFormData` (`amenityIds` lặp lại) + nhiều file `images` | 201 `PostDetailDto` (status `pending`) |
| PUT | `/{id}` 🔑 chủ tin | multipart: `PostFormData` + `keepImageIds` lặp lại + file `images` mới | `PostDetailDto` (về `pending`) |
| DELETE | `/{id}` 🔑 chủ tin | — | 204 (xóa mềm) |
| PUT | `/{id}/extend` 🔑 chủ tin | — | `PostDetailDto` |
| PUT | `/{id}/close` 🔑 chủ tin | — | `PostDetailDto` |
| GET | `/mine` 🔑 | `status?` | `PostSummaryDto[]` (có `rejectReason`, `extendCount`, `pendingRequestCount`) |
| GET | `/saved` 🔑 | — | `PostSummaryDto[]` |
| POST | `/{id}/save` 🔑 | — | 204 |
| DELETE | `/{id}/save` 🔑 | — | 204 |

Ràng buộc form tin: `title` 10–150 ký tự; `price` > 0; `neededOccupants` ≥ 1; tin `has_room` cần 1–10 ảnh, `seeking` không bắt buộc ảnh; mỗi ảnh ≤ 5 MB, chỉ jpg/png/webp.

### Kiểm duyệt — `/api/moderation` 🛡
| Method | Path | Body / Query | Trả về |
|---|---|---|---|
| GET | `/posts` | `status` (mặc định `pending`), `page`, `pageSize` | `PagedResult<PostSummaryDto>` |
| POST | `/posts/{id}/approve` | — | `PostDetailDto` |
| POST | `/posts/{id}/reject` | `RejectPostRequest` | `PostDetailDto` |
| GET | `/reports` | `status` (mặc định `pending`), `page`, `pageSize` | `PagedResult<ReportDto>` |
| POST | `/reports/{id}/handle` | `HandleReportRequest` | `ReportDto` |
| GET | `/violations` | `userId?`, `page`, `pageSize` | `PagedResult<ViolationDto>` |
| PUT | `/reviews/{id}/hide` | — | 204 |
| PUT | `/reviews/{id}/unhide` | — | 204 |

### Kết nối — `/api/connections` 🔑
| Method | Path | Body / Query | Trả về |
|---|---|---|---|
| POST | `/` | `CreateConnectionRequest` | 201 `ConnectionRequestDto` |
| GET | `/incoming` | `status?` | `ConnectionRequestDto[]` (yêu cầu gửi tới tin của tôi) |
| GET | `/outgoing` | `status?` | `ConnectionRequestDto[]` (yêu cầu tôi đã gửi) |
| PUT | `/{id}/accept` | — | `ConnectionRequestDto` (409 nếu tin đã đủ người) |
| PUT | `/{id}/reject` | — | `ConnectionRequestDto` |
| PUT | `/{id}/cancel` | — | `ConnectionRequestDto` (người gửi rút lại) |

### Trò chuyện — `/api/conversations` 🔑
| Method | Path | Body / Query | Trả về |
|---|---|---|---|
| GET | `/` | — | `ConversationDto[]` (mới nhất trước) |
| GET | `/{id}/messages` | `before?` (messageId), `limit` (mặc định 30) | `MessageDto[]` theo thứ tự cũ → mới |
| POST | `/{id}/messages` | `SendMessageRequest` | `MessageDto` |
| PUT | `/{id}/read` | — | 204 |

### Đánh giá, báo cáo, thông báo 🔑
| Method | Path | Body / Query | Trả về |
|---|---|---|---|
| POST | `/api/reviews` | `CreateReviewRequest` (rating 1–5) | 201 `ReviewDto` |
| POST | `/api/reports` | `CreateReportRequest` (đúng một trong `postId` / `reportedUserId`) | 201 `MessageResponse` |
| GET | `/api/notifications` | `page`, `pageSize` | `PagedResult<NotificationDto>` |
| GET | `/api/notifications/unread-count` | — | `{ count: number }` |
| PUT | `/api/notifications/{id}/read` | — | 204 |
| PUT | `/api/notifications/read-all` | — | 204 |

### Quản trị — `/api/admin` 👑
| Method | Path | Body / Query | Trả về |
|---|---|---|---|
| GET | `/users` | `AdminUserQuery` | `PagedResult<AdminUserDto>` |
| PUT | `/users/{id}/role` | `UpdateRoleRequest` | `AdminUserDto` |
| PUT | `/users/{id}/status` | `UpdateUserStatusRequest` | `AdminUserDto` |
| GET / POST / PUT | `/areas`, `/areas/{id}` | `AreaUpsertRequest` | `AreaDto[]` / `AreaDto` |
| GET / POST / PUT | `/landmarks`, `/landmarks/{id}` | `LandmarkUpsertRequest` | `LandmarkDto[]` / `LandmarkDto` |
| GET / POST / PUT | `/amenities`, `/amenities/{id}` | `AmenityUpsertRequest` | `AmenityDto[]` / `AmenityDto` |
| GET / POST / PUT | `/report-reasons`, `/report-reasons/{id}` | `ReportReasonUpsertRequest` | `ReportReasonDto[]` / `ReportReasonDto` |
| GET | `/configs` | — | `SystemConfigDto[]` |
| PUT | `/configs/{key}` | `UpdateConfigRequest` | `SystemConfigDto` |
| GET | `/dashboard` | `from`, `to` (yyyy-MM-dd, mặc định 30 ngày gần nhất) | `DashboardDto` |

Danh mục ở `/api/admin/*` trả cả mục đã ẩn (`isActive = false`); không có DELETE — ẩn bằng `isActive`.

### Khác
| Method | Path | Trả về |
|---|---|---|
| GET | `/api/health` 🔓 | `{ status: "ok", time }` |
| GET | `/uploads/...` 🔓 | file ảnh tĩnh (URL ảnh trong DTO là đường dẫn tương đối `/uploads/...`) |

## SignalR

Kết nối bằng `@microsoft/signalr`, token truyền qua `accessTokenFactory` (server đọc `access_token` trên query string).

| Hub | Hướng | Tên | Dữ liệu |
|---|---|---|---|
| `/hubs/notifications` | server → client | `notification` | `NotificationDto` |
| `/hubs/chat` | client → server | `SendMessage(conversationId: number, content: string)` | trả về `MessageDto` |
| `/hubs/chat` | client → server | `MarkRead(conversationId: number)` | — |
| `/hubs/chat` | server → client | `message` | `MessageDto` (gửi tới cả hai thành viên, kể cả người gửi ở tab khác) |
| `/hubs/chat` | server → client | `messagesRead` | `{ conversationId: number, readerId: number }` |

Nếu kết nối SignalR lỗi, frontend tự kết nối lại (`WithAutomaticReconnect`, thư viện `Microsoft.AspNetCore.SignalR.Client`) và trong lúc mất kết nối thì tải lại tin nhắn mỗi 10 giây (phương án dự phòng trong kế hoạch).

## Tài khoản demo (chỉ môi trường Development)

| Vai trò | Email | Mật khẩu |
|---|---|---|
| Admin | `admin@roommate.test` | `Admin@123` |
| Moderator | `mod1@roommate.test`, `mod2@roommate.test` | `Mod@12345` |
| User | `user1@roommate.test` … `user6@roommate.test` | `User@1234` |
