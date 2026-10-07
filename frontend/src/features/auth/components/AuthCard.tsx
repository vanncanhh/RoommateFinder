import { Card, Flex, Typography } from 'antd'
import type { ReactNode } from 'react'
import { useDocumentTitle } from '../../../hooks/useDocumentTitle'

interface AuthCardProps {
  title: string
  subtitle?: ReactNode
  children: ReactNode
  footer?: ReactNode
}

/** Khung chung cho các trang đăng nhập / đăng ký / quên mật khẩu. */
export function AuthCard({ title, subtitle, children, footer }: AuthCardProps) {
  useDocumentTitle(title)
  return (
    <Flex justify="center" style={{ padding: '24px 0' }}>
      <Card style={{ width: '100%', maxWidth: 440 }}>
        <Typography.Title level={3} style={{ marginTop: 0, textAlign: 'center' }}>
          {title}
        </Typography.Title>
        {subtitle && (
          <Typography.Paragraph type="secondary" style={{ textAlign: 'center' }}>
            {subtitle}
          </Typography.Paragraph>
        )}
        {children}
        {footer && <div style={{ textAlign: 'center', marginTop: 8 }}>{footer}</div>}
      </Card>
    </Flex>
  )
}
