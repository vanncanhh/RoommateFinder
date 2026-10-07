import { Layout, Spin, Typography } from 'antd'
import { Suspense } from 'react'
import { Outlet } from 'react-router-dom'
import { AppHeader } from './AppHeader'

const CURRENT_YEAR = new Date().getFullYear()

export function PageFallback() {
  return (
    <div style={{ padding: 48, textAlign: 'center' }}>
      <Spin size="large" />
    </div>
  )
}

/** Khung trang chung: header cố định, nội dung căn giữa tối đa 1200px, footer. */
export function MainLayout() {
  return (
    <Layout style={{ minHeight: '100vh' }}>
      <AppHeader />
      <Layout.Content style={{ padding: '16px', width: '100%', maxWidth: 1232, margin: '0 auto' }}>
        <Suspense fallback={<PageFallback />}>
          <Outlet />
        </Suspense>
      </Layout.Content>
      <Layout.Footer style={{ textAlign: 'center', padding: '16px' }}>
        <Typography.Text type="secondary">
          © {CURRENT_YEAR} RoommateFinder – Tìm người ở ghép an toàn, phù hợp lối sống.
        </Typography.Text>
      </Layout.Footer>
    </Layout>
  )
}
