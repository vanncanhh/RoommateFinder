import { EditOutlined, PlusOutlined } from '@ant-design/icons'
import { useMutation, useQuery, useQueryClient, type QueryKey } from '@tanstack/react-query'
import { Button, Flex, Form, Modal, Switch, Table, type FormInstance, type TableColumnsType } from 'antd'
import { useState, type ReactNode } from 'react'
import { queryKeys } from '../../../api/queryKeys'
import { ErrorResult } from '../../../components/shared/QueryState'
import { feedback } from '../../../utils/feedback'

interface CatalogApi<TDto, TUpsert> {
  list: () => Promise<TDto[]>
  create: (body: TUpsert) => Promise<TDto>
  update: (id: number, body: TUpsert) => Promise<TDto>
}

export interface CatalogTabProps<TDto extends { isActive: boolean }, TUpsert, TForm extends object> {
  /** Tên loại danh mục, ví dụ "khu vực". */
  entityName: string
  queryKey: QueryKey
  api: CatalogApi<TDto, TUpsert>
  rowKey: keyof TDto & string
  getId: (row: TDto) => number
  columns: TableColumnsType<TDto>
  /** Trường riêng của form (ngoài isActive). */
  renderFields: (form: FormInstance<TForm>) => ReactNode
  defaultValues: Partial<TForm>
  toFormValues: (row: TDto) => TForm
  toUpsert: (values: TForm, isActive: boolean) => TUpsert
  /** Upsert từ dòng hiện có, dùng khi bật/tắt isActive. */
  rowToUpsert: (row: TDto, isActive: boolean) => TUpsert
}

type Editing<TDto> = { mode: 'create' } | { mode: 'edit'; row: TDto }

/** Bảng danh mục + modal thêm/sửa; không có xóa — ẩn bằng công tắc isActive. */
export function CatalogTab<TDto extends { isActive: boolean }, TUpsert, TForm extends object>(
  props: CatalogTabProps<TDto, TUpsert, TForm>,
) {
  const { entityName, queryKey, api, rowKey, getId, columns, renderFields, defaultValues, toFormValues, toUpsert, rowToUpsert } =
    props
  const queryClient = useQueryClient()
  const [form] = Form.useForm<TForm & { isActive: boolean }>()
  const [editing, setEditing] = useState<Editing<TDto> | null>(null)

  const query = useQuery({ queryKey, queryFn: api.list })

  const afterChange = () => {
    void queryClient.invalidateQueries({ queryKey })
    // Danh mục công khai (bộ lọc, form đăng tin) cũng cần làm mới.
    void queryClient.invalidateQueries({ queryKey: ['catalog'] })
    void queryClient.invalidateQueries({ queryKey: queryKeys.posts.all })
  }

  const save = useMutation({
    mutationFn: (values: TForm & { isActive: boolean }) => {
      const body = toUpsert(values, values.isActive)
      return editing?.mode === 'edit' ? api.update(getId(editing.row), body) : api.create(body)
    },
    onSuccess: () => {
      feedback.message.success(editing?.mode === 'edit' ? `Đã cập nhật ${entityName}` : `Đã thêm ${entityName}`)
      setEditing(null)
      afterChange()
    },
  })

  const toggle = useMutation({
    mutationFn: ({ row, isActive }: { row: TDto; isActive: boolean }) => api.update(getId(row), rowToUpsert(row, isActive)),
    onSuccess: (_d, { isActive }) => {
      feedback.message.success(isActive ? `Đã hiển thị ${entityName}` : `Đã ẩn ${entityName}`)
      afterChange()
    },
  })

  const openEditor = (next: Editing<TDto>) => {
    setEditing(next)
    form.resetFields()
    form.setFieldsValue(
      (next.mode === 'edit'
        ? { ...toFormValues(next.row), isActive: next.row.isActive }
        : { ...defaultValues, isActive: true }) as Parameters<typeof form.setFieldsValue>[0],
    )
  }

  const allColumns: TableColumnsType<TDto> = [
    ...columns,
    {
      title: 'Hiển thị',
      key: 'isActive',
      width: 90,
      render: (_, row) => (
        <Switch
          checked={row.isActive}
          size="small"
          loading={toggle.isPending && toggle.variables?.row === row}
          onChange={(checked) => toggle.mutate({ row, isActive: checked })}
          aria-label="Hiển thị"
        />
      ),
    },
    {
      title: '',
      key: 'edit',
      width: 70,
      render: (_, row) => (
        <Button size="small" icon={<EditOutlined />} onClick={() => openEditor({ mode: 'edit', row })} aria-label="Sửa" />
      ),
    },
  ]

  return (
    <>
      <Flex justify="end" style={{ marginBottom: 12 }}>
        <Button type="primary" icon={<PlusOutlined />} onClick={() => openEditor({ mode: 'create' })}>
          Thêm {entityName}
        </Button>
      </Flex>
      {query.isError ? (
        <ErrorResult error={query.error} onRetry={() => void query.refetch()} />
      ) : (
        <Table<TDto>
          rowKey={rowKey}
          size="small"
          columns={allColumns}
          dataSource={query.data ?? []}
          loading={query.isLoading}
          scroll={{ x: 640 }}
          pagination={{ pageSize: 20, showSizeChanger: false, hideOnSinglePage: true }}
          rowClassName={(row) => (row.isActive ? '' : 'catalog-row-inactive')}
        />
      )}
      <Modal
        open={editing !== null}
        title={editing?.mode === 'edit' ? `Sửa ${entityName}` : `Thêm ${entityName}`}
        okText="Lưu"
        cancelText="Hủy"
        okButtonProps={{ loading: save.isPending }}
        onOk={() => form.submit()}
        onCancel={() => setEditing(null)}
        forceRender
      >
        <Form form={form} layout="vertical" onFinish={(v) => save.mutate(v)}>
          {renderFields(form as unknown as FormInstance<TForm>)}
          <Form.Item name="isActive" label="Hiển thị" valuePropName="checked">
            <Switch />
          </Form.Item>
        </Form>
      </Modal>
    </>
  )
}
