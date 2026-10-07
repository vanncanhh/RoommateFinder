import { Button, Result } from 'antd'
import type { ReactNode } from 'react'
import { Navigate, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { PageFallback } from '../components/layout/MainLayout'
import { useAuth } from '../hooks/useAuth'
import type { Role } from '../types'

interface ProtectedRouteProps {
  /** Vai trò được phép; bỏ trống = mọi người dùng đã đăng nhập. */
  roles?: Role[]
  children?: ReactNode
}

/**
 * Bảo vệ route phía client: chưa đăng nhập → /login?returnUrl=..., sai vai trò → 403.
 * (Backend vẫn kiểm tra quyền ở mọi endpoint; ẩn menu chỉ là UI.)
 */
export function ProtectedRoute({ roles, children }: ProtectedRouteProps) {
  const { user, isInitializing, hasRole } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()

  if (isInitializing) return <PageFallback />

  if (!user) {
    const returnUrl = encodeURIComponent(`${location.pathname}${location.search}`)
    return <Navigate to={`/login?returnUrl=${returnUrl}`} replace />
  }

  if (roles && roles.length > 0 && !hasRole(...roles)) {
    return (
      <Result
        status="403"
        title="Không có quyền truy cập"
        subTitle="Bạn không có quyền truy cập trang này."
        extra={
          <Button type="primary" onClick={() => navigate('/')}>
            Về trang chủ
          </Button>
        }
      />
    )
  }

  return children ? <>{children}</> : <Outlet />
}
