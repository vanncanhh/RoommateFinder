import { GENDERS, POST_SORTS, POST_TYPES, type Gender, type PostSearchQuery, type PostSort, type PostType } from '../../types'
import { oneOf, toNumber } from '../../utils/query'

export const SEARCH_PAGE_SIZE = 12

/** Sắp xếp mặc định: theo khoảng cách khi có địa điểm mốc (giống backend), ngược lại mới nhất. */
export function defaultSort(landmarkId: number | undefined): PostSort {
  return landmarkId !== undefined ? 'distance' : 'newest'
}

/** Đọc bộ lọc tìm kiếm từ URL query string (nguồn sự thật của trang tìm kiếm). */
export function parseSearchQuery(params: URLSearchParams): PostSearchQuery {
  const amenityIds = params
    .getAll('amenityIds')
    .map((v) => toNumber(v))
    .filter((v): v is number => v !== undefined)
  const landmarkId = toNumber(params.get('landmarkId'))
  let sort = oneOf(POST_SORTS, params.get('sort'))
  // Sắp xếp theo khoảng cách chỉ hợp lệ khi có địa điểm mốc.
  if (sort === 'distance' && landmarkId === undefined) sort = undefined
  // Luôn có sắp xếp hiệu lực: gửi tường minh lên API và hiển thị đúng trên dropdown.
  sort ??= defaultSort(landmarkId)

  return {
    keyword: params.get('keyword')?.trim() || undefined,
    postType: oneOf(POST_TYPES, params.get('postType')),
    areaId: toNumber(params.get('areaId')),
    minPrice: toNumber(params.get('minPrice')),
    maxPrice: toNumber(params.get('maxPrice')),
    gender: oneOf(GENDERS, params.get('gender')),
    minNeeded: toNumber(params.get('minNeeded')),
    landmarkId,
    radiusKm: landmarkId !== undefined ? toNumber(params.get('radiusKm')) : undefined,
    amenityIds: amenityIds.length > 0 ? amenityIds : undefined,
    sort,
    page: Math.max(1, toNumber(params.get('page')) ?? 1),
    pageSize: SEARCH_PAGE_SIZE,
  }
}

/** Ghi bộ lọc ra URL query string (bỏ giá trị rỗng, amenityIds lặp lại). */
export function toSearchParams(q: PostSearchQuery): URLSearchParams {
  const params = new URLSearchParams()
  const set = (key: string, value: string | number | undefined) => {
    if (value !== undefined && value !== '') params.set(key, String(value))
  }
  set('keyword', q.keyword)
  set('postType', q.postType)
  set('areaId', q.areaId)
  set('minPrice', q.minPrice)
  set('maxPrice', q.maxPrice)
  set('gender', q.gender)
  set('minNeeded', q.minNeeded)
  set('landmarkId', q.landmarkId)
  if (q.landmarkId !== undefined) set('radiusKm', q.radiusKm)
  q.amenityIds?.forEach((id) => params.append('amenityIds', String(id)))
  // Chỉ ghi sort khi hợp lệ và khác mặc định (mặc định phụ thuộc việc có địa điểm mốc hay không).
  const sortValid = !(q.sort === 'distance' && q.landmarkId === undefined)
  if (q.sort && sortValid && q.sort !== defaultSort(q.landmarkId)) set('sort', q.sort)
  if (q.page && q.page > 1) set('page', q.page)
  return params
}

/** Giá trị form bộ lọc (InputNumber trả null khi xóa). */
export interface SearchFilterValues {
  keyword?: string
  postType?: PostType
  areaId?: number
  minPrice?: number | null
  maxPrice?: number | null
  gender?: Gender
  minNeeded?: number | null
  amenityIds?: number[]
  landmarkId?: number
  radiusKm?: number
}

export function toFilterValues(q: PostSearchQuery): SearchFilterValues {
  return {
    keyword: q.keyword,
    postType: q.postType,
    areaId: q.areaId,
    minPrice: q.minPrice ?? null,
    maxPrice: q.maxPrice ?? null,
    gender: q.gender,
    minNeeded: q.minNeeded ?? null,
    amenityIds: q.amenityIds ?? [],
    landmarkId: q.landmarkId,
    radiusKm: q.radiusKm,
  }
}
