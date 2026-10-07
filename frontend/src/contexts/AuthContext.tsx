import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { authApi } from '../api/auth'
import { setAuthFailureHandler } from '../api/client'
import { queryKeys } from '../api/queryKeys'
import type { AuthResponse, Role } from '../types'
import { feedback } from '../utils/feedback'
import { tokenStorage } from '../utils/tokenStorage'
import { AuthContext, type AuthContextValue } from './authContextInstance'

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const location = useLocation()
  const [token, setToken] = useState<string | null>(() => tokenStorage.get())

  const meQuery = useQuery({
    queryKey: queryKeys.me,
    queryFn: authApi.me,
    enabled: token !== null,
    staleTime: 5 * 60_000,
    retry: false,
  })

  const login = useCallback(
    (response: AuthResponse) => {
      tokenStorage.set(response.accessToken)
      queryClient.setQueryData(queryKeys.me, response.user)
      setToken(response.accessToken)
    },
    [queryClient],
  )

  const logout = useCallback(() => {
    tokenStorage.clear()
    setToken(null)
    // Xóa toàn bộ dữ liệu của phiên cũ (thông báo, tin nhắn...).
    queryClient.clear()
  }, [queryClient])

  const refreshUser = useCallback(async () => {
    await queryClient.invalidateQueries({ queryKey: queryKeys.me })
  }, [queryClient])

  // Đường dẫn hiện tại, dùng làm returnUrl khi phiên hết hạn.
  const currentPath = `${location.pathname}${location.search}`

  useEffect(() => {
    setAuthFailureHandler((reason, msg) => {
      logout()
      if (reason === 'locked') {
        feedback.notification.error({ message: 'Tài khoản bị khóa', description: msg, duration: 8 })
        navigate('/login', { replace: true })
      } else {
        feedback.message.warning(msg)
        const isAuthPage = currentPath.startsWith('/login')
        navigate(isAuthPage ? '/login' : `/login?returnUrl=${encodeURIComponent(currentPath)}`, { replace: true })
      }
    })
    return () => setAuthFailureHandler(null)
  }, [logout, navigate, currentPath])

  const user = token !== null ? (meQuery.data ?? null) : null
  const role = user?.role

  const value = useMemo<AuthContextValue>(() => {
    const hasRole = (...roles: Role[]) => role !== undefined && roles.includes(role)
    return {
      user,
      token,
      isInitializing: token !== null && meQuery.isPending,
      isAuthenticated: user !== null,
      hasRole,
      isModerator: hasRole('moderator', 'admin'),
      isAdmin: hasRole('admin'),
      login,
      logout,
      refreshUser,
    }
  }, [user, token, role, meQuery.isPending, login, logout, refreshUser])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
