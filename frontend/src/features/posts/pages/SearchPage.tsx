import { FilterOutlined } from '@ant-design/icons'
import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { Badge, Button, Card, Col, Drawer, Flex, Grid, Pagination, Row, Select, Spin, Typography } from 'antd'
import { useMemo, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { postsApi } from '../../../api/posts'
import { queryKeys } from '../../../api/queryKeys'
import { PageTitle } from '../../../components/shared/PageTitle'
import { PostGrid } from '../../../components/shared/PostGrid'
import { ErrorResult, LoadingBlock } from '../../../components/shared/QueryState'
import { POST_SORTS, type PostSearchQuery, type PostSort } from '../../../types'
import { SORT_LABELS } from '../../../utils/labels'
import { SearchFilters } from '../components/SearchFilters'
import { defaultSort, parseSearchQuery, toSearchParams, type SearchFilterValues } from '../searchParams'

function countActiveFilters(q: PostSearchQuery): number {
  return [q.keyword, q.postType, q.areaId, q.minPrice, q.maxPrice, q.gender, q.minNeeded, q.landmarkId, q.amenityIds?.length]
    .filter((v) => v !== undefined && v !== 0).length
}

export default function SearchPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const paramsKey = searchParams.toString()
  // Ghi nhớ theo chuỗi query để tránh tạo object mới mỗi lần render.
  const query = useMemo(() => parseSearchQuery(new URLSearchParams(paramsKey)), [paramsKey])
  const screens = Grid.useBreakpoint()
  const isDesktop = Boolean(screens.lg)
  const [drawerOpen, setDrawerOpen] = useState(false)

  const result = useQuery({
    queryKey: queryKeys.posts.search(query),
    queryFn: () => postsApi.search(query),
    placeholderData: keepPreviousData,
  })

  const update = (next: PostSearchQuery) => {
    setSearchParams(toSearchParams(next))
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }

  const applyFilters = (v: SearchFilterValues) => {
    setDrawerOpen(false)
    const landmarkId = v.landmarkId ?? undefined
    update({
      keyword: v.keyword?.trim() || undefined,
      postType: v.postType ?? undefined,
      areaId: v.areaId ?? undefined,
      minPrice: v.minPrice ?? undefined,
      maxPrice: v.maxPrice ?? undefined,
      gender: v.gender ?? undefined,
      minNeeded: v.minNeeded ?? undefined,
      amenityIds: v.amenityIds,
      landmarkId,
      radiusKm: landmarkId ? (v.radiusKm ?? undefined) : undefined,
      // Đang dùng sắp xếp mặc định → để trống cho mặc định mới (theo địa điểm mốc) áp dụng.
      // Bỏ chọn địa điểm mốc → không thể sắp xếp theo khoảng cách nữa (quay về "Mới nhất").
      sort:
        query.sort === defaultSort(query.landmarkId) || (query.sort === 'distance' && !landmarkId)
          ? undefined
          : query.sort,
      page: 1,
    })
  }

  const resetFilters = () => {
    setDrawerOpen(false)
    update({})
  }

  const sortOptions = POST_SORTS.map((s) => ({
    value: s,
    label: SORT_LABELS[s],
    disabled: s === 'distance' && query.landmarkId === undefined,
  }))

  const filters = <SearchFilters query={query} onApply={applyFilters} onReset={resetFilters} />
  const activeCount = countActiveFilters(query)

  return (
    <>
      <PageTitle title="Tìm phòng ở ghép" />
      <Row gutter={[16, 16]}>
        {isDesktop && (
          <Col lg={7} xl={6}>
            <Card title="Bộ lọc" size="small" style={{ position: 'sticky', top: 80 }}>
              {filters}
            </Card>
          </Col>
        )}
        <Col xs={24} lg={17} xl={18}>
          <Flex justify="space-between" align="center" gap={8} wrap="wrap" style={{ marginBottom: 12 }}>
            <Flex gap={8} align="center">
              {!isDesktop && (
                <Badge count={activeCount} size="small">
                  <Button icon={<FilterOutlined />} onClick={() => setDrawerOpen(true)}>
                    Bộ lọc
                  </Button>
                </Badge>
              )}
              <Typography.Text type="secondary">
                {result.data ? `${result.data.totalCount.toLocaleString('vi-VN')} tin phù hợp` : ''}
              </Typography.Text>
              {result.isFetching && !result.isLoading && <Spin size="small" />}
            </Flex>
            <Select<PostSort>
              value={query.sort ?? defaultSort(query.landmarkId)}
              style={{ minWidth: 200 }}
              options={sortOptions}
              onChange={(sort) => update({ ...query, sort, page: 1 })}
              aria-label="Sắp xếp"
            />
          </Flex>

          {result.isLoading ? (
            <LoadingBlock rows={10} />
          ) : result.isError ? (
            <ErrorResult error={result.error} onRetry={() => void result.refetch()} />
          ) : (
            <>
              <PostGrid posts={result.data?.items ?? []} emptyText="Không tìm thấy tin phù hợp. Hãy thử nới rộng bộ lọc." />
              {result.data && result.data.totalCount > result.data.pageSize && (
                <Flex justify="center" style={{ marginTop: 24 }}>
                  <Pagination
                    current={result.data.page}
                    pageSize={result.data.pageSize}
                    total={result.data.totalCount}
                    showSizeChanger={false}
                    size={screens.sm ? 'default' : 'small'}
                    onChange={(page) => update({ ...query, page })}
                  />
                </Flex>
              )}
            </>
          )}
        </Col>
      </Row>

      {!isDesktop && (
        <Drawer title="Bộ lọc" placement="left" open={drawerOpen} onClose={() => setDrawerOpen(false)} width={320}>
          {filters}
        </Drawer>
      )}
    </>
  )
}
