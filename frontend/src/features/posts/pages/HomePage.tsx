import { SearchOutlined } from '@ant-design/icons'
import { useQuery } from '@tanstack/react-query'
import { Button, Card, Col, Flex, Input, Row, Select, Space, Typography } from 'antd'
import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { postsApi } from '../../../api/posts'
import { queryKeys } from '../../../api/queryKeys'
import { PostGrid } from '../../../components/shared/PostGrid'
import { ErrorResult, LoadingBlock } from '../../../components/shared/QueryState'
import { useAreas } from '../../../hooks/useCatalog'
import { useDocumentTitle } from '../../../hooks/useDocumentTitle'
import { POST_TYPES, type PostType } from '../../../types'
import { POST_TYPE_LABELS, toOptions } from '../../../utils/labels'

const LATEST_COUNT = 8

export default function HomePage() {
  useDocumentTitle(null)
  const navigate = useNavigate()
  const areas = useAreas()
  const [keyword, setKeyword] = useState('')
  const [areaId, setAreaId] = useState<number>()
  const [postType, setPostType] = useState<PostType>()

  const latest = useQuery({ queryKey: queryKeys.posts.latest(LATEST_COUNT), queryFn: () => postsApi.latest(LATEST_COUNT) })

  const search = () => {
    const params = new URLSearchParams()
    if (keyword.trim()) params.set('keyword', keyword.trim())
    if (areaId) params.set('areaId', String(areaId))
    if (postType) params.set('postType', postType)
    navigate(`/search?${params.toString()}`)
  }

  return (
    <Space direction="vertical" size={24} style={{ width: '100%' }}>
      <Card
        style={{
          background: 'linear-gradient(135deg, #1677ff 0%, #69b1ff 100%)',
          border: 'none',
        }}
        styles={{ body: { padding: 'clamp(20px, 5vw, 48px)' } }}
      >
        <Typography.Title style={{ color: '#fff', marginTop: 0, fontSize: 'clamp(24px, 5vw, 38px)' }}>
          Tìm người ở ghép phù hợp với bạn
        </Typography.Title>
        <Typography.Paragraph style={{ color: 'rgba(255,255,255,0.9)', fontSize: 16 }}>
          Hàng trăm tin phòng ở ghép gần trường, gần công ty. Lọc theo khu vực, giá, tiện ích và lối sống.
        </Typography.Paragraph>
        <Row gutter={[8, 8]}>
          <Col xs={24} md={10}>
            <Input
              size="large"
              allowClear
              placeholder="Từ khóa: tên đường, trường học, tiêu đề..."
              prefix={<SearchOutlined />}
              value={keyword}
              onChange={(e) => setKeyword(e.target.value)}
              onPressEnter={search}
            />
          </Col>
          <Col xs={12} md={5}>
            <Select
              size="large"
              allowClear
              showSearch
              optionFilterProp="label"
              placeholder="Khu vực"
              style={{ width: '100%' }}
              loading={areas.isLoading}
              value={areaId}
              onChange={setAreaId}
              options={(areas.data ?? []).map((a) => ({ value: a.areaId, label: a.name }))}
            />
          </Col>
          <Col xs={12} md={5}>
            <Select
              size="large"
              allowClear
              placeholder="Loại tin"
              style={{ width: '100%' }}
              value={postType}
              onChange={setPostType}
              options={toOptions(POST_TYPES, POST_TYPE_LABELS)}
            />
          </Col>
          <Col xs={24} md={4}>
            <Button size="large" type="default" block icon={<SearchOutlined />} onClick={search}>
              Tìm kiếm
            </Button>
          </Col>
        </Row>
      </Card>

      <div>
        <Flex justify="space-between" align="center" style={{ marginBottom: 12 }} wrap="wrap" gap={8}>
          <Typography.Title level={4} style={{ margin: 0 }}>
            Tin mới nhất
          </Typography.Title>
          <Link to="/search">Xem tất cả →</Link>
        </Flex>
        {latest.isLoading ? (
          <LoadingBlock />
        ) : latest.isError ? (
          <ErrorResult error={latest.error} onRetry={() => void latest.refetch()} />
        ) : (
          <PostGrid posts={latest.data ?? []} emptyText="Chưa có tin đăng nào được duyệt" />
        )}
      </div>

      <Card>
        <Row gutter={[16, 16]}>
          {[
            ['1. Tìm & lọc', 'Lọc theo khu vực, giá, tiện ích, khoảng cách tới trường/công ty.'],
            ['2. Gửi yêu cầu kết nối', 'Giới thiệu bản thân với chủ tin, chờ chấp nhận để trò chuyện.'],
            ['3. Trò chuyện & đánh giá', 'Nhắn tin trực tiếp, sau khi ở ghép hãy đánh giá để cộng đồng tin cậy hơn.'],
          ].map(([title, text]) => (
            <Col xs={24} md={8} key={title}>
              <Typography.Title level={5} style={{ marginTop: 0 }}>
                {title}
              </Typography.Title>
              <Typography.Text type="secondary">{text}</Typography.Text>
            </Col>
          ))}
        </Row>
      </Card>
    </Space>
  )
}
