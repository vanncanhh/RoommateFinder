import {
  AppstoreOutlined,
  AuditOutlined,
  BarChartOutlined,
  FlagOutlined,
  HistoryOutlined,
  SettingOutlined,
  TeamOutlined,
} from '@ant-design/icons'
import { lazy } from 'react'
import { Navigate, Route, Routes } from 'react-router-dom'
import { ConsoleLayout, type ConsoleNavItem } from '../components/layout/ConsoleLayout'
import { MainLayout } from '../components/layout/MainLayout'
import { ProtectedRoute } from './ProtectedRoute'

// Tải trang theo nhu cầu (code splitting) để giảm dung lượng tải lần đầu.
const HomePage = lazy(() => import('../features/posts/pages/HomePage'))
const SearchPage = lazy(() => import('../features/posts/pages/SearchPage'))
const PostDetailPage = lazy(() => import('../features/posts/pages/PostDetailPage'))
const CreatePostPage = lazy(() => import('../features/posts/pages/CreatePostPage'))
const EditPostPage = lazy(() => import('../features/posts/pages/EditPostPage'))
const MyPostsPage = lazy(() => import('../features/posts/pages/MyPostsPage'))
const SavedPostsPage = lazy(() => import('../features/posts/pages/SavedPostsPage'))
const LoginPage = lazy(() => import('../features/auth/pages/LoginPage'))
const RegisterPage = lazy(() => import('../features/auth/pages/RegisterPage'))
const ForgotPasswordPage = lazy(() => import('../features/auth/pages/ForgotPasswordPage'))
const ResetPasswordPage = lazy(() => import('../features/auth/pages/ResetPasswordPage'))
const ProfilePage = lazy(() => import('../features/profile/pages/ProfilePage'))
const PublicProfilePage = lazy(() => import('../features/profile/pages/PublicProfilePage'))
const ConnectionsPage = lazy(() => import('../features/connections/pages/ConnectionsPage'))
const ChatPage = lazy(() => import('../features/chat/pages/ChatPage'))
const PendingPostsPage = lazy(() => import('../features/moderation/pages/PendingPostsPage'))
const ReportsPage = lazy(() => import('../features/moderation/pages/ReportsPage'))
const ViolationsPage = lazy(() => import('../features/moderation/pages/ViolationsPage'))
const DashboardPage = lazy(() => import('../features/admin/pages/DashboardPage'))
const UsersPage = lazy(() => import('../features/admin/pages/UsersPage'))
const CatalogPage = lazy(() => import('../features/admin/pages/CatalogPage'))
const ConfigsPage = lazy(() => import('../features/admin/pages/ConfigsPage'))
const NotFoundPage = lazy(() => import('../components/shared/NotFoundPage'))

const MODERATOR_NAV: ConsoleNavItem[] = [
  { path: '/moderator/posts', label: 'Duyệt tin', icon: <AuditOutlined /> },
  { path: '/moderator/reports', label: 'Báo cáo', icon: <FlagOutlined /> },
  { path: '/moderator/violations', label: 'Lịch sử vi phạm', icon: <HistoryOutlined /> },
]

const ADMIN_NAV: ConsoleNavItem[] = [
  { path: '/admin/dashboard', label: 'Thống kê', icon: <BarChartOutlined /> },
  { path: '/admin/users', label: 'Người dùng', icon: <TeamOutlined /> },
  { path: '/admin/catalog', label: 'Danh mục', icon: <AppstoreOutlined /> },
  { path: '/admin/configs', label: 'Cấu hình', icon: <SettingOutlined /> },
]

/** Bảng route của ứng dụng. */
export function AppRoutes() {
  return (
    <Routes>
      <Route element={<MainLayout />}>
        {/* Công khai */}
        <Route index element={<HomePage />} />
        <Route path="search" element={<SearchPage />} />
        <Route path="posts/:id" element={<PostDetailPage />} />
        <Route path="users/:id" element={<PublicProfilePage />} />
        <Route path="login" element={<LoginPage />} />
        <Route path="register" element={<RegisterPage />} />
        <Route path="forgot-password" element={<ForgotPasswordPage />} />
        <Route path="reset-password" element={<ResetPasswordPage />} />

        {/* Người dùng đã đăng nhập */}
        <Route element={<ProtectedRoute />}>
          <Route path="posts/new" element={<CreatePostPage />} />
          <Route path="posts/:id/edit" element={<EditPostPage />} />
          <Route path="my-posts" element={<MyPostsPage />} />
          <Route path="saved" element={<SavedPostsPage />} />
          <Route path="profile" element={<ProfilePage />} />
          <Route path="connections" element={<ConnectionsPage />} />
          <Route path="chat" element={<ChatPage />} />
          <Route path="chat/:conversationId" element={<ChatPage />} />
        </Route>

        {/* Moderator hoặc Admin */}
        <Route path="moderator" element={<ProtectedRoute roles={['moderator', 'admin']} />}>
          <Route element={<ConsoleLayout items={MODERATOR_NAV} />}>
            <Route index element={<Navigate to="posts" replace />} />
            <Route path="posts" element={<PendingPostsPage />} />
            <Route path="reports" element={<ReportsPage />} />
            <Route path="violations" element={<ViolationsPage />} />
          </Route>
        </Route>

        {/* Chỉ Admin */}
        <Route path="admin" element={<ProtectedRoute roles={['admin']} />}>
          <Route element={<ConsoleLayout items={ADMIN_NAV} />}>
            <Route index element={<Navigate to="dashboard" replace />} />
            <Route path="dashboard" element={<DashboardPage />} />
            <Route path="users" element={<UsersPage />} />
            <Route path="catalog" element={<CatalogPage />} />
            <Route path="configs" element={<ConfigsPage />} />
          </Route>
        </Route>

        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  )
}
