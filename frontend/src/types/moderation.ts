// backend/src/RoommateFinder.Application/Dtos/ModerationDtos.cs
import type { NotificationType, PostStatus, ReportDecision, ReportStatus, ViolationAction, ViolationLevel } from './enums'

export interface RejectPostRequest {
  reason: string
}

/** Đúng một trong `postId` / `reportedUserId`. */
export interface CreateReportRequest {
  postId?: number | null
  reportedUserId?: number | null
  reasonId: number
  description?: string | null
}

export interface ReportDto {
  reportId: number
  reporterId: number
  reporterName: string
  postId: number | null
  postTitle: string | null
  postStatus: PostStatus | null
  reportedUserId: number | null
  reportedUserName: string | null
  /** Người chịu trách nhiệm: người bị báo cáo, hoặc chủ tin bị báo cáo. */
  targetUserId: number
  targetUserName: string
  reasonId: number
  reasonName: string
  description: string | null
  status: ReportStatus
  handledBy: number | null
  handlerName: string | null
  handledAt: string | null
  handledNote: string | null
  createdAt: string
  pendingReportsOnTarget: number
}

export interface HandleReportRequest {
  decision: ReportDecision
  suspendDays?: number | null
  note?: string | null
}

export interface ViolationDto {
  violationId: number
  userId: number
  userName: string
  reportId: number | null
  postId: number | null
  level: ViolationLevel
  action: ViolationAction
  suspendDays: number | null
  note: string | null
  handledBy: number
  handlerName: string
  createdAt: string
}

export interface NotificationDto {
  notificationId: number
  userId: number
  type: NotificationType
  content: string
  relatedId: number | null
  link: string | null
  isRead: boolean
  createdAt: string
}
