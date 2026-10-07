import { Form, Input, Modal } from 'antd'
import { REJECT_REASON_MIN, SHORT_TEXT_MAX } from '../../../utils/validation'

interface RejectPostModalProps {
  open: boolean
  postTitle?: string
  loading: boolean
  onCancel: () => void
  onSubmit: (reason: string) => void
}

/** Nhập lý do từ chối tin (bắt buộc, 5–500 ký tự). */
export function RejectPostModal({ open, postTitle, loading, onCancel, onSubmit }: RejectPostModalProps) {
  const [form] = Form.useForm<{ reason: string }>()
  return (
    <Modal
      open={open}
      title={`Từ chối tin${postTitle ? `: ${postTitle}` : ''}`}
      okText="Từ chối"
      okButtonProps={{ danger: true, loading }}
      cancelText="Hủy"
      onOk={() => form.submit()}
      onCancel={onCancel}
      destroyOnHidden
    >
      <Form form={form} layout="vertical" preserve={false} onFinish={(v) => onSubmit(v.reason.trim())}>
        <Form.Item
          name="reason"
          label="Lý do từ chối (gửi tới người đăng)"
          rules={[
            { required: true, whitespace: true, message: 'Vui lòng nhập lý do từ chối' },
            { min: REJECT_REASON_MIN, max: SHORT_TEXT_MAX, message: `Lý do phải từ ${REJECT_REASON_MIN} đến ${SHORT_TEXT_MAX} ký tự` },
          ]}
        >
          <Input.TextArea rows={4} showCount maxLength={SHORT_TEXT_MAX} placeholder="VD: Ảnh không đúng thực tế, thiếu thông tin giá..." />
        </Form.Item>
      </Form>
    </Modal>
  )
}
