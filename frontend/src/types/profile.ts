// backend/src/RoommateFinder.Application/Dtos/ProfileDtos.cs
import type { Gender, Occupation, Role, SleepSchedule } from './enums'

export interface ProfileDto {
  userId: number
  fullName: string
  email: string
  phone: string | null
  gender: Gender | null
  /** DateOnly `yyyy-MM-dd` */
  dateOfBirth: string | null
  role: Role
  occupation: Occupation | null
  schoolOrCompany: string | null
  landmarkId: number | null
  landmarkName: string | null
  avatarUrl: string | null
  bio: string | null
  sleepSchedule: SleepSchedule | null
  isSmoker: boolean | null
  hasPet: boolean | null
  cleanlinessLevel: number | null
  avgRating: number
  reviewCount: number
  createdAt: string
}

/** Không có role, status, avgRating — chống over-posting. */
export interface UpdateProfileRequest {
  fullName: string
  phone?: string | null
  gender?: Gender | null
  /** DateOnly `yyyy-MM-dd` */
  dateOfBirth?: string | null
  occupation?: Occupation | null
  schoolOrCompany?: string | null
  landmarkId?: number | null
  bio?: string | null
  sleepSchedule?: SleepSchedule | null
  isSmoker?: boolean | null
  hasPet?: boolean | null
  cleanlinessLevel?: number | null
}

export interface PublicProfileDto {
  userId: number
  fullName: string
  avatarUrl: string | null
  gender: Gender | null
  occupation: Occupation | null
  schoolOrCompany: string | null
  bio: string | null
  sleepSchedule: SleepSchedule | null
  isSmoker: boolean | null
  hasPet: boolean | null
  cleanlinessLevel: number | null
  avgRating: number
  reviewCount: number
  createdAt: string
}

export interface ReviewDto {
  reviewId: number
  requestId: number
  reviewerId: number
  reviewerName: string
  reviewerAvatarUrl: string | null
  revieweeId: number
  revieweeName: string
  rating: number
  comment: string | null
  postTitle: string | null
  isHidden: boolean
  createdAt: string
}

export interface CreateReviewRequest {
  requestId: number
  rating: number
  comment?: string | null
}
