// Giá trị liệt kê — đối chiếu backend/src/RoommateFinder.Domain/Constants/Statuses.cs và docs/API.md

export type Role = 'user' | 'moderator' | 'admin'
export type UserStatus = 'active' | 'suspended' | 'banned'
export type PostType = 'has_room' | 'seeking'
export type PostStatus = 'pending' | 'approved' | 'rejected' | 'hidden' | 'expired' | 'closed'
export type RequestStatus = 'pending' | 'accepted' | 'rejected' | 'cancelled' | 'expired'
export type ReportStatus = 'pending' | 'resolved' | 'dismissed'
export type Gender = 'male' | 'female' | 'other'
export type PreferredGender = 'male' | 'female' | 'any'
export type Occupation = 'student' | 'worker'
export type SleepSchedule = 'early' | 'late' | 'flexible'
export type LandmarkType = 'school' | 'company' | 'other'
export type ReasonAppliesTo = 'post' | 'user' | 'both'
export type ReportDecision = 'dismiss' | 'warning' | 'hide_post' | 'suspend' | 'ban'
export type PostSort = 'newest' | 'price_asc' | 'price_desc' | 'distance'
export type ViolationLevel = 'light' | 'medium' | 'severe'
export type ViolationAction = 'warning' | 'hide_post' | 'suspend_account' | 'ban_account'
export type NotificationType =
  | 'post_submitted'
  | 'post_approved'
  | 'post_rejected'
  | 'post_hidden'
  | 'post_restored'
  | 'post_expired'
  | 'request_new'
  | 'request_accepted'
  | 'request_rejected'
  | 'request_expired'
  | 'request_cancelled'
  | 'review_new'
  | 'report_new'
  | 'violation'
  | 'account_status'
  | 'role_changed'

export const ROLES: Role[] = ['user', 'moderator', 'admin']
export const USER_STATUSES: UserStatus[] = ['active', 'suspended', 'banned']
export const POST_TYPES: PostType[] = ['has_room', 'seeking']
export const POST_STATUSES: PostStatus[] = ['pending', 'approved', 'rejected', 'hidden', 'expired', 'closed']
export const REQUEST_STATUSES: RequestStatus[] = ['pending', 'accepted', 'rejected', 'cancelled', 'expired']
export const REPORT_STATUSES: ReportStatus[] = ['pending', 'resolved', 'dismissed']
export const GENDERS: Gender[] = ['male', 'female', 'other']
export const PREFERRED_GENDERS: PreferredGender[] = ['any', 'male', 'female']
export const OCCUPATIONS: Occupation[] = ['student', 'worker']
export const SLEEP_SCHEDULES: SleepSchedule[] = ['early', 'late', 'flexible']
export const LANDMARK_TYPES: LandmarkType[] = ['school', 'company', 'other']
export const REASON_APPLIES_TO: ReasonAppliesTo[] = ['post', 'user', 'both']
export const REPORT_DECISIONS: ReportDecision[] = ['dismiss', 'warning', 'hide_post', 'suspend', 'ban']
export const POST_SORTS: PostSort[] = ['newest', 'price_asc', 'price_desc', 'distance']
