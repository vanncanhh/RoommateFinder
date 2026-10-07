import { useMutation } from '@tanstack/react-query'
import { Alert, Button, Form, Input } from 'antd'
import { useNavigate } from 'react-router-dom'
import { authApi } from '../../../api/auth'
import { useAuth } from '../../../hooks/useAuth'
import { feedback } from '../../../utils/feedback'
import { confirmPasswordRules, passwordRules } from '../../../utils/validation'

interface ChangePasswordValues {
  currentPassword: string
  newPassword: string
  confirmPassword: string
}

/** Đổi mật khẩu → thành công thì đăng xuất, yêu cầu đăng nhập lại. */
export function ChangePasswordForm() {
  const { logout } = useAuth()
  const navigate = useNavigate()

  const mutation = useMutation({
    mutationFn: authApi.changePassword,
    onSuccess: (res) => {
      feedback.message.success(res.message || 'Đổi mật khẩu thành công. Vui lòng đăng nhập lại.')
      logout()
      navigate('/login', { replace: true })
    },
  })

  return (
    <Form<ChangePasswordValues>
      layout="vertical"
      style={{ maxWidth: 420 }}
      onFinish={(v) => mutation.mutate({ currentPassword: v.currentPassword, newPassword: v.newPassword })}
    >
      <Alert type="info" showIcon message="Sau khi đổi mật khẩu, bạn sẽ cần đăng nhập lại." style={{ marginBottom: 16 }} />
      <Form.Item name="currentPassword" label="Mật khẩu hiện tại" rules={[{ required: true, message: 'Vui lòng nhập mật khẩu hiện tại' }]}>
        <Input.Password autoComplete="current-password" />
      </Form.Item>
      <Form.Item
        name="newPassword"
        label="Mật khẩu mới"
        dependencies={['currentPassword']}
        rules={[
          ...passwordRules,
          ({ getFieldValue }) => ({
            validator: (_, value: string | undefined) =>
              !value || value !== getFieldValue('currentPassword')
                ? Promise.resolve()
                : Promise.reject(new Error('Mật khẩu mới phải khác mật khẩu hiện tại')),
          }),
        ]}
        hasFeedback
      >
        <Input.Password autoComplete="new-password" />
      </Form.Item>
      <Form.Item
        name="confirmPassword"
        label="Nhập lại mật khẩu mới"
        dependencies={['newPassword']}
        rules={confirmPasswordRules('newPassword')}
        hasFeedback
      >
        <Input.Password autoComplete="new-password" />
      </Form.Item>
      <Button type="primary" htmlType="submit" loading={mutation.isPending}>
        Đổi mật khẩu
      </Button>
    </Form>
  )
}
