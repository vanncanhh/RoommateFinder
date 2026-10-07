import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Form, Input, Modal, Rate, Typography } from 'antd'
import { queryKeys } from '../../../api/queryKeys'
import { reviewsApi } from '../../../api/reviews'
import { feedback } from '../../../utils/feedback'

interface ReviewModalProps {
  open: boolean
  onClose: () => void
  requestId: number
  revieweeId: number
  revieweeName: string
}

interface ReviewFormValues {
  rating: number
  comment?: string
}

const RATING_TEXTS = ['Rất tệ', 'Tệ', 'Bình thường', 'Tốt', 'Rất tốt']

/** Đánh giá người ở ghép sau khi kết nối được chấp nhận (chỉ mở khi canReview). */
export function ReviewModal({ open, onClose, requestId, revieweeId, revieweeName }: ReviewModalProps) {
  const [form] = Form.useForm<ReviewFormValues>()
  const queryClient = useQueryClient()

  const mutation = useMutation({
    mutationFn: (values: ReviewFormValues) =>
      reviewsApi.create({ requestId, rating: values.rating, comment: values.comment?.trim() || null }),
    onSuccess: () => {
      feedback.message.success('Cảm ơn bạn đã đánh giá!')
      void queryClient.invalidateQueries({ queryKey: queryKeys.connections.all })
      void queryClient.invalidateQueries({ queryKey: queryKeys.users.detail(revieweeId) })
      onClose()
    },
  })

  return (
    <Modal
      open={open}
      title="Đánh giá người ở ghép"
      okText="Gửi đánh giá"
      cancelText="Hủy"
      okButtonProps={{ loading: mutation.isPending }}
      onOk={() => form.submit()}
      onCancel={onClose}
      destroyOnHidden
    >
      <Typography.Paragraph>
        Bạn đánh giá thế nào về <strong>{revieweeName}</strong>?
      </Typography.Paragraph>
      <Form form={form} layout="vertical" onFinish={(v) => mutation.mutate(v)} preserve={false}>
        <Form.Item name="rating" label="Số sao" rules={[{ required: true, message: 'Vui lòng chọn số sao (1–5)' }]}>
          <Rate tooltips={RATING_TEXTS} />
        </Form.Item>
        <Form.Item name="comment" label="Nhận xét" rules={[{ max: 500, message: 'Tối đa 500 ký tự' }]}>
          <Input.TextArea rows={4} placeholder="Chia sẻ trải nghiệm ở ghép của bạn" showCount maxLength={500} />
        </Form.Item>
      </Form>
    </Modal>
  )
}
