import {
  AuditOutlined,
  CrownOutlined,
  FileTextOutlined,
  HeartOutlined,
  LogoutOutlined,
  MenuOutlined,
  MessageOutlined,
  PlusCircleOutlined,
  SearchOutlined,
  TeamOutlined,
  UserOutlined,
} from '@ant-design/icons'
import { Button, Drawer, Dropdown, Flex, Grid, Layout, Menu, Space, Typography, type MenuProps } from 'antd'
import { useState, type ReactNode } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { NotificationBell } from '../../features/notifications/components/NotificationBell'
import { useAuth } from '../../hooks/useAuth'
import { feedback } from '../../utils/feedback'
import { UserAvatar } from '../shared/UserAvatar'

interface NavItem {
  key: string
  label: string
  icon: ReactNode
  requiresAuth: boolean
}

const NAV_ITEMS: NavItem[] = [
  { key: '/search', label: 'Tìm phòng', icon: <SearchOutlined />, requiresAuth: false },
  { key: '/posts/new', label: 'Đăng tin', icon: <PlusCircleOutlined />, requiresAuth: false },
  { key: '/my-posts', label: 'Tin của tôi', icon: <FileTextOutlined />, requiresAuth: true },
  { key: '/connections', label: 'Kết nối', icon: <TeamOutlined />, requiresAuth: true },
  { key: '/chat', label: 'Tin nhắn', icon: <MessageOutlined />, requiresAuth: true },
]

function Logo({ compact = false }: { compact?: boolean }) {
  return (
    <Link
      to="/"
      aria-label="RoommateFinder – Trang chủ"
      style={{ display: 'flex', alignItems: 'center', gap: 8, flexShrink: 0 }}
    >
      <img src="/favicon.svg" alt="" width={28} height={28} />
      {!compact && (
        <Typography.Text strong style={{ fontSize: 18, color: '#1677ff', whiteSpace: 'nowrap' }}>
          RoommateFinder
        </Typography.Text>
      )}
    </Link>
  )
}

/** Header: logo, menu chính, chuông thông báo, menu tài khoản; điện thoại → menu trong Drawer. */
export function AppHeader() {
  const { user, isAuthenticated, isModerator, isAdmin, logout } = useAuth()
  const screens = Grid.useBreakpoint()
  const isDesktop = Boolean(screens.lg)
  const location = useLocation()
  const navigate = useNavigate()
  const [drawerOpen, setDrawerOpen] = useState(false)

  const navItems = NAV_ITEMS.filter((i) => isAuthenticated || !i.requiresAuth).map((i) => ({
    key: i.key,
    icon: i.icon,
    label: <Link to={i.key}>{i.label}</Link>,
  }))
  const selectedKey = NAV_ITEMS.find((i) => location.pathname === i.key || location.pathname.startsWith(`${i.key}/`))?.key

  const handleLogout = () => {
    logout()
    feedback.message.success('Đã đăng xuất')
    navigate('/')
  }

  const accountItems: MenuProps['items'] = [
    { key: '/profile', icon: <UserOutlined />, label: <Link to="/profile">Hồ sơ</Link> },
    { key: '/saved', icon: <HeartOutlined />, label: <Link to="/saved">Tin đã lưu</Link> },
    ...(isModerator
      ? [{ key: '/moderator', icon: <AuditOutlined />, label: <Link to="/moderator">Kiểm duyệt</Link> }]
      : []),
    ...(isAdmin ? [{ key: '/admin', icon: <CrownOutlined />, label: <Link to="/admin">Quản trị</Link> }] : []),
    { type: 'divider' as const },
    { key: 'logout', icon: <LogoutOutlined />, label: 'Đăng xuất', danger: true, onClick: handleLogout },
  ]

  const loginUrl = `/login?returnUrl=${encodeURIComponent(location.pathname + location.search)}`

  return (
    <Layout.Header
      style={{
        position: 'sticky',
        top: 0,
        zIndex: 100,
        background: '#fff',
        borderBottom: '1px solid #f0f0f0',
        padding: '0 16px',
        height: 64,
        lineHeight: '64px',
      }}
    >
      <Flex align="center" gap={screens.sm ? 16 : 8} style={{ maxWidth: 1200, margin: '0 auto', height: '100%' }}>
        {!isDesktop && (
          <Button type="text" icon={<MenuOutlined />} aria-label="Mở menu" onClick={() => setDrawerOpen(true)} />
        )}
        <Logo compact={!screens.sm} />
        {isDesktop ? (
          <Menu
            mode="horizontal"
            items={navItems}
            selectedKeys={selectedKey ? [selectedKey] : []}
            style={{ flex: 1, minWidth: 0, borderBottom: 'none' }}
          />
        ) : (
          <div style={{ flex: 1 }} />
        )}
        {isAuthenticated && user ? (
          <Space size={8}>
            <NotificationBell />
            <Dropdown menu={{ items: accountItems }} trigger={['click']} placement="bottomRight">
              <Button type="text" style={{ height: 40, padding: '0 4px' }} aria-label="Tài khoản">
                <Space size={6}>
                  <UserAvatar url={user.avatarUrl} name={user.fullName} size="small" />
                  {isDesktop && (
                    <Typography.Text ellipsis style={{ maxWidth: 140 }}>
                      {user.fullName}
                    </Typography.Text>
                  )}
                </Space>
              </Button>
            </Dropdown>
          </Space>
        ) : (
          <Space size={8}>
            <Button type="primary" onClick={() => navigate(loginUrl)}>
              Đăng nhập
            </Button>
            {isDesktop && <Button onClick={() => navigate('/register')}>Đăng ký</Button>}
          </Space>
        )}
      </Flex>

      <Drawer
        title={<Logo />}
        placement="left"
        open={drawerOpen}
        onClose={() => setDrawerOpen(false)}
        width={280}
        styles={{ body: { padding: 0 } }}
      >
        <Menu
          mode="inline"
          items={[
            ...navItems,
            ...(isAuthenticated
              ? []
              : [{ key: '/register', icon: <UserOutlined />, label: <Link to="/register">Đăng ký</Link> }]),
          ]}
          selectedKeys={selectedKey ? [selectedKey] : []}
          onClick={() => setDrawerOpen(false)}
          style={{ borderInlineEnd: 'none' }}
        />
      </Drawer>
    </Layout.Header>
  )
}
