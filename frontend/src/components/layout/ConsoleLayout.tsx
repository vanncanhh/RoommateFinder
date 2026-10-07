import { Card, Menu, type MenuProps } from 'antd'
import { Suspense, type ReactNode } from 'react'
import { Link, Outlet, useLocation } from 'react-router-dom'
import { PageFallback } from './MainLayout'

export interface ConsoleNavItem {
  path: string
  label: string
  icon: ReactNode
}

interface ConsoleLayoutProps {
  items: ConsoleNavItem[]
}

/** Khung cho khu vực Kiểm duyệt / Quản trị: menu ngang (tự thu gọn khi hẹp) + nội dung. */
export function ConsoleLayout({ items }: ConsoleLayoutProps) {
  const location = useLocation()
  const menuItems: MenuProps['items'] = items.map((i) => ({
    key: i.path,
    icon: i.icon,
    label: <Link to={i.path}>{i.label}</Link>,
  }))
  const selected = items.find((i) => location.pathname.startsWith(i.path))?.path

  return (
    <>
      <Card styles={{ body: { padding: '0 8px' } }} style={{ marginBottom: 16 }}>
        <Menu mode="horizontal" items={menuItems} selectedKeys={selected ? [selected] : []} style={{ borderBottom: 'none' }} />
      </Card>
      <Suspense fallback={<PageFallback />}>
        <Outlet />
      </Suspense>
    </>
  )
}
