import { Flex, Typography } from 'antd'
import type { ReactNode } from 'react'
import { useDocumentTitle } from '../../hooks/useDocumentTitle'

interface PageTitleProps {
  title: string
  subTitle?: ReactNode
  extra?: ReactNode
}

/** Tiêu đề trang + đặt document.title. */
export function PageTitle({ title, subTitle, extra }: PageTitleProps) {
  useDocumentTitle(title)
  return (
    <Flex justify="space-between" align="center" wrap="wrap" gap={12} style={{ marginBottom: 16 }}>
      <div style={{ minWidth: 0 }}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          {title}
        </Typography.Title>
        {subTitle && <Typography.Text type="secondary">{subTitle}</Typography.Text>}
      </div>
      {extra && <div>{extra}</div>}
    </Flex>
  )
}
