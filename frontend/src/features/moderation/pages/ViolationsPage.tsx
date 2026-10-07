import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { Button, InputNumber, Space, Table, Typography, type TableColumnsType } from 'antd'
import { useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { moderationApi } from '../../../api/moderation'
import { queryKeys } from '../../../api/queryKeys'
import { EnumTag } from '../../../components/shared/EnumTag'
import { PageTitle } from '../../../components/shared/PageTitle'
import { ErrorResult } from '../../../components/shared/QueryState'
import type { ViolationDto } from '../../../types'
import { formatDateTime } from '../../../utils/datetime'
import { VIOLATION_ACTION_LABELS, VIOLATION_LEVEL_LABELS, labelOf } from '../../../utils/labels'
import { toNumber } from '../../../utils/query'

const PAGE_SIZE = 20

/** Lịch sử vi phạm, lọc theo userId (tùy chọn, đồng bộ URL). */
export default function ViolationsPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const userId = toNumber(searchParams.get('userId'))
  const page = Math.max(1, toNumber(searchParams.get('page')) ?? 1)
  const [userIdInput, setUserIdInput] = useState<number | null>(userId ?? null)

  const query = useQuery({
    queryKey: queryKeys.moderation.violations(userId, page, PAGE_SIZE),
    queryFn: () => moderationApi.getViolations({ userId, page, pageSize: PAGE_SIZE }),
    placeholderData: keepPreviousData,
  })

  const applyFilter = (id: number | null) => {
    const params = new URLSearchParams()
    if (id) params.set('userId', String(id))
    setSearchParams(params)
  }

  const columns: TableColumnsType<ViolationDto> = [
    { title: 'Thời gian', dataIndex: 'createdAt', width: 140, render: (v: string) => formatDateTime(v) },
    {
      title: 'Người vi phạm',
      key: 'user',
      render: (_, v) => (
        <Space size={4} wrap>
          <Link to={`/users/${v.userId}`} target="_blank">
            {v.userName}
          </Link>
          <Typography.Link onClick={() => applyFilter(v.userId)} style={{ fontSize: 12 }}>
            (lọc)
          </Typography.Link>
        </Space>
      ),
    },
    { title: 'Mức độ', dataIndex: 'level', width: 110, render: (l: string) => <EnumTag map={VIOLATION_LEVEL_LABELS} value={l} /> },
    {
      title: 'Hình thức xử lý',
      key: 'action',
      render: (_, v) =>
        `${labelOf(VIOLATION_ACTION_LABELS, v.action)}${v.suspendDays ? ` (${v.suspendDays} ngày)` : ''}`,
    },
    {
      title: 'Liên quan',
      key: 'related',
      width: 130,
      render: (_, v) => (
        <Space direction="vertical" size={0}>
          {v.postId && (
            <Link to={`/posts/${v.postId}`} target="_blank">
              Tin #{v.postId}
            </Link>
          )}
          {v.reportId && <Typography.Text type="secondary">Báo cáo #{v.reportId}</Typography.Text>}
        </Space>
      ),
    },
    { title: 'Ghi chú', dataIndex: 'note', render: (n: string | null) => n ?? '—' },
    { title: 'Người xử lý', dataIndex: 'handlerName', width: 140 },
  ]

  return (
    <>
      <PageTitle
        title="Lịch sử vi phạm"
        subTitle={query.data ? `${query.data.totalCount} bản ghi` : undefined}
        extra={
          <Space.Compact>
            <InputNumber<number>
              min={1}
              placeholder="Mã người dùng"
              value={userIdInput}
              onChange={setUserIdInput}
              onPressEnter={() => applyFilter(userIdInput)}
              style={{ width: 150 }}
            />
            <Button type="primary" onClick={() => applyFilter(userIdInput)}>
              Lọc
            </Button>
            {userId && (
              <Button
                onClick={() => {
                  setUserIdInput(null)
                  applyFilter(null)
                }}
              >
                Bỏ lọc
              </Button>
            )}
          </Space.Compact>
        }
      />
      {query.isError ? (
        <ErrorResult error={query.error} onRetry={() => void query.refetch()} />
      ) : (
        <Table<ViolationDto>
          rowKey="violationId"
          columns={columns}
          dataSource={query.data?.items ?? []}
          loading={query.isFetching}
          scroll={{ x: 900 }}
          locale={{ emptyText: 'Không có vi phạm nào' }}
          pagination={{
            current: page,
            pageSize: PAGE_SIZE,
            total: query.data?.totalCount ?? 0,
            showSizeChanger: false,
            onChange: (p) => {
              const params = new URLSearchParams(searchParams)
              params.set('page', String(p))
              setSearchParams(params)
            },
          }}
        />
      )}
    </>
  )
}
