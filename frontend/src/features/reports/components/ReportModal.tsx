import { useMutation } from '@tanstack/react-query'
import { Alert, Form, Input, Modal, Radio, Space } from 'antd'
import { reportsApi } from '../../../api/reports'
import { useReportReasons } from '../../../hooks/useCatalog'
import { feedback } from '../../../utils/feedback'

interface ReportModalProps {
  open: boolean
  onClose: () => void
  /** Báo cáo tin đăng (truyền postId) hoặc người dùng (truyền reportedUserId) — đúng một trong hai. */
  target: { postId: number } | { reportedUserId: number }
  targetName: string
}

interface ReportFormValues {
  reasonId: number
  description?: string
}

/** Form báo cáo: lý do lọc theo appliesTo (post | user). */
export function ReportModal({ open, onClose, target, targetName }: ReportModalProps) {
  const [form] = Form.useForm<ReportFormValues>()
  const appliesTo = 'postId' in target ? 'post' : 'user'
  const reasons = useReportReasons(appliesTo, open)

  const mutation = useMutation({
    mutationFn: (values: ReportFormValues) =>
      reportsApi.create({ ...target, reasonId: values.reasonId, description: values.description?.trim() || null }),
    onSuccess: (res) => {
      feedback.message.success(res.message || 'Đã gửi báo cáo. Cảm ơn bạn!')
      form.resetFields()
      onClose()
    },
  })

  return (
    <Modal
      open={open}
      title={appliesTo === 'post' ? 'Báo cáo tin đăng' : 'Báo cáo người dùng'}
      okText="Gửi báo cáo"
      cancelText="Hủy"
      okButtonProps={{ danger: true, loading: mutation.isPending }}
      onOk={() => form.submit()}
      onCancel={onClose}
      destroyOnHidden
    >
      <Space direction="vertical" style={{ width: '100%' }}>
        <Alert type="info" showIcon message={`Đối tượng: ${targetName}`} />
        <Form form={form} layout="vertical" onFinish={(v) => mutation.mutate(v)} preserve={false}>
          <Form.Item name="reasonId" label="Lý do" rules={[{ required: true, message: 'Vui lòng chọn lý do' }]}>
            <Radio.Group disabled={reasons.isLoading}>
              <Space direction="vertical">
                {(reasons.data ?? []).map((r) => (
                  <Radio key={r.reasonId} value={r.reasonId}>
                    {r.name}
                  </Radio>
                ))}
              </Space>
            </Radio.Group>
          </Form.Item>
          <Form.Item name="description" label="Mô tả thêm" rules={[{ max: 500, message: 'Tối đa 500 ký tự' }]}>
            <Input.TextArea rows={3} placeholder="Mô tả chi tiết vấn đề (không bắt buộc)" showCount maxLength={500} />
          </Form.Item>
        </Form>
      </Space>
    </Modal>
  )
}
