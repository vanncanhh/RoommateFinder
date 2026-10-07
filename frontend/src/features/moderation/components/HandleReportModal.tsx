import { Alert, Descriptions, Form, Input, InputNumber, Modal, Select } from 'antd'
import { REPORT_DECISIONS, type HandleReportRequest, type ReportDecision, type ReportDto } from '../../../types'
import { REPORT_DECISION_LABELS } from '../../../utils/labels'
import { SHORT_TEXT_MAX, SUSPEND_DAYS_MAX } from '../../../utils/validation'

interface HandleReportModalProps {
  report: ReportDto | null
  loading: boolean
  onCancel: () => void
  onSubmit: (body: HandleReportRequest) => void
}

interface HandleFormValues {
  decision: ReportDecision
  suspendDays?: number
  note?: string
}

const DECISION_HINTS: Record<ReportDecision, string> = {
  dismiss: 'Báo cáo không có cơ sở, không xử lý người bị báo cáo.',
  warning: 'Ghi nhận vi phạm mức nhẹ và gửi cảnh cáo tới người vi phạm.',
  hide_post: 'Ẩn tin đăng bị báo cáo (vi phạm mức trung bình).',
  suspend: 'Tạm khóa tài khoản người vi phạm trong số ngày chỉ định.',
  ban: 'Cấm vĩnh viễn tài khoản người vi phạm (mức nghiêm trọng).',
}

/** Xử lý báo cáo: bác bỏ / cảnh cáo / ẩn tin (chỉ báo cáo tin) / tạm khóa (cần số ngày) / cấm. */
export function HandleReportModal({ report, loading, onCancel, onSubmit }: HandleReportModalProps) {
  const [form] = Form.useForm<HandleFormValues>()
  const decision = Form.useWatch('decision', form)
  const isPostReport = report?.postId !== null && report?.postId !== undefined

  const options = REPORT_DECISIONS.filter((d) => d !== 'hide_post' || isPostReport).map((d) => ({
    value: d,
    label: REPORT_DECISION_LABELS[d],
  }))

  return (
    <Modal
      open={report !== null}
      title="Xử lý báo cáo"
      okText="Xác nhận xử lý"
      cancelText="Hủy"
      okButtonProps={{ loading, danger: decision === 'ban' || decision === 'suspend' }}
      onOk={() => form.submit()}
      onCancel={onCancel}
      destroyOnHidden
    >
      {report && (
        <>
          <Descriptions column={1} size="small" bordered style={{ marginBottom: 16 }}>
            <Descriptions.Item label="Đối tượng">
              {report.postId ? `Tin: ${report.postTitle ?? `#${report.postId}`}` : `Người dùng: ${report.reportedUserName ?? ''}`}
            </Descriptions.Item>
            <Descriptions.Item label="Người chịu trách nhiệm">{report.targetUserName}</Descriptions.Item>
            <Descriptions.Item label="Lý do">{report.reasonName}</Descriptions.Item>
            {report.description && <Descriptions.Item label="Mô tả">{report.description}</Descriptions.Item>}
            <Descriptions.Item label="Báo cáo chờ xử lý về đối tượng">{report.pendingReportsOnTarget}</Descriptions.Item>
          </Descriptions>
          <Form
            form={form}
            layout="vertical"
            preserve={false}
            initialValues={{ decision: 'warning' }}
            onFinish={(v) =>
              onSubmit({
                decision: v.decision,
                suspendDays: v.decision === 'suspend' ? (v.suspendDays ?? null) : null,
                note: v.note?.trim() || null,
              })
            }
          >
            <Form.Item name="decision" label="Quyết định" rules={[{ required: true, message: 'Chọn quyết định' }]}>
              <Select options={options} />
            </Form.Item>
            {decision && <Alert type="info" showIcon message={DECISION_HINTS[decision]} style={{ marginBottom: 16 }} />}
            {decision === 'suspend' && (
              <Form.Item
                name="suspendDays"
                label="Số ngày tạm khóa"
                rules={[{ required: true, message: 'Nhập số ngày tạm khóa' }]}
              >
                <InputNumber min={1} max={SUSPEND_DAYS_MAX} precision={0} style={{ width: '100%' }} suffix="ngày" />
              </Form.Item>
            )}
            <Form.Item name="note" label="Ghi chú" rules={[{ max: SHORT_TEXT_MAX, message: `Tối đa ${SHORT_TEXT_MAX} ký tự` }]}>
              <Input.TextArea rows={3} showCount maxLength={SHORT_TEXT_MAX} />
            </Form.Item>
          </Form>
        </>
      )}
    </Modal>
  )
}
