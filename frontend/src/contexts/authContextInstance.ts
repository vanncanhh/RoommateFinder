import { createContext } from 'react'
import type { AuthResponse, CurrentUserDto, Role } from '../types'

export interface AuthContextValue {
  user: CurrentUserDto | null
  token: string | null
  /** Đang kiểm tra token đã lưu (gọi /api/auth/me) — chưa biết đã đăng nhập hay chưa. */
  isInitializing: boolean
  isAuthenticated: boolean
  hasRole: (...roles: Role[]) => boolean
  isModerator: boolean
  isAdmin: boolean
  login: (response: AuthResponse) => void
  logout: () => void
  refreshUser: () => Promise<void>
}

export const AuthContext = createContext<AuthContextValue | null>(null)
