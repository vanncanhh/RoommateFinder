// backend/src/RoommateFinder.Application/Common/Models.cs

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface PageQuery {
  page?: number
  pageSize?: number
}

/** Body lỗi nghiệp vụ `{ code, message }` (docs/API.md). */
export interface ApiErrorBody {
  code: string
  message: string
}

export interface MessageResponse {
  message: string
}

export interface CountResponse {
  count: number
}
