import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { App, Button, Col, Input, Row, Select, Space, Table, Typography, type TableColumnsType } from 'antd'
import { useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { adminApi } from '../../../api/admin'
import { queryKeys } from '../../../api/queryKeys'
import { EnumTag } from '../../../components/shared/EnumTag'
import { PageTitle } from '../../../components/shared/PageTitle'
import { ErrorResult } from '../../../components/shared/QueryState'
import { RatingBadge } from '../../../components/shared/RatingBadge'
import { useAuth } from '../../../hooks/useAuth'
import {
  ROLES,
  USER_STATUSES,
  type AdminUserDto,
  type AdminUserQuery,
  type Role,
  type UpdateUserStatusRequest,
} from '../../../types'
import { formatDate, formatDateTime } from '../../../utils/datetime'
import { feedback } from '../../../utils/feedback'
import { ROLE_LABELS, USER_STATUS_LABELS, toOptions } from '../../../utils/labels'
import { oneOf, toNumber } from '../../../utils/query'
import { UserStatusModal } from '../components/UserStatusModal'

const PAGE_SIZE = 20

export default function UsersPage() {
  const queryClient = useQueryClient()
  const { user: me } = useAuth()
  const { modal } = App.useApp()
  const [searchParams, setSearchParams] = useSearchParams()
  const [editingStatus, setEditingStatus] = useState<AdminUserDto | null>(null)

  const q: AdminUserQuery = {
    keyword: searchParams.get('keyword') || undefined,
    role: oneOf(ROLES, searchParams.get('role')),
    status: oneOf(USER_STATUSES, searchParams.get('status')),
    page: Math.max(1, toNumber(searchParams.get('page')) ?? 1),
    pageSize: PAGE_SIZE,
  }

  const query = useQuery({
    queryKey: queryKeys.admin.users(q),
    queryFn: () => adminApi.getUsers(q),
    placeholderData: keepPreviousData,
  })

  const setParam = (key: string, value: string | undefined) => {
    const params = new URLSearchParams(searchParams)
    if (value) params.set(key, value)
    else params.delete(key)
    if (key !== 'page') params.delete('page')
    setSearchParams(params)
  }

  const invalidate = () => queryClient.invalidateQueries({ queryKey: queryKeys.admin.all })

  const changeRole = useMutation({
    mutationFn: ({ id, role }: { id: number; role: Role }) => adminApi.updateRole(id, { role }),
    onSuccess: () => {
      feedback.message.success('Đã đổi vai trò')
      void invalidate()
    },
  })

  const changeStatus = useMutation({
    mutationFn: ({ id, body }: { id: number; body: UpdateUserStatusRequest }) => adminApi.updateStatus(id, body),
    onSuccess: () => {
      feedback.message.success('Đã cập nhật trạng thái tài khoản')
      setEditingStatus(null)
      void invalidate()
    },
  })

  const confirmRole = (u: AdminUserDto, role: Role) =>
    modal.confirm({
      title: 'Đổi vai trò người dùng?',
      content: `${u.fullName}: ${ROLE_LABELS[u.role].label} → ${ROLE_LABELS[role].label}`,
      okText: 'Đổi vai trò',
      cancelText: 'Hủy',
      onOk: () => changeRole.mutateAsync({ id: u.userId, role }),
    })

  const columns: TableColumnsType<AdminUserDto> = [
    { title: 'ID', dataIndex: 'userId', width: 70 },
    {
      title: 'Người dùng',
      key: 'name',
      render: (_, u) => (
        <Space direction="vertical" size={0}>
          <Link to={`/users/${u.userId}`} target="_blank">
            {u.fullName}
          </Link>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            {u.email}
            {u.phone && ` · ${u.phone}`}
          </Typography.Text>
        </Space>
      ),
    },
    {
      title: 'Vai trò',
      key: 'role',
      width: 170,
      render: (_, u) => (
        <Select<Role>
          size="small"
          value={u.role}
          style={{ width: 150 }}
          disabled={u.userId === me?.userId}
          options={toOptions(ROLES, ROLE_LABELS)}
          onChange={(role) => confirmRole(u, role)}
        />
      ),
    },
    {
      title: 'Trạng thái',
      key: 'status',
      width: 170,
      render: (_, u) => (
        <Space direction="vertical" size={0}>
          <EnumTag map={USER_STATUS_LABELS} value={u.status} />
          {u.status === 'suspended' && u.suspendedUntil && (
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              đến {formatDateTime(u.suspendedUntil)}
            </Typography.Text>
          )}
        </Space>
      ),
    },
    {
      title: 'Hoạt động',
      key: 'stats',
      width: 190,
      render: (_, u) => (
        <Space direction="vertical" size={0}>
          <RatingBadge rating={u.avgRating} reviewCount={u.reviewCount} />
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            {u.postCount} tin ·{' '}
            {u.violationCount > 0 ? (
              <Link to={`/moderator/violations?userId=${u.userId}`}>{u.violationCount} vi phạm</Link>
            ) : (
              '0 vi phạm'
            )}
          </Typography.Text>
        </Space>
      ),
    },
    { title: 'Ngày tạo', dataIndex: 'createdAt', width: 110, render: (v: string) => formatDate(v) },
    {
      title: '',
      key: 'actions',
      width: 130,
      render: (_, u) => (
        <Button size="small" disabled={u.userId === me?.userId} onClick={() => setEditingStatus(u)}>
          Đổi trạng thái
        </Button>
      ),
    },
  ]

  return (
    <>
      <PageTitle title="Quản lý người dùng" subTitle={query.data ? `${query.data.totalCount} người dùng` : undefined} />
      <Row gutter={[8, 8]} style={{ marginBottom: 12 }}>
        <Col xs={24} md={10}>
          <Input.Search
            allowClear
            placeholder="Tìm theo tên, email, số điện thoại"
            defaultValue={q.keyword}
            onSearch={(v) => setParam('keyword', v.trim() || undefined)}
          />
        </Col>
        <Col xs={12} md={5}>
          <Select<Role>
            allowClear
            placeholder="Vai trò"
            style={{ width: '100%' }}
            value={q.role}
            options={toOptions(ROLES, ROLE_LABELS)}
            onChange={(v) => setParam('role', v)}
          />
        </Col>
        <Col xs={12} md={5}>
          <Select
            allowClear
            placeholder="Trạng thái"
            style={{ width: '100%' }}
            value={q.status}
            options={toOptions(USER_STATUSES, USER_STATUS_LABELS)}
            onChange={(v: string | undefined) => setParam('status', v)}
          />
        </Col>
      </Row>
      {query.isError ? (
        <ErrorResult error={query.error} onRetry={() => void query.refetch()} />
      ) : (
        <Table<AdminUserDto>
          rowKey="userId"
          columns={columns}
          dataSource={query.data?.items ?? []}
          loading={query.isFetching}
          scroll={{ x: 1050 }}
          pagination={{
            current: q.page,
            pageSize: PAGE_SIZE,
            total: query.data?.totalCount ?? 0,
            showSizeChanger: false,
            onChange: (p) => setParam('page', String(p)),
          }}
        />
      )}
      <UserStatusModal
        user={editingStatus}
        loading={changeStatus.isPending}
        onCancel={() => setEditingStatus(null)}
        onSubmit={(body) => editingStatus && changeStatus.mutate({ id: editingStatus.userId, body })}
      />
    </>
  )
}
