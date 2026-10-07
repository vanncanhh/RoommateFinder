import { Button, Empty, Result, Skeleton } from 'antd'
import type { ReactNode } from 'react'
import { getErrorMessage, getErrorStatus } from '../../utils/errors'

interface ErrorResultProps {
  error: unknown
  onRetry?: () => void
  title?: string
}

/** Hiển thị lỗi khi tải dữ liệu (message tiếng Việt từ API). */
export function ErrorResult({ error, onRetry, title }: ErrorResultProps) {
  const status = getErrorStatus(error)
  const resultStatus = status === 404 ? '404' : status === 403 ? '403' : 'error'
  return (
    <Result
      status={resultStatus}
      title={title ?? (status === 404 ? 'Không tìm thấy' : status === 403 ? 'Không có quyền truy cập' : 'Không tải được dữ liệu')}
      subTitle={getErrorMessage(error)}
      extra={
        onRetry ? (
          <Button type="primary" onClick={onRetry}>
            Thử lại
          </Button>
        ) : undefined
      }
    />
  )
}

/** Skeleton khi đang tải. */
export function LoadingBlock({ rows = 6 }: { rows?: number }) {
  return <Skeleton active paragraph={{ rows }} />
}

export function EmptyBlock({ description, children }: { description: ReactNode; children?: ReactNode }) {
  return (
    <Empty description={description} style={{ padding: '32px 0' }}>
      {children}
    </Empty>
  )
}
