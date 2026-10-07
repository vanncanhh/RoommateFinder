import type { PagedResult, PageQuery, PostSummaryDto, PublicProfileDto, ReviewDto } from '../types'
import { apiClient } from './client'

const BASE = '/api/users'

export const usersApi = {
  getPublicProfile: async (id: number): Promise<PublicProfileDto> =>
    (await apiClient.get<PublicProfileDto>(`${BASE}/${id}`)).data,

  getReviews: async (id: number, query: PageQuery): Promise<PagedResult<ReviewDto>> =>
    (await apiClient.get<PagedResult<ReviewDto>>(`${BASE}/${id}/reviews`, { params: query })).data,

  getPosts: async (id: number): Promise<PostSummaryDto[]> =>
    (await apiClient.get<PostSummaryDto[]>(`${BASE}/${id}/posts`)).data,
}
