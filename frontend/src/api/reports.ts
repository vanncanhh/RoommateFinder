import type { CreateReportRequest, MessageResponse } from '../types'
import { apiClient } from './client'

export const reportsApi = {
  create: async (body: CreateReportRequest): Promise<MessageResponse> =>
    (await apiClient.post<MessageResponse>('/api/reports', body)).data,
}
