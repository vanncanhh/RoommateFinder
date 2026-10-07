import { LockOutlined, MailOutlined } from '@ant-design/icons'
import { useMutation } from '@tanstack/react-query'
import { Alert, Button, Collapse, Form, Input, Typography } from 'antd'
import { Link, Navigate, useNavigate, useSearchParams } from 'react-router-dom'
import { authApi } from '../../../api/auth'
import { useAuth } from '../../../hooks/useAuth'
import { getSafeReturnUrl } from '../../../routes/redirect'
import type { LoginRequest } from '../../../types'
import { getErrorMessage } from '../../../utils/errors'
import { feedback } from '../../../utils/feedback'
import { AuthCard } from '../components/AuthCard'

const DEMO_ACCOUNTS = [
  { role: 'Admin', email: 'admin@roommate.test', password: 'Admin@123' },
  { role: 'Moderator', email: 'mod1@roommate.test', password: 'Mod@12345' },
  { role: 'Người dùng', email: 'user1@roommate.test', password: 'User@1234' },
]

export default function LoginPage() {
  const [form] = Form.useForm<LoginRequest>()
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const { login, isAuthenticated } = useAuth()
  const returnUrl = getSafeReturnUrl(searchParams)

  const mutation = useMutation({
    mutationFn: authApi.login,
    meta: { skipGlobalError: true },
    onSuccess: (res) => {
      login(res)
      feedback.message.success(`Xin chào, ${res.user.fullName}!`)
      navigate(returnUrl, { replace: true })
    },
  })

  if (isAuthenticated && !mutation.isPending && !mutation.isSuccess) return <Navigate to={returnUrl} replace />

  return (
    <AuthCard
      title="Đăng nhập"
      subtitle="Chào mừng bạn quay lại RoommateFinder"
      footer={
        <Typography.Text>
          Chưa có tài khoản? <Link to="/register">Đăng ký ngay</Link>
        </Typography.Text>
      }
    >
      {mutation.isError && (
        <Alert type="error" showIcon message={getErrorMessage(mutation.error)} style={{ marginBottom: 16 }} />
      )}
      <Form
        form={form}
        layout="vertical"
        onFinish={(v) => mutation.mutate({ email: v.email.trim(), password: v.password })}
        requiredMark={false}
      >
        <Form.Item name="email" label="Email" rules={[{ required: true, message: 'Vui lòng nhập email' }, { type: 'email', message: 'Email không hợp lệ' }]}>
          <Input prefix={<MailOutlined />} placeholder="email@example.com" autoComplete="email" size="large" />
        </Form.Item>
        <Form.Item name="password" label="Mật khẩu" rules={[{ required: true, message: 'Vui lòng nhập mật khẩu' }]}>
          <Input.Password prefix={<LockOutlined />} placeholder="Mật khẩu" autoComplete="current-password" size="large" />
        </Form.Item>
        <div style={{ textAlign: 'right', marginTop: -8, marginBottom: 16 }}>
          <Link to="/forgot-password">Quên mật khẩu?</Link>
        </div>
        <Button type="primary" htmlType="submit" block size="large" loading={mutation.isPending}>
          Đăng nhập
        </Button>
      </Form>
      {import.meta.env.DEV && (
        <Collapse
          ghost
          size="small"
          style={{ marginTop: 16 }}
          items={[
            {
              key: 'demo',
              label: 'Tài khoản demo (môi trường phát triển)',
              children: DEMO_ACCOUNTS.map((a) => (
                <Button
                  key={a.email}
                  type="link"
                  size="small"
                  style={{ display: 'block', padding: 0 }}
                  onClick={() => form.setFieldsValue({ email: a.email, password: a.password })}
                >
                  {a.role}: {a.email}
                </Button>
              )),
            },
          ]}
        />
      )}
    </AuthCard>
  )
}
