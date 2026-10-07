import { useQuery } from '@tanstack/react-query'
import { catalogApi } from '../api/catalog'
import { queryKeys } from '../api/queryKeys'

/** Danh mục ít thay đổi → cache lâu. */
const CATALOG_STALE_TIME = 30 * 60_000

export function useAreas() {
  return useQuery({ queryKey: queryKeys.catalog.areas, queryFn: catalogApi.getAreas, staleTime: CATALOG_STALE_TIME })
}

export function useLandmarks(areaId?: number) {
  return useQuery({
    queryKey: queryKeys.catalog.landmarks(areaId),
    queryFn: () => catalogApi.getLandmarks(areaId),
    staleTime: CATALOG_STALE_TIME,
  })
}

export function useAmenities() {
  return useQuery({
    queryKey: queryKeys.catalog.amenities,
    queryFn: catalogApi.getAmenities,
    staleTime: CATALOG_STALE_TIME,
  })
}

export function useReportReasons(appliesTo: 'post' | 'user', enabled = true) {
  return useQuery({
    queryKey: queryKeys.catalog.reportReasons(appliesTo),
    queryFn: () => catalogApi.getReportReasons(appliesTo),
    staleTime: CATALOG_STALE_TIME,
    enabled,
  })
}
