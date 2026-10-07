import { useMutation } from '@tanstack/react-query'
import { Button, Form, Input, Result } from 'antd'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { authApi } from '../../../api/auth'
import { confirmPasswordRules, passwordRules } from '../../../utils/validation'
import { AuthCard } from '../components/AuthCard'

interface ResetFormValues {
  newPassword: string
  confirmPassword: string
}

export default function ResetPasswordPage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const token = searchParams.get('token') ?? ''
  const mutation = useMutation({ mutationFn: authApi.resetPassword })

  if (!token) {
    return (
      <AuthCard title="Đặt lại mật khẩu">
        <Result
          status="warning"
          title="Đường dẫn không hợp lệ"
          subTitle="Đường dẫn đặt lại mật khẩu thiếu mã xác thực. Vui lòng yêu cầu lại."
          extra={<Link to="/forgot-password">Yêu cầu đường dẫn mới</Link>}
        />
      </AuthCard>
    )
  }

  if (mutation.isSuccess) {
    return (
      <AuthCard title="Đặt lại mật khẩu">
        <Result
          status="success"
          title="Đổi mật khẩu thành công"
          subTitle={mutation.data.message}
          extra={
            <Button type="primary" onClick={() => navigate('/login')}>
              Đăng nhập
            </Button>
          }
        />
      </AuthCard>
    )
  }

  return (
    <AuthCard title="Đặt lại mật khẩu" subtitle="Nhập mật khẩu mới cho tài khoản của bạn.">
      <Form<ResetFormValues> layout="vertical" onFinish={(v) => mutation.mutate({ token, newPassword: v.newPassword })}>
        <Form.Item name="newPassword" label="Mật khẩu mới" rules={passwordRules} hasFeedback>
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
        <Button type="primary" htmlType="submit" block size="large" loading={mutation.isPending}>
          Đặt lại mật khẩu
        </Button>
      </Form>
    </AuthCard>
  )
}
