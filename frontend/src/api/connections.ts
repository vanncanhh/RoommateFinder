import type { ConnectionRequestDto, CreateConnectionRequest, RequestStatus } from '../types'
import { apiClient } from './client'

const BASE = '/api/connections'

export const connectionsApi = {
  create: async (body: CreateConnectionRequest): Promise<ConnectionRequestDto> =>
    (await apiClient.post<ConnectionRequestDto>(BASE, body)).data,

  incoming: async (status?: RequestStatus): Promise<ConnectionRequestDto[]> =>
    (await apiClient.get<ConnectionRequestDto[]>(`${BASE}/incoming`, { params: { status } })).data,

  outgoing: async (status?: RequestStatus): Promise<ConnectionRequestDto[]> =>
    (await apiClient.get<ConnectionRequestDto[]>(`${BASE}/outgoing`, { params: { status } })).data,

  accept: async (id: number): Promise<ConnectionRequestDto> =>
    (await apiClient.put<ConnectionRequestDto>(`${BASE}/${id}/accept`)).data,

  reject: async (id: number): Promise<ConnectionRequestDto> =>
    (await apiClient.put<ConnectionRequestDto>(`${BASE}/${id}/reject`)).data,

  cancel: async (id: number): Promise<ConnectionRequestDto> =>
    (await apiClient.put<ConnectionRequestDto>(`${BASE}/${id}/cancel`)).data,
}
