// backend/src/RoommateFinder.Application/Dtos/PostDtos.cs
import type { AmenityDto } from './catalog'
import type { Gender, Occupation, PostSort, PostStatus, PostType, PreferredGender, RequestStatus } from './enums'

/** Trường form đăng/sửa tin — gửi dưới dạng multipart/form-data (ảnh truyền riêng). */
export interface PostFormData {
  postType: PostType
  title: string
  description?: string | null
  price: number
  areaId: number
  address?: string | null
  latitude?: number | null
  longitude?: number | null
  currentOccupants: number
  neededOccupants: number
  preferredGender?: PreferredGender | null
  amenityIds: number[]
  /** Chỉ dùng khi sửa: Id các ảnh cũ muốn giữ lại. */
  keepImageIds: number[]
}

export interface PostSearchQuery {
  keyword?: string
  postType?: PostType
  areaId?: number
  minPrice?: number
  maxPrice?: number
  /** Giới tính người tìm: lấy tin có preferredGender bằng giá trị này hoặc 'any'. */
  gender?: Gender
  minNeeded?: number
  landmarkId?: number
  radiusKm?: number
  amenityIds?: number[]
  sort?: PostSort
  page?: number
  pageSize?: number
}

export interface PostSummaryDto {
  postId: number
  postType: PostType
  title: string
  price: number
  areaId: number
  areaName: string
  district: string | null
  city: string
  address: string | null
  currentOccupants: number
  neededOccupants: number
  preferredGender: PreferredGender | null
  thumbnailUrl: string | null
  status: PostStatus
  createdAt: string
  expiredAt: string | null
  viewCount: number
  ownerId: number
  ownerName: string
  ownerAvatarUrl: string | null
  ownerRating: number
  /** Tọa độ hiệu lực: của tin, hoặc tâm khu vực nếu tin không có. */
  latitude: number | null
  longitude: number | null
  distanceKm: number | null
  // Chỉ có giá trị ở danh sách "Tin của tôi" / hàng đợi duyệt
  rejectReason: string | null
  extendCount: number
  pendingRequestCount: number
}

export interface PostImageDto {
  imageId: number
  imageUrl: string
  sortOrder: number
}

export interface PostDetailDto {
  postId: number
  postType: PostType
  title: string
  description: string | null
  price: number
  areaId: number
  areaName: string
  district: string | null
  city: string
  address: string | null
  latitude: number | null
  longitude: number | null
  currentOccupants: number
  neededOccupants: number
  preferredGender: PreferredGender | null
  status: PostStatus
  rejectReason: string | null
  extendCount: number
  viewCount: number
  createdAt: string
  updatedAt: string | null
  expiredAt: string | null
  images: PostImageDto[]
  amenities: AmenityDto[]

  ownerId: number
  ownerName: string
  ownerAvatarUrl: string | null
  ownerGender: Gender | null
  ownerOccupation: Occupation | null
  ownerRating: number
  ownerReviewCount: number
  /** Chỉ có giá trị khi người xem là chủ tin, moderator/admin, hoặc đã được chấp nhận kết nối. */
  ownerPhone: string | null

  // Thông tin theo người xem
  isOwner: boolean
  isSaved: boolean
  myRequestId: number | null
  myRequestStatus: RequestStatus | null
  canSendRequest: boolean
}
