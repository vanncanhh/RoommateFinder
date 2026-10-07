import type { PostStatus } from '../../types'
import { dayjs } from '../../utils/datetime'

/**
 * Số lần gia hạn tối đa — cấu hình hệ thống POST_MAX_EXTEND (mặc định 3).
 * API công khai không trả cấu hình này nên frontend dùng giá trị mặc định; backend vẫn kiểm tra lại.
 */
export const POST_MAX_EXTEND = 3

/** Tin đang hiển thị chỉ được gia hạn khi còn dưới số ngày này (PostService.ExtendWindowDays). */
export const POST_EXTEND_WINDOW_DAYS = 3

interface PostLike {
  status: PostStatus
  expiredAt: string | null
  extendCount: number
}

/** Tin công khai: đã duyệt và chưa quá hạn (PostService.IsPublic). */
export function isPostPublic(post: Pick<PostLike, 'status' | 'expiredAt'>): boolean {
  return post.status === 'approved' && (post.expiredAt === null || dayjs.utc(post.expiredAt).isAfter(dayjs()))
}

/** Gia hạn: tin hết hạn, hoặc tin đang hiển thị còn dưới 3 ngày; chưa vượt số lần gia hạn tối đa. */
export function canExtendPost(post: PostLike): boolean {
  if (post.extendCount >= POST_MAX_EXTEND) return false
  if (post.status === 'expired') return true
  if (post.status !== 'approved') return false
  return post.expiredAt === null || !dayjs.utc(post.expiredAt).isAfter(dayjs().add(POST_EXTEND_WINDOW_DAYS, 'day'))
}

/** Chỉ sửa được tin đang chờ duyệt, đã duyệt hoặc bị từ chối. */
export function canEditPost(post: Pick<PostLike, 'status'>): boolean {
  return (['pending', 'approved', 'rejected'] as PostStatus[]).includes(post.status)
}

/** Đóng tin: chỉ khi đang hiển thị. */
export function canClosePost(post: Pick<PostLike, 'status'>): boolean {
  return post.status === 'approved'
}
