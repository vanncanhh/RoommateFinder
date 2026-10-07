import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Button, Result } from 'antd'
import { useNavigate, useParams } from 'react-router-dom'
import { postsApi } from '../../../api/posts'
import { queryKeys } from '../../../api/queryKeys'
import { PageTitle } from '../../../components/shared/PageTitle'
import { ErrorResult, LoadingBlock } from '../../../components/shared/QueryState'
import type { PostFormData } from '../../../types'
import { feedback } from '../../../utils/feedback'
import { POST_STATUS_LABELS } from '../../../utils/labels'
import { toNumber } from '../../../utils/query'
import { PostForm } from '../components/PostForm'
import { canEditPost } from '../postRules'

export default function EditPostPage() {
  const { id } = useParams()
  const postId = toNumber(id) ?? 0
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const query = useQuery({
    queryKey: queryKeys.posts.detail(postId),
    queryFn: () => postsApi.get(postId),
    enabled: postId > 0,
  })

  const mutation = useMutation({
    mutationFn: ({ data, images }: { data: PostFormData; images: File[] }) => postsApi.update(postId, data, images),
    onSuccess: (post) => {
      feedback.message.success('Đã cập nhật tin. Tin sẽ được kiểm duyệt lại.')
      queryClient.setQueryData(queryKeys.posts.detail(postId), post)
      void queryClient.invalidateQueries({ queryKey: queryKeys.posts.all })
      navigate(`/posts/${postId}`)
    },
  })

  if (query.isLoading) return <LoadingBlock rows={12} />
  if (query.isError || !query.data) return <ErrorResult error={query.error} onRetry={() => void query.refetch()} />
  if (!query.data.isOwner) {
    return <Result status="403" title="Không có quyền" subTitle="Bạn chỉ có thể sửa tin đăng của chính mình." />
  }
  if (!canEditPost(query.data)) {
    return (
      <Result
        status="warning"
        title="Không thể sửa tin đăng này"
        subTitle={`Tin đang ở trạng thái "${POST_STATUS_LABELS[query.data.status].label}". Chỉ sửa được tin đang chờ duyệt, đã duyệt hoặc bị từ chối.`}
        extra={
          <Button type="primary" onClick={() => navigate(`/posts/${postId}`)}>
            Xem tin đăng
          </Button>
        }
      />
    )
  }

  return (
    <div style={{ maxWidth: 900, margin: '0 auto' }}>
      <PageTitle title="Sửa tin đăng" subTitle={query.data.title} />
      <PostForm
        key={query.data.postId}
        initial={query.data}
        submitting={mutation.isPending}
        onSubmit={(data, images) => mutation.mutate({ data, images })}
      />
    </div>
  )
}
