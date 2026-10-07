import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { Card, Col, DatePicker, Row, Statistic, Table, Typography } from 'antd'
import type { Dayjs } from 'dayjs'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import { adminApi } from '../../../api/admin'
import { queryKeys } from '../../../api/queryKeys'
import { PageTitle } from '../../../components/shared/PageTitle'
import { ErrorResult, LoadingBlock } from '../../../components/shared/QueryState'
import type { AreaCountDto, DashboardQuery } from '../../../types'
import { formatDateOnly, toDateOnly, todayLocal } from '../../../utils/datetime'
import { formatPercent } from '../../../utils/format'
import { DailyBarChart } from '../components/DailyBarChart'

type Range = [Dayjs, Dayjs]

function KpiCard({ title, value, suffix, link }: { title: string; value: string | number; suffix?: string; link?: string }) {
  const content = <Statistic title={title} value={value} suffix={suffix} />
  return <Card size="small">{link ? <Link to={link}>{content}</Link> : content}</Card>
}

export default function DashboardPage() {
  const [range, setRange] = useState<Range>(() => [todayLocal().subtract(29, 'day'), todayLocal()])
  const params: DashboardQuery = { from: toDateOnly(range[0]) ?? undefined, to: toDateOnly(range[1]) ?? undefined }

  const query = useQuery({
    queryKey: queryKeys.admin.dashboard(params),
    queryFn: () => adminApi.getDashboard(params),
    placeholderData: keepPreviousData,
  })
  const d = query.data

  return (
    <>
      <PageTitle
        title="Thống kê"
        subTitle={d ? `Từ ${formatDateOnly(d.from)} đến ${formatDateOnly(d.to)}` : undefined}
        extra={
          <DatePicker.RangePicker
            value={range}
            format="DD/MM/YYYY"
            allowClear={false}
            disabledDate={(day) => day.isAfter(todayLocal(), 'day')}
            onChange={(v) => {
              if (v && v[0] && v[1]) setRange([v[0], v[1]])
            }}
            presets={[
              { label: '7 ngày', value: [todayLocal().subtract(6, 'day'), todayLocal()] },
              { label: '30 ngày', value: [todayLocal().subtract(29, 'day'), todayLocal()] },
              { label: '90 ngày', value: [todayLocal().subtract(89, 'day'), todayLocal()] },
            ]}
          />
        }
      />
      {query.isLoading ? (
        <LoadingBlock rows={12} />
      ) : query.isError || !d ? (
        <ErrorResult error={query.error} onRetry={() => void query.refetch()} />
      ) : (
        <Row gutter={[16, 16]}>
          <Col xs={12} md={6}>
            <KpiCard title="Người dùng mới" value={d.newUsers} />
          </Col>
          <Col xs={12} md={6}>
            <KpiCard title="Tin có phòng mới" value={d.newPostsHasRoom} />
          </Col>
          <Col xs={12} md={6}>
            <KpiCard title="Tin tìm phòng mới" value={d.newPostsSeeking} />
          </Col>
          <Col xs={12} md={6}>
            <KpiCard title="Kết nối thành công" value={d.acceptedConnections} />
          </Col>
          <Col xs={12} md={6}>
            <KpiCard title="Tin đã duyệt / từ chối" value={`${d.approvedCount} / ${d.rejectedCount}`} />
          </Col>
          <Col xs={12} md={6}>
            <KpiCard title="Tỉ lệ duyệt" value={formatPercent(d.approvalRate)} />
          </Col>
          <Col xs={12} md={6}>
            <KpiCard
              title="Thời gian duyệt TB"
              value={d.avgModerationHours === null ? '—' : d.avgModerationHours.toLocaleString('vi-VN', { maximumFractionDigits: 1 })}
              suffix={d.avgModerationHours === null ? undefined : 'giờ'}
            />
          </Col>
          <Col xs={12} md={6}>
            <KpiCard title="Tin đang hiển thị" value={d.activePosts} />
          </Col>
          <Col xs={12} md={8}>
            <KpiCard title="Tin chờ duyệt (hiện tại)" value={d.pendingPosts} link="/moderator/posts" />
          </Col>
          <Col xs={12} md={8}>
            <KpiCard title="Báo cáo chờ xử lý" value={d.pendingReports} link="/moderator/reports" />
          </Col>
          <Col xs={24} md={8}>
            <KpiCard title="Tổng người dùng" value={d.totalUsers} />
          </Col>

          <Col xs={24} lg={12}>
            <Card title="Tin đăng mới theo ngày" size="small">
              <DailyBarChart data={d.dailyPosts} unit="tin" color="#1677ff" />
            </Card>
          </Col>
          <Col xs={24} lg={12}>
            <Card title="Người dùng mới theo ngày" size="small">
              <DailyBarChart data={d.dailyUsers} unit="người dùng" color="#13a8a8" />
            </Card>
          </Col>
          <Col xs={24}>
            <Card title="Khu vực nhiều tin nhất" size="small">
              <Table<AreaCountDto>
                rowKey="areaName"
                size="small"
                pagination={false}
                dataSource={d.topAreas}
                locale={{ emptyText: 'Không có dữ liệu' }}
                columns={[
                  { title: '#', key: 'rank', width: 48, render: (_, __, i) => i + 1 },
                  { title: 'Khu vực', dataIndex: 'areaName' },
                  {
                    title: 'Số tin',
                    dataIndex: 'postCount',
                    width: 100,
                    align: 'right',
                    render: (v: number) => <Typography.Text strong>{v}</Typography.Text>,
                  },
                ]}
              />
            </Card>
          </Col>
        </Row>
      )}
    </>
  )
}
