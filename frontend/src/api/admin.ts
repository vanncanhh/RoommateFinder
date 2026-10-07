import type {
  AdminUserDto,
  AdminUserQuery,
  AmenityDto,
  AmenityUpsertRequest,
  AreaDto,
  AreaUpsertRequest,
  DashboardDto,
  DashboardQuery,
  LandmarkDto,
  LandmarkUpsertRequest,
  PagedResult,
  ReportReasonDto,
  ReportReasonUpsertRequest,
  SystemConfigDto,
  UpdateConfigRequest,
  UpdateRoleRequest,
  UpdateUserStatusRequest,
} from '../types'
import { compact } from '../utils/query'
import { apiClient } from './client'

const BASE = '/api/admin'

/** CRUD danh mục (không có DELETE — ẩn bằng isActive). */
function catalogCrud<TDto, TUpsert>(path: string) {
  return {
    list: async (): Promise<TDto[]> => (await apiClient.get<TDto[]>(`${BASE}/${path}`)).data,
    create: async (body: TUpsert): Promise<TDto> => (await apiClient.post<TDto>(`${BASE}/${path}`, body)).data,
    update: async (id: number, body: TUpsert): Promise<TDto> =>
      (await apiClient.put<TDto>(`${BASE}/${path}/${id}`, body)).data,
  }
}

export const adminApi = {
  getUsers: async (query: AdminUserQuery): Promise<PagedResult<AdminUserDto>> =>
    (await apiClient.get<PagedResult<AdminUserDto>>(`${BASE}/users`, { params: compact(query) })).data,

  updateRole: async (id: number, body: UpdateRoleRequest): Promise<AdminUserDto> =>
    (await apiClient.put<AdminUserDto>(`${BASE}/users/${id}/role`, body)).data,

  updateStatus: async (id: number, body: UpdateUserStatusRequest): Promise<AdminUserDto> =>
    (await apiClient.put<AdminUserDto>(`${BASE}/users/${id}/status`, body)).data,

  areas: catalogCrud<AreaDto, AreaUpsertRequest>('areas'),
  landmarks: catalogCrud<LandmarkDto, LandmarkUpsertRequest>('landmarks'),
  amenities: catalogCrud<AmenityDto, AmenityUpsertRequest>('amenities'),
  reportReasons: catalogCrud<ReportReasonDto, ReportReasonUpsertRequest>('report-reasons'),

  getConfigs: async (): Promise<SystemConfigDto[]> => (await apiClient.get<SystemConfigDto[]>(`${BASE}/configs`)).data,

  updateConfig: async (key: string, body: UpdateConfigRequest): Promise<SystemConfigDto> =>
    (await apiClient.put<SystemConfigDto>(`${BASE}/configs/${encodeURIComponent(key)}`, body)).data,

  getDashboard: async (query: DashboardQuery): Promise<DashboardDto> =>
    (await apiClient.get<DashboardDto>(`${BASE}/dashboard`, { params: compact(query) })).data,
}
