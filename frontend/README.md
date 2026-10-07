# RoommateFinder – Frontend

Giao diện web (React 18 + TypeScript + Vite) cho hệ thống **RoommateFinder – Tìm người ở ghép**.

## Công nghệ

- React 18, TypeScript (strict), Vite
- Ant Design 5 (`ConfigProvider locale={viVN}`), `@ant-design/icons`
- React Router 6, TanStack Query 5 (server state), Axios
- `@microsoft/signalr` (chat & thông báo realtime), dayjs (múi giờ GMT+7)
- Lint: oxlint

## Chạy dự án

Yêu cầu: Node.js ≥ 22.12, npm ≥ 10. **Backend phải chạy ở `http://localhost:5080`** (xem `backend/`).

```bash
cd frontend
npm install
npm run dev        # http://localhost:5173
```

Vite proxy `/api`, `/uploads` và `/hubs` (WebSocket) sang `http://localhost:5080`, nên khi dev không cần CORS.
`.env.development` đặt `VITE_API_URL=` (rỗng → gọi đường dẫn tương đối qua proxy). Khi triển khai tách domain,
đặt `VITE_API_URL=https://api.example.com`.

Các lệnh khác:

```bash
npm run build      # kiểm tra kiểu (tsc -b) + build ra dist/
npm run lint       # oxlint
npm run preview    # xem bản build
```

Tài khoản demo (môi trường Development): `admin@roommate.test / Admin@123`,
`mod1@roommate.test / Mod@12345`, `user1@roommate.test … user6@roommate.test / User@1234`.

## Cấu trúc thư mục

```
src/
├── api/                 # Lớp gọi API — component KHÔNG gọi axios trực tiếp
│   ├── client.ts        # Axios instance: baseURL, gắn Bearer token, xử lý 401 / ACCOUNT_LOCKED
│   ├── queryClient.ts   # TanStack QueryClient + thông báo lỗi chung cho mutation
│   ├── queryKeys.ts     # Khóa query tập trung (invalidate nhất quán)
│   └── auth, profile, users, catalog, posts, moderation, connections,
│       conversations, reviews, reports, notifications, admin .ts
├── types/               # Interface TypeScript dịch từ DTO C# (backend/.../Dtos/*.cs) + enum
├── contexts/            # AuthContext (token trong localStorage, user từ /api/auth/me)
├── hooks/               # useAuth, useSignalR, useCatalog, useDocumentTitle
├── routes/              # AppRoutes (bảng route, lazy load), ProtectedRoute (kiểm tra vai trò)
├── components/
│   ├── layout/          # MainLayout, AppHeader (menu + Drawer trên điện thoại), ConsoleLayout
│   └── shared/          # PostCard, PostGrid, EnumTag, UserAvatar, RatingBadge, QueryState...
├── features/<khu vực>/{pages,components}
│   ├── auth             # Đăng nhập, đăng ký, quên / đặt lại mật khẩu
│   ├── posts            # Trang chủ, tìm kiếm, chi tiết, đăng/sửa tin, tin của tôi, tin đã lưu
│   ├── profile          # Hồ sơ cá nhân, hồ sơ công khai
│   ├── connections      # Yêu cầu kết nối đến / đã gửi
│   ├── chat             # Hội thoại + chat realtime
│   ├── reviews, reports # Modal đánh giá, modal báo cáo
│   ├── notifications    # Chuông thông báo realtime
│   ├── moderation       # Duyệt tin, xử lý báo cáo, lịch sử vi phạm
│   └── admin            # Thống kê, người dùng, danh mục, cấu hình
└── utils/               # format tiền (3.500.000 đ), datetime GMT+7, nhãn tiếng Việt, kiểm tra form
```

## Bảng route

| Đường dẫn | Quyền |
|---|---|
| `/`, `/search`, `/posts/:id`, `/users/:id` | Công khai |
| `/login`, `/register`, `/forgot-password`, `/reset-password?token=` | Công khai |
| `/posts/new`, `/posts/:id/edit`, `/my-posts`, `/saved`, `/profile`, `/connections`, `/chat`, `/chat/:conversationId` | Đã đăng nhập |
| `/moderator/posts`, `/moderator/reports`, `/moderator/violations` | Moderator hoặc Admin |
| `/admin/dashboard`, `/admin/users`, `/admin/catalog`, `/admin/configs` | Admin |

## Quy ước

- Thời gian từ API là UTC (ISO, hậu tố `Z`) → hiển thị theo `Asia/Ho_Chi_Minh` (`utils/datetime.ts`).
- Lỗi API `{ code, message }`: hiển thị `message` tiếng Việt; 401 → xóa token, chuyển `/login?returnUrl=...`;
  403 `ACCOUNT_LOCKED` → đăng xuất kèm thông báo.
- Bộ lọc tìm kiếm lưu trên URL query string (chia sẻ / back-forward được).
- Chat: SignalR `/hubs/chat` tự kết nối lại; khi mất kết nối tải lại tin nhắn mỗi 10 giây;
  tin nhắn được gộp và loại trùng theo `messageId`; đóng kết nối khi rời trang.
