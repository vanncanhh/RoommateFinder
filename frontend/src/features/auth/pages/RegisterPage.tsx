import { useMutation } from '@tanstack/react-query'
import { Button, Form, Input, Result, Typography } from 'antd'
import { Link, useNavigate } from 'react-router-dom'
import { authApi } from '../../../api/auth'
import { confirmPasswordRules, emailRules, fullNameRules, passwordRules, phoneRules } from '../../../utils/validation'
import { AuthCard } from '../components/AuthCard'

interface RegisterFormValues {
  fullName: string
  email: string
  phone?: string
  password: string
  confirmPassword: string
}

export default function RegisterPage() {
  const navigate = useNavigate()
  const mutation = useMutation({ mutationFn: authApi.register })

  if (mutation.isSuccess) {
    return (
      <AuthCard title="Đăng ký thành công">
        <Result
          status="success"
          title="Tạo tài khoản thành công"
          subTitle={mutation.data.message}
          extra={
            <Button type="primary" onClick={() => navigate('/login')}>
              Đăng nhập ngay
            </Button>
          }
        />
      </AuthCard>
    )
  }

  const onFinish = (v: RegisterFormValues) =>
    mutation.mutate({
      fullName: v.fullName.trim(),
      email: v.email.trim(),
      password: v.password,
      phone: v.phone?.trim() || null,
    })

  return (
    <AuthCard
      title="Đăng ký tài khoản"
      subtitle="Tìm người ở ghép phù hợp với lối sống của bạn"
      footer={
        <Typography.Text>
          Đã có tài khoản? <Link to="/login">Đăng nhập</Link>
        </Typography.Text>
      }
    >
      <Form<RegisterFormValues> layout="vertical" onFinish={onFinish}>
        <Form.Item name="fullName" label="Họ và tên" rules={fullNameRules}>
          <Input placeholder="Nguyễn Văn A" autoComplete="name" maxLength={100} />
        </Form.Item>
        <Form.Item name="email" label="Email" rules={emailRules}>
          <Input placeholder="email@example.com" autoComplete="email" />
        </Form.Item>
        <Form.Item name="phone" label="Số điện thoại" rules={phoneRules} extra="Không bắt buộc. Chỉ hiển thị cho người đã được bạn chấp nhận kết nối.">
          <Input placeholder="0912345678" autoComplete="tel" inputMode="tel" maxLength={11} />
        </Form.Item>
        <Form.Item name="password" label="Mật khẩu" rules={passwordRules} hasFeedback>
          <Input.Password autoComplete="new-password" />
        </Form.Item>
        <Form.Item
          name="confirmPassword"
          label="Nhập lại mật khẩu"
          dependencies={['password']}
          rules={confirmPasswordRules('password')}
          hasFeedback
        >
          <Input.Password autoComplete="new-password" />
        </Form.Item>
        <Button type="primary" htmlType="submit" block size="large" loading={mutation.isPending}>
          Đăng ký
        </Button>
      </Form>
    </AuthCard>
  )
}
