import type {
  HandleReportRequest,
  PagedResult,
  PageQuery,
  PostDetailDto,
  PostStatus,
  PostSummaryDto,
  RejectPostRequest,
  ReportDto,
  ReportStatus,
  ViolationDto,
} from '../types'
import { apiClient } from './client'

const BASE = '/api/moderation'

export const moderationApi = {
  getPosts: async (query: PageQuery & { status?: PostStatus }): Promise<PagedResult<PostSummaryDto>> =>
    (await apiClient.get<PagedResult<PostSummaryDto>>(`${BASE}/posts`, { params: query })).data,

  approvePost: async (id: number): Promise<PostDetailDto> =>
    (await apiClient.post<PostDetailDto>(`${BASE}/posts/${id}/approve`)).data,

  rejectPost: async (id: number, body: RejectPostRequest): Promise<PostDetailDto> =>
    (await apiClient.post<PostDetailDto>(`${BASE}/posts/${id}/reject`, body)).data,

  getReports: async (query: PageQuery & { status?: ReportStatus }): Promise<PagedResult<ReportDto>> =>
    (await apiClient.get<PagedResult<ReportDto>>(`${BASE}/reports`, { params: query })).data,

  handleReport: async (id: number, body: HandleReportRequest): Promise<ReportDto> =>
    (await apiClient.post<ReportDto>(`${BASE}/reports/${id}/handle`, body)).data,

  getViolations: async (query: PageQuery & { userId?: number }): Promise<PagedResult<ViolationDto>> =>
    (await apiClient.get<PagedResult<ViolationDto>>(`${BASE}/violations`, { params: query })).data,

  hideReview: async (id: number): Promise<void> => {
    await apiClient.put(`${BASE}/reviews/${id}/hide`)
  },

  unhideReview: async (id: number): Promise<void> => {
    await apiClient.put(`${BASE}/reviews/${id}/unhide`)
  },
}
