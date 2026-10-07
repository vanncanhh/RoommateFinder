import type { PagedResult, PostDetailDto, PostFormData, PostSearchQuery, PostStatus, PostSummaryDto } from '../types'
import { compact } from '../utils/query'
import { appendFormValue, apiClient } from './client'

const BASE = '/api/posts'

/** Dựng multipart/form-data cho POST/PUT tin đăng: trường PostFormData + `amenityIds`/`keepImageIds` lặp lại + file `images`. */
export function buildPostFormData(data: PostFormData, images: File[], includeKeepImageIds: boolean): FormData {
  const fd = new FormData()
  appendFormValue(fd, 'postType', data.postType)
  appendFormValue(fd, 'title', data.title)
  appendFormValue(fd, 'description', data.description)
  appendFormValue(fd, 'price', data.price)
  appendFormValue(fd, 'areaId', data.areaId)
  appendFormValue(fd, 'address', data.address)
  appendFormValue(fd, 'latitude', data.latitude)
  appendFormValue(fd, 'longitude', data.longitude)
  appendFormValue(fd, 'currentOccupants', data.currentOccupants)
  appendFormValue(fd, 'neededOccupants', data.neededOccupants)
  appendFormValue(fd, 'preferredGender', data.preferredGender)
  appendFormValue(fd, 'amenityIds', data.amenityIds)
  if (includeKeepImageIds) appendFormValue(fd, 'keepImageIds', data.keepImageIds)
  images.forEach((file) => fd.append('images', file, file.name))
  return fd
}

export const postsApi = {
  search: async (query: PostSearchQuery): Promise<PagedResult<PostSummaryDto>> =>
    (await apiClient.get<PagedResult<PostSummaryDto>>(`${BASE}/search`, { params: compact(query) })).data,

  latest: async (count = 8): Promise<PostSummaryDto[]> =>
    (await apiClient.get<PostSummaryDto[]>(`${BASE}/latest`, { params: { count } })).data,

  get: async (id: number): Promise<PostDetailDto> => (await apiClient.get<PostDetailDto>(`${BASE}/${id}`)).data,

  create: async (data: PostFormData, images: File[]): Promise<PostDetailDto> =>
    (await apiClient.post<PostDetailDto>(BASE, buildPostFormData(data, images, false))).data,

  update: async (id: number, data: PostFormData, images: File[]): Promise<PostDetailDto> =>
    (await apiClient.put<PostDetailDto>(`${BASE}/${id}`, buildPostFormData(data, images, true))).data,

  remove: async (id: number): Promise<void> => {
    await apiClient.delete(`${BASE}/${id}`)
  },

  extend: async (id: number): Promise<PostDetailDto> => (await apiClient.put<PostDetailDto>(`${BASE}/${id}/extend`)).data,

  close: async (id: number): Promise<PostDetailDto> => (await apiClient.put<PostDetailDto>(`${BASE}/${id}/close`)).data,

  mine: async (status?: PostStatus): Promise<PostSummaryDto[]> =>
    (await apiClient.get<PostSummaryDto[]>(`${BASE}/mine`, { params: { status } })).data,

  saved: async (): Promise<PostSummaryDto[]> => (await apiClient.get<PostSummaryDto[]>(`${BASE}/saved`)).data,

  save: async (id: number): Promise<void> => {
    await apiClient.post(`${BASE}/${id}/save`)
  },

  unsave: async (id: number): Promise<void> => {
    await apiClient.delete(`${BASE}/${id}/save`)
  },
}
