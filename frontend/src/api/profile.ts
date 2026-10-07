import type { ProfileDto, UpdateProfileRequest } from '../types'
import { apiClient } from './client'

const BASE = '/api/profile'

export const profileApi = {
  get: async (): Promise<ProfileDto> => (await apiClient.get<ProfileDto>(BASE)).data,

  update: async (body: UpdateProfileRequest): Promise<ProfileDto> => (await apiClient.put<ProfileDto>(BASE, body)).data,

  uploadAvatar: async (file: File): Promise<ProfileDto> => {
    const fd = new FormData()
    fd.append('file', file)
    return (await apiClient.post<ProfileDto>(`${BASE}/avatar`, fd)).data
  },
}
