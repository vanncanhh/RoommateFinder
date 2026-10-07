import type {
  Gender,
  LandmarkType,
  NotificationType,
  Occupation,
  PostSort,
  PostStatus,
  PostType,
  PreferredGender,
  ReasonAppliesTo,
  ReportDecision,
  ReportStatus,
  RequestStatus,
  Role,
  SleepSchedule,
  UserStatus,
  ViolationAction,
  ViolationLevel,
} from '../types'

/** Nhãn + màu Tag (antd preset color). */
export interface LabelInfo {
  label: string
  color?: string
}

export const ROLE_LABELS: Record<Role, LabelInfo> = {
  user: { label: 'Người dùng', color: 'default' },
  moderator: { label: 'Kiểm duyệt viên', color: 'blue' },
  admin: { label: 'Quản trị viên', color: 'purple' },
}

export const USER_STATUS_LABELS: Record<UserStatus, LabelInfo> = {
  active: { label: 'Hoạt động', color: 'green' },
  suspended: { label: 'Tạm khóa', color: 'orange' },
  banned: { label: 'Cấm vĩnh viễn', color: 'red' },
}

export const POST_TYPE_LABELS: Record<PostType, LabelInfo> = {
  has_room: { label: 'Có phòng, cần người ở ghép', color: 'geekblue' },
  seeking: { label: 'Đang tìm phòng ở ghép', color: 'cyan' },
}

export const POST_TYPE_SHORT_LABELS: Record<PostType, string> = {
  has_room: 'Có phòng',
  seeking: 'Tìm phòng',
}

export const POST_STATUS_LABELS: Record<PostStatus, LabelInfo> = {
  pending: { label: 'Chờ duyệt', color: 'gold' },
  approved: { label: 'Đang hiển thị', color: 'green' },
  rejected: { label: 'Bị từ chối', color: 'red' },
  hidden: { label: 'Bị ẩn', color: 'volcano' },
  expired: { label: 'Hết hạn', color: 'default' },
  closed: { label: 'Đã đóng', color: 'default' },
}

export const REQUEST_STATUS_LABELS: Record<RequestStatus, LabelInfo> = {
  pending: { label: 'Đang chờ', color: 'gold' },
  accepted: { label: 'Đã chấp nhận', color: 'green' },
  rejected: { label: 'Bị từ chối', color: 'red' },
  cancelled: { label: 'Đã rút lại', color: 'default' },
  expired: { label: 'Hết hiệu lực', color: 'default' },
}

export const REPORT_STATUS_LABELS: Record<ReportStatus, LabelInfo> = {
  pending: { label: 'Chờ xử lý', color: 'gold' },
  resolved: { label: 'Đã xử lý', color: 'green' },
  dismissed: { label: 'Bác bỏ', color: 'default' },
}

export const GENDER_LABELS: Record<Gender, string> = {
  male: 'Nam',
  female: 'Nữ',
  other: 'Khác',
}

export const PREFERRED_GENDER_LABELS: Record<PreferredGender, string> = {
  any: 'Không yêu cầu',
  male: 'Nam',
  female: 'Nữ',
}

export const OCCUPATION_LABELS: Record<Occupation, string> = {
  student: 'Sinh viên',
  worker: 'Người đi làm',
}

export const SLEEP_SCHEDULE_LABELS: Record<SleepSchedule, string> = {
  early: 'Ngủ sớm',
  late: 'Ngủ muộn',
  flexible: 'Linh hoạt',
}

export const CLEANLINESS_LABELS: Record<number, string> = {
  1: 'Khá bừa bộn',
  2: 'Hơi bừa',
  3: 'Bình thường',
  4: 'Gọn gàng',
  5: 'Rất ngăn nắp',
}

export const LANDMARK_TYPE_LABELS: Record<LandmarkType, string> = {
  school: 'Trường học',
  company: 'Công ty',
  other: 'Khác',
}

export const APPLIES_TO_LABELS: Record<ReasonAppliesTo, string> = {
  post: 'Tin đăng',
  user: 'Người dùng',
  both: 'Cả hai',
}

export const REPORT_DECISION_LABELS: Record<ReportDecision, string> = {
  dismiss: 'Bác bỏ báo cáo',
  warning: 'Cảnh cáo',
  hide_post: 'Ẩn tin đăng',
  suspend: 'Tạm khóa tài khoản',
  ban: 'Cấm vĩnh viễn',
}

export const SORT_LABELS: Record<PostSort, string> = {
  newest: 'Mới nhất',
  price_asc: 'Giá tăng dần',
  price_desc: 'Giá giảm dần',
  distance: 'Gần địa điểm mốc nhất',
}

export const VIOLATION_LEVEL_LABELS: Record<ViolationLevel, LabelInfo> = {
  light: { label: 'Nhẹ', color: 'gold' },
  medium: { label: 'Trung bình', color: 'orange' },
  severe: { label: 'Nghiêm trọng', color: 'red' },
}

export const VIOLATION_ACTION_LABELS: Record<ViolationAction, string> = {
  warning: 'Cảnh cáo',
  hide_post: 'Ẩn tin đăng',
  suspend_account: 'Tạm khóa tài khoản',
  ban_account: 'Cấm vĩnh viễn',
}

export const NOTIFICATION_TYPE_LABELS: Record<NotificationType, string> = {
  post_submitted: 'Tin chờ duyệt',
  post_approved: 'Tin được duyệt',
  post_rejected: 'Tin bị từ chối',
  post_hidden: 'Tin bị ẩn',
  post_restored: 'Tin được khôi phục',
  post_expired: 'Tin hết hạn',
  request_new: 'Yêu cầu kết nối mới',
  request_accepted: 'Yêu cầu được chấp nhận',
  request_rejected: 'Yêu cầu bị từ chối',
  request_expired: 'Yêu cầu hết hiệu lực',
  request_cancelled: 'Yêu cầu bị rút lại',
  review_new: 'Đánh giá mới',
  report_new: 'Báo cáo mới',
  violation: 'Vi phạm',
  account_status: 'Trạng thái tài khoản',
  role_changed: 'Thay đổi vai trò',
}

export function yesNoLabel(value: boolean | null | undefined): string {
  if (value === null || value === undefined) return 'Chưa cập nhật'
  return value ? 'Có' : 'Không'
}

/**
 * Tra nhãn an toàn: backend trả `string`, nếu xuất hiện giá trị lạ thì hiển thị nguyên văn.
 */
export function labelOf<K extends string>(map: Record<K, string>, value: string | null | undefined, empty = '—'): string {
  if (value === null || value === undefined || value === '') return empty
  return (map as Record<string, string>)[value] ?? value
}

export function labelInfoOf<K extends string>(map: Record<K, LabelInfo>, value: string | null | undefined): LabelInfo {
  if (!value) return { label: '—' }
  return (map as Record<string, LabelInfo>)[value] ?? { label: value }
}

/** Tạo options cho Select/Radio từ danh sách giá trị + bảng nhãn. */
export function toOptions<K extends string>(values: readonly K[], map: Record<K, string | LabelInfo>): { value: K; label: string }[] {
  return values.map((value) => {
    const entry = map[value]
    return { value, label: typeof entry === 'string' ? entry : entry.label }
  })
}
