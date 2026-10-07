import { MailOutlined } from '@ant-design/icons'
import { useMutation } from '@tanstack/react-query'
import { Button, Form, Input, Result } from 'antd'
import { Link } from 'react-router-dom'
import { authApi } from '../../../api/auth'
import { emailRules } from '../../../utils/validation'
import { AuthCard } from '../components/AuthCard'

export default function ForgotPasswordPage() {
  const mutation = useMutation({ mutationFn: authApi.forgotPassword })

  return (
    <AuthCard
      title="Quên mật khẩu"
      subtitle={mutation.isSuccess ? undefined : 'Nhập email đã đăng ký, chúng tôi sẽ gửi đường dẫn đặt lại mật khẩu.'}
      footer={<Link to="/login">Quay lại đăng nhập</Link>}
    >
      {mutation.isSuccess ? (
        <Result status="success" title="Đã gửi yêu cầu" subTitle={mutation.data.message} />
      ) : (
        <Form<{ email: string }> layout="vertical" onFinish={(v) => mutation.mutate({ email: v.email.trim() })}>
          <Form.Item name="email" label="Email" rules={emailRules}>
            <Input prefix={<MailOutlined />} placeholder="email@example.com" autoComplete="email" size="large" />
          </Form.Item>
          <Button type="primary" htmlType="submit" block size="large" loading={mutation.isPending}>
            Gửi đường dẫn đặt lại
          </Button>
        </Form>
      )}
    </AuthCard>
  )
}
