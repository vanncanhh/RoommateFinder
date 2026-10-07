import { UserOutlined } from '@ant-design/icons'
import { Avatar } from 'antd'
import { resolveFileUrl } from '../../utils/format'

interface UserAvatarProps {
  url: string | null | undefined
  name?: string
  size?: number | 'small' | 'default' | 'large'
}

export function UserAvatar({ url, name, size = 'default' }: UserAvatarProps) {
  const src = resolveFileUrl(url)
  const initial = name?.trim().charAt(0).toUpperCase()
  return (
    <Avatar
      size={size}
      src={src}
      icon={!src && !initial ? <UserOutlined /> : undefined}
      alt={name}
      style={src ? undefined : { backgroundColor: '#1677ff', flexShrink: 0 }}
    >
      {!src ? initial : null}
    </Avatar>
  )
}
