import type { AmenityDto, AreaDto, LandmarkDto, ReasonAppliesTo, ReportReasonDto } from '../types'
import { apiClient } from './client'

const BASE = '/api/catalog'

export const catalogApi = {
  getAreas: async (): Promise<AreaDto[]> => (await apiClient.get<AreaDto[]>(`${BASE}/areas`)).data,

  getLandmarks: async (areaId?: number): Promise<LandmarkDto[]> =>
    (await apiClient.get<LandmarkDto[]>(`${BASE}/landmarks`, { params: { areaId } })).data,

  getAmenities: async (): Promise<AmenityDto[]> => (await apiClient.get<AmenityDto[]>(`${BASE}/amenities`)).data,

  /** `appliesTo` = post | user — backend trả cả lý do áp dụng `both`. */
  getReportReasons: async (appliesTo?: Exclude<ReasonAppliesTo, 'both'>): Promise<ReportReasonDto[]> =>
    (await apiClient.get<ReportReasonDto[]>(`${BASE}/report-reasons`, { params: { appliesTo } })).data,
}
