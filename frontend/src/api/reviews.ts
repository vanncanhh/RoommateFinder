import type { CreateReviewRequest, ReviewDto } from '../types'
import { apiClient } from './client'

export const reviewsApi = {
  create: async (body: CreateReviewRequest): Promise<ReviewDto> =>
    (await apiClient.post<ReviewDto>('/api/reviews', body)).data,
}
