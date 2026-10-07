// backend/src/RoommateFinder.Application/Dtos/AuthDtos.cs
import type { Role } from './enums'

export interface RegisterRequest {
  fullName: string
  email: string
  password: string
  phone?: string | null
}

export interface LoginRequest {
  email: string
  password: string
}

export interface ForgotPasswordRequest {
  email: string
}

export interface ResetPasswordRequest {
  token: string
  newPassword: string
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
}

export interface CurrentUserDto {
  userId: number
  fullName: string
  email: string
  role: Role
  avatarUrl: string | null
}

export interface AuthResponse {
  accessToken: string
  expiresAt: string
  user: CurrentUserDto
}
