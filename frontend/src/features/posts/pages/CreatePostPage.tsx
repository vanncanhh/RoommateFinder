import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { postsApi } from '../../../api/posts'
import { queryKeys } from '../../../api/queryKeys'
import { PageTitle } from '../../../components/shared/PageTitle'
import type { PostFormData } from '../../../types'
import { feedback } from '../../../utils/feedback'
import { PostForm } from '../components/PostForm'

export default function CreatePostPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const mutation = useMutation({
    mutationFn: ({ data, images }: { data: PostFormData; images: File[] }) => postsApi.create(data, images),
    onSuccess: (post) => {
      feedback.message.success('Đăng tin thành công! Tin đang chờ kiểm duyệt.')
      void queryClient.invalidateQueries({ queryKey: queryKeys.posts.all })
      navigate(`/posts/${post.postId}`)
    },
  })

  return (
    <div style={{ maxWidth: 900, margin: '0 auto' }}>
      <PageTitle title="Đăng tin mới" subTitle="Tin đăng sẽ được kiểm duyệt trước khi hiển thị công khai." />
      <PostForm submitting={mutation.isPending} onSubmit={(data, images) => mutation.mutate({ data, images })} />
    </div>
  )
}
