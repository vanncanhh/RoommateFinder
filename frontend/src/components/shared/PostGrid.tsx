import { Col, Row } from 'antd'
import type { ReactNode } from 'react'
import type { PostSummaryDto } from '../../types'
import { PostCard } from './PostCard'
import { EmptyBlock } from './QueryState'

interface PostGridProps {
  posts: PostSummaryDto[]
  emptyText?: string
  renderActions?: (post: PostSummaryDto) => ReactNode[]
}

/** Lưới thẻ tin đăng, tự xuống dòng theo độ rộng màn hình (1 cột ở 360px). */
export function PostGrid({ posts, emptyText = 'Chưa có tin đăng nào', renderActions }: PostGridProps) {
  if (posts.length === 0) return <EmptyBlock description={emptyText} />
  return (
    <Row gutter={[16, 16]}>
      {posts.map((post) => (
        <Col key={post.postId} xs={24} sm={12} lg={8} xl={6}>
          <PostCard post={post} actions={renderActions?.(post)} />
        </Col>
      ))}
    </Row>
  )
}
