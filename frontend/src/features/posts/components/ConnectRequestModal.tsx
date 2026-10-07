import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Form, Input, Modal, Typography } from 'antd'
import { connectionsApi } from '../../../api/connections'
import { queryKeys } from '../../../api/queryKeys'
import { feedback } from '../../../utils/feedback'
import { SHORT_TEXT_MAX } from '../../../utils/validation'

interface ConnectRequestModalProps {
  open: boolean
  onClose: () => void
  postId: number
  postTitle: string
}

/** Gửi yêu cầu kết nối tới tin đăng, kèm lời nhắn giới thiệu. */
export function ConnectRequestModal({ open, onClose, postId, postTitle }: ConnectRequestModalProps) {
  const [form] = Form.useForm<{ message?: string }>()
  const queryClient = useQueryClient()

  const mutation = useMutation({
    mutationFn: (message: string | null) => connectionsApi.create({ postId, message }),
    onSuccess: () => {
      feedback.message.success('Đã gửi yêu cầu kết nối. Vui lòng chờ chủ tin phản hồi.')
      void queryClient.invalidateQueries({ queryKey: queryKeys.posts.detail(postId) })
      void queryClient.invalidateQueries({ queryKey: queryKeys.connections.all })
      onClose()
    },
  })

  return (
    <Modal
      open={open}
      title="Gửi yêu cầu kết nối"
      okText="Gửi yêu cầu"
      cancelText="Hủy"
      okButtonProps={{ loading: mutation.isPending }}
      onOk={() => form.submit()}
      onCancel={onClose}
      destroyOnHidden
    >
      <Typography.Paragraph type="secondary">
        Tin: <strong>{postTitle}</strong>
      </Typography.Paragraph>
      <Form form={form} layout="vertical" preserve={false} onFinish={(v) => mutation.mutate(v.message?.trim() || null)}>
        <Form.Item
          name="message"
          label="Lời nhắn"
          rules={[{ max: SHORT_TEXT_MAX, message: `Tối đa ${SHORT_TEXT_MAX} ký tự` }]}
        >
          <Input.TextArea
            rows={4}
            showCount
            maxLength={SHORT_TEXT_MAX}
            placeholder="Giới thiệu ngắn về bản thân: nghề nghiệp, giờ giấc sinh hoạt, thời gian dự kiến dọn vào..."
          />
        </Form.Item>
      </Form>
    </Modal>
  )
}
