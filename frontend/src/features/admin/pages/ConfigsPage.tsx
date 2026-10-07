import { CheckOutlined, CloseOutlined, EditOutlined } from '@ant-design/icons'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Button, Input, InputNumber, Space, Table, Typography, type TableColumnsType } from 'antd'
import { useState } from 'react'
import { adminApi } from '../../../api/admin'
import { queryKeys } from '../../../api/queryKeys'
import { PageTitle } from '../../../components/shared/PageTitle'
import { ErrorResult } from '../../../components/shared/QueryState'
import type { SystemConfigDto } from '../../../types'
import { feedback } from '../../../utils/feedback'

interface EditingState {
  key: string
  value: string
}

function rangeError(config: SystemConfigDto, value: string): string | null {
  const isNumeric = config.min !== null || config.max !== null
  if (!isNumeric) return value.trim() === '' ? 'Giá trị không được để trống' : null
  if (!/^-?\d+$/.test(value.trim())) return 'Giá trị phải là số nguyên'
  const n = Number(value)
  if (config.min !== null && n < config.min) return `Giá trị tối thiểu là ${config.min}`
  if (config.max !== null && n > config.max) return `Giá trị tối đa là ${config.max}`
  return null
}

/** Cấu hình hệ thống: sửa trực tiếp trên bảng, kiểm tra min/max. */
export default function ConfigsPage() {
  const queryClient = useQueryClient()
  const [editing, setEditing] = useState<EditingState | null>(null)
  const query = useQuery({ queryKey: queryKeys.admin.configs, queryFn: adminApi.getConfigs })

  const save = useMutation({
    mutationFn: ({ key, value }: EditingState) => adminApi.updateConfig(key, { value }),
    onSuccess: (updated) => {
      feedback.message.success(`Đã lưu ${updated.configKey}`)
      setEditing(null)
      void queryClient.invalidateQueries({ queryKey: queryKeys.admin.configs })
    },
  })

  const submit = (config: SystemConfigDto) => {
    if (!editing) return
    const error = rangeError(config, editing.value)
    if (error) {
      feedback.message.error(error)
      return
    }
    save.mutate({ key: config.configKey, value: editing.value.trim() })
  }

  const columns: TableColumnsType<SystemConfigDto> = [
    {
      title: 'Khóa',
      dataIndex: 'configKey',
      width: 220,
      render: (v: string) => <Typography.Text code>{v}</Typography.Text>,
    },
    { title: 'Mô tả', dataIndex: 'description', render: (v: string | null) => v ?? '—' },
    {
      title: 'Giá trị',
      key: 'value',
      width: 260,
      render: (_, c) => {
        const isEditing = editing?.key === c.configKey
        if (!isEditing) {
          return (
            <Space>
              <Typography.Text strong>{c.configValue}</Typography.Text>
              <Button
                size="small"
                type="text"
                icon={<EditOutlined />}
                aria-label="Sửa"
                disabled={editing !== null}
                onClick={() => setEditing({ key: c.configKey, value: c.configValue })}
              />
            </Space>
          )
        }
        const numeric = c.min !== null || c.max !== null
        return (
          <Space.Compact>
            {numeric ? (
              <InputNumber
                autoFocus
                size="small"
                precision={0}
                min={c.min ?? undefined}
                max={c.max ?? undefined}
                value={editing.value === '' ? null : Number(editing.value)}
                onChange={(v) => setEditing({ key: c.configKey, value: v === null ? '' : String(v) })}
                onPressEnter={() => submit(c)}
                style={{ width: 120 }}
              />
            ) : (
              <Input
                autoFocus
                size="small"
                value={editing.value}
                onChange={(e) => setEditing({ key: c.configKey, value: e.target.value })}
                onPressEnter={() => submit(c)}
                style={{ width: 140 }}
              />
            )}
            <Button size="small" type="primary" icon={<CheckOutlined />} loading={save.isPending} onClick={() => submit(c)} aria-label="Lưu" />
            <Button size="small" icon={<CloseOutlined />} onClick={() => setEditing(null)} aria-label="Hủy" />
          </Space.Compact>
        )
      },
    },
    {
      title: 'Giới hạn',
      key: 'range',
      width: 130,
      render: (_, c) =>
        c.min !== null || c.max !== null ? (
          <Typography.Text type="secondary">
            {c.min ?? '−∞'} – {c.max ?? '∞'}
          </Typography.Text>
        ) : (
          '—'
        ),
    },
  ]

  return (
    <>
      <PageTitle title="Cấu hình hệ thống" subTitle="Thay đổi có hiệu lực ngay, không cần khởi động lại máy chủ." />
      {query.isError ? (
        <ErrorResult error={query.error} onRetry={() => void query.refetch()} />
      ) : (
        <Table<SystemConfigDto>
          rowKey="configKey"
          columns={columns}
          dataSource={query.data ?? []}
          loading={query.isLoading}
          pagination={false}
          scroll={{ x: 800 }}
        />
      )}
    </>
  )
}
