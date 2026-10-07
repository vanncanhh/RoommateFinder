// backend/src/RoommateFinder.Application/Dtos/AdminDtos.cs
import type { Role, UserStatus } from './enums'

export interface AdminUserQuery {
  keyword?: string
  role?: Role
  status?: UserStatus
  page?: number
  pageSize?: number
}

export interface AdminUserDto {
  userId: number
  fullName: string
  email: string
  phone: string | null
  role: Role
  status: UserStatus
  suspendedUntil: string | null
  avgRating: number
  reviewCount: number
  postCount: number
  violationCount: number
  createdAt: string
}

export interface UpdateRoleRequest {
  role: Role
}

export interface UpdateUserStatusRequest {
  status: UserStatus
  suspendDays?: number | null
  reason?: string | null
}

export interface SystemConfigDto {
  configKey: string
  configValue: string
  description: string | null
  min: number | null
  max: number | null
}

export interface UpdateConfigRequest {
  value: string
}

export interface AreaCountDto {
  areaName: string
  postCount: number
}

export interface DailyCountDto {
  /** DateOnly `yyyy-MM-dd` */
  date: string
  count: number
}

export interface DashboardDto {
  /** DateOnly `yyyy-MM-dd` */
  from: string
  /** DateOnly `yyyy-MM-dd` */
  to: string
  newUsers: number
  newPostsHasRoom: number
  newPostsSeeking: number
  approvedCount: number
  rejectedCount: number
  approvalRate: number | null
  avgModerationHours: number | null
  acceptedConnections: number
  pendingReports: number
  pendingPosts: number
  activePosts: number
  totalUsers: number
  topAreas: AreaCountDto[]
  dailyPosts: DailyCountDto[]
  dailyUsers: DailyCountDto[]
}

export interface DashboardQuery {
  /** yyyy-MM-dd */
  from?: string
  /** yyyy-MM-dd */
  to?: string
}
