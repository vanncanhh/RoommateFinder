import type { AdminUserQuery, DashboardQuery, PostSearchQuery, PostStatus, ReasonAppliesTo, ReportStatus, RequestStatus } from '../types'

/** Khóa TanStack Query tập trung để invalidate nhất quán. */
export const queryKeys = {
  me: ['auth', 'me'] as const,
  profile: ['profile'] as const,

  users: {
    all: ['users'] as const,
    detail: (id: number) => ['users', id] as const,
    reviews: (id: number, page: number) => ['users', id, 'reviews', page] as const,
    posts: (id: number) => ['users', id, 'posts'] as const,
  },

  catalog: {
    areas: ['catalog', 'areas'] as const,
    landmarks: (areaId?: number) => ['catalog', 'landmarks', areaId ?? 'all'] as const,
    amenities: ['catalog', 'amenities'] as const,
    reportReasons: (appliesTo?: ReasonAppliesTo) => ['catalog', 'report-reasons', appliesTo ?? 'all'] as const,
  },

  posts: {
    all: ['posts'] as const,
    search: (q: PostSearchQuery) => ['posts', 'search', q] as const,
    latest: (count: number) => ['posts', 'latest', count] as const,
    detail: (id: number) => ['posts', 'detail', id] as const,
    mine: (status?: PostStatus) => ['posts', 'mine', status ?? 'all'] as const,
    saved: ['posts', 'saved'] as const,
  },

  moderation: {
    all: ['moderation'] as const,
    posts: (status: PostStatus, page: number, pageSize: number) => ['moderation', 'posts', status, page, pageSize] as const,
    reports: (status: ReportStatus, page: number, pageSize: number) =>
      ['moderation', 'reports', status, page, pageSize] as const,
    violations: (userId: number | undefined, page: number, pageSize: number) =>
      ['moderation', 'violations', userId ?? 'all', page, pageSize] as const,
  },

  connections: {
    all: ['connections'] as const,
    incoming: (status?: RequestStatus) => ['connections', 'incoming', status ?? 'all'] as const,
    outgoing: (status?: RequestStatus) => ['connections', 'outgoing', status ?? 'all'] as const,
  },

  conversations: {
    all: ['conversations'] as const,
    list: ['conversations', 'list'] as const,
    latestMessages: (id: number) => ['conversations', id, 'messages', 'latest'] as const,
  },

  notifications: {
    all: ['notifications'] as const,
    list: (page: number, pageSize: number) => ['notifications', 'list', page, pageSize] as const,
    unreadCount: ['notifications', 'unread-count'] as const,
  },

  admin: {
    all: ['admin'] as const,
    users: (q: AdminUserQuery) => ['admin', 'users', q] as const,
    areas: ['admin', 'areas'] as const,
    landmarks: ['admin', 'landmarks'] as const,
    amenities: ['admin', 'amenities'] as const,
    reportReasons: ['admin', 'report-reasons'] as const,
    configs: ['admin', 'configs'] as const,
    dashboard: (q: DashboardQuery) => ['admin', 'dashboard', q] as const,
  },
}
