import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Segmented, Tabs } from 'antd'
import { useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { connectionsApi } from '../../../api/connections'
import { queryKeys } from '../../../api/queryKeys'
import { PageTitle } from '../../../components/shared/PageTitle'
import { EmptyBlock, ErrorResult, LoadingBlock } from '../../../components/shared/QueryState'
import { useAuth } from '../../../hooks/useAuth'
import { REQUEST_STATUSES, type ConnectionRequestDto, type RequestStatus } from '../../../types'
import { feedback } from '../../../utils/feedback'
import { REQUEST_STATUS_LABELS } from '../../../utils/labels'
import { oneOf } from '../../../utils/query'
import { ReviewModal } from '../../reviews/components/ReviewModal'
import { ConnectionCard, type ConnectionDirection } from '../components/ConnectionCard'

const DIRECTIONS: ConnectionDirection[] = ['incoming', 'outgoing']

function ConnectionList({ direction, status }: { direction: ConnectionDirection; status?: RequestStatus }) {
  const queryClient = useQueryClient()
  const { user } = useAuth()
  const [reviewing, setReviewing] = useState<ConnectionRequestDto | null>(null)

  const query = useQuery({
    queryKey: direction === 'incoming' ? queryKeys.connections.incoming(status) : queryKeys.connections.outgoing(status),
    queryFn: () => (direction === 'incoming' ? connectionsApi.incoming(status) : connectionsApi.outgoing(status)),
  })

  const onDone = (text: string) => {
    feedback.message.success(text)
    void queryClient.invalidateQueries({ queryKey: queryKeys.connections.all })
    void queryClient.invalidateQueries({ queryKey: queryKeys.posts.all })
    void queryClient.invalidateQueries({ queryKey: queryKeys.conversations.all })
  }

  const accept = useMutation({ mutationFn: connectionsApi.accept, onSuccess: () => onDone('Đã chấp nhận yêu cầu') })
  const reject = useMutation({ mutationFn: connectionsApi.reject, onSuccess: () => onDone('Đã từ chối yêu cầu') })
  const cancel = useMutation({ mutationFn: connectionsApi.cancel, onSuccess: () => onDone('Đã rút lại yêu cầu') })
  const busy = accept.isPending || reject.isPending || cancel.isPending

  if (query.isLoading) return <LoadingBlock />
  if (query.isError) return <ErrorResult error={query.error} onRetry={() => void query.refetch()} />
  const items = query.data ?? []
  if (items.length === 0) {
    return (
      <EmptyBlock
        description={direction === 'incoming' ? 'Chưa có yêu cầu nào gửi tới tin của bạn' : 'Bạn chưa gửi yêu cầu nào'}
      />
    )
  }

  // Người được đánh giá: người còn lại trong kết nối.
  const reviewee = reviewing
    ? reviewing.senderId === user?.userId
      ? { id: reviewing.receiverId, name: reviewing.receiverName }
      : { id: reviewing.senderId, name: reviewing.senderName }
    : null

  return (
    <>
      {items.map((r) => (
        <ConnectionCard
          key={r.requestId}
          request={r}
          direction={direction}
          busy={busy}
          onAccept={(id) => accept.mutate(id)}
          onReject={(id) => reject.mutate(id)}
          onCancel={(id) => cancel.mutate(id)}
          onReview={setReviewing}
        />
      ))}
      {reviewing && reviewee && (
        <ReviewModal
          open
          onClose={() => setReviewing(null)}
          requestId={reviewing.requestId}
          revieweeId={reviewee.id}
          revieweeName={reviewee.name}
        />
      )}
    </>
  )
}

export default function ConnectionsPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const tab = oneOf(DIRECTIONS, searchParams.get('tab')) ?? 'incoming'
  const status = oneOf(REQUEST_STATUSES, searchParams.get('status'))

  const setParam = (next: { tab?: ConnectionDirection; status?: RequestStatus | 'all' }) => {
    const params = new URLSearchParams(searchParams)
    if (next.tab) params.set('tab', next.tab)
    if (next.status) {
      if (next.status === 'all') params.delete('status')
      else params.set('status', next.status)
    }
    setSearchParams(params, { replace: true })
  }

  const statusFilter = (
    <div style={{ overflowX: 'auto', marginBottom: 12 }}>
      <Segmented<RequestStatus | 'all'>
        value={status ?? 'all'}
        onChange={(v) => setParam({ status: v })}
        options={[
          { value: 'all', label: 'Tất cả' },
          ...REQUEST_STATUSES.map((s) => ({ value: s, label: REQUEST_STATUS_LABELS[s].label })),
        ]}
      />
    </div>
  )

  return (
    <>
      <PageTitle title="Kết nối" subTitle="Quản lý yêu cầu ở ghép bạn nhận được và đã gửi" />
      <Tabs
        activeKey={tab}
        onChange={(k) => setParam({ tab: k as ConnectionDirection })}
        items={[
          {
            key: 'incoming',
            label: 'Yêu cầu đến',
            children: (
              <>
                {statusFilter}
                <ConnectionList direction="incoming" status={status} />
              </>
            ),
          },
          {
            key: 'outgoing',
            label: 'Yêu cầu đã gửi',
            children: (
              <>
                {statusFilter}
                <ConnectionList direction="outgoing" status={status} />
              </>
            ),
          },
        ]}
      />
    </>
  )
}
