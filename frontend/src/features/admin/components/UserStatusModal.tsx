import { Alert, Form, Input, InputNumber, Modal, Radio } from 'antd'
import { USER_STATUSES, type AdminUserDto, type UpdateUserStatusRequest, type UserStatus } from '../../../types'
import { formatDateTime } from '../../../utils/datetime'
import { USER_STATUS_LABELS } from '../../../utils/labels'
import { SHORT_TEXT_MAX, SUSPEND_DAYS_MAX } from '../../../utils/validation'

interface UserStatusModalProps {
  user: AdminUserDto | null
  loading: boolean
  onCancel: () => void
  onSubmit: (body: UpdateUserStatusRequest) => void
}

interface StatusFormValues {
  status: UserStatus
  suspendDays?: number
  reason?: string
}

/** Đổi trạng thái tài khoản: hoạt động / tạm khóa (số ngày + lý do) / cấm (lý do). */
export function UserStatusModal({ user, loading, onCancel, onSubmit }: UserStatusModalProps) {
  const [form] = Form.useForm<StatusFormValues>()
  const status = Form.useWatch('status', form)
  const needsReason = status === 'suspended' || status === 'banned'

  return (
    <Modal
      open={user !== null}
      title={`Đổi trạng thái: ${user?.fullName ?? ''}`}
      okText="Cập nhật"
      cancelText="Hủy"
      okButtonProps={{ loading, danger: needsReason }}
      onOk={() => form.submit()}
      onCancel={onCancel}
      destroyOnHidden
    >
      {user?.status === 'suspended' && user.suspendedUntil && (
        <Alert
          type="warning"
          showIcon
          message={`Đang tạm khóa đến ${formatDateTime(user.suspendedUntil)}`}
          style={{ marginBottom: 16 }}
        />
      )}
      <Form
        form={form}
        layout="vertical"
        preserve={false}
        initialValues={{ status: user?.status ?? 'active' }}
        onFinish={(v) =>
          onSubmit({
            status: v.status,
            suspendDays: v.status === 'suspended' ? (v.suspendDays ?? null) : null,
            reason: v.reason?.trim() || null,
          })
        }
      >
        <Form.Item name="status" label="Trạng thái">
          <Radio.Group
            optionType="button"
            buttonStyle="solid"
            options={USER_STATUSES.map((s) => ({ value: s, label: USER_STATUS_LABELS[s].label }))}
          />
        </Form.Item>
        {status === 'suspended' && (
          <Form.Item name="suspendDays" label="Số ngày tạm khóa" rules={[{ required: true, message: 'Nhập số ngày (1–365)' }]}>
            <InputNumber min={1} max={SUSPEND_DAYS_MAX} precision={0} style={{ width: '100%' }} suffix="ngày" />
          </Form.Item>
        )}
        <Form.Item
          name="reason"
          label="Lý do"
          rules={[
            { required: needsReason, whitespace: true, message: 'Vui lòng nhập lý do khóa' },
            { max: SHORT_TEXT_MAX, message: `Tối đa ${SHORT_TEXT_MAX} ký tự` },
          ]}
        >
          <Input.TextArea rows={3} showCount maxLength={SHORT_TEXT_MAX} />
        </Form.Item>
      </Form>
    </Modal>
  )
}
