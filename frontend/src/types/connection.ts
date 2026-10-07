// backend/src/RoommateFinder.Application/Dtos/ConnectionDtos.cs
import type { PostStatus, PostType, RequestStatus } from './enums'

export interface CreateConnectionRequest {
  postId: number
  message?: string | null
}

export interface ConnectionRequestDto {
  requestId: number
  postId: number
  postTitle: string
  postType: PostType
  postStatus: PostStatus
  postThumbnailUrl: string | null
  senderId: number
  senderName: string
  senderAvatarUrl: string | null
  senderRating: number
  senderReviewCount: number
  receiverId: number
  receiverName: string
  receiverAvatarUrl: string | null
  message: string | null
  status: RequestStatus
  createdAt: string
  respondedAt: string | null
  conversationId: number | null
  // Đánh giá (BR-05) — tính theo người đang xem
  hasReviewed: boolean
  canReview: boolean
  reviewAvailableAt: string | null
}

export interface ConversationDto {
  conversationId: number
  requestId: number
  postId: number
  postTitle: string
  otherUserId: number
  otherUserName: string
  otherUserAvatarUrl: string | null
  lastMessage: string | null
  lastMessageSenderId: number | null
  lastMessageAt: string | null
  createdAt: string
  unreadCount: number
}

export interface MessageDto {
  messageId: number
  conversationId: number
  senderId: number
  content: string
  sentAt: string
  isRead: boolean
}

export interface SendMessageRequest {
  content: string
}

/** Sự kiện SignalR `messagesRead` trên /hubs/chat. */
export interface MessagesReadEvent {
  conversationId: number
  readerId: number
}
