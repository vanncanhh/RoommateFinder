import { useMutation, useQueryClient } from '@tanstack/react-query'
import { postsApi } from '../../../api/posts'
import { queryKeys } from '../../../api/queryKeys'
import { feedback } from '../../../utils/feedback'

/** Thao tác của chủ tin: gia hạn, đóng, xóa — dùng chung cho trang chi tiết và "Tin của tôi". */
export function usePostOwnerActions(onDeleted?: () => void) {
  const queryClient = useQueryClient()
  const invalidate = () => queryClient.invalidateQueries({ queryKey: queryKeys.posts.all })

  const extend = useMutation({
    mutationFn: postsApi.extend,
    onSuccess: () => {
      feedback.message.success('Đã gia hạn tin đăng')
      void invalidate()
    },
  })

  const close = useMutation({
    mutationFn: postsApi.close,
    onSuccess: () => {
      feedback.message.success('Đã đóng tin đăng')
      void invalidate()
    },
  })

  const remove = useMutation({
    mutationFn: postsApi.remove,
    onSuccess: () => {
      feedback.message.success('Đã xóa tin đăng')
      void invalidate()
      onDeleted?.()
    },
  })

  return { extend, close, remove }
}

/** Lưu / bỏ lưu tin. */
export function useSavePost() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ postId, save }: { postId: number; save: boolean }) =>
      save ? postsApi.save(postId) : postsApi.unsave(postId),
    onSuccess: (_data, { postId, save }) => {
      feedback.message.success(save ? 'Đã lưu tin' : 'Đã bỏ lưu tin')
      void queryClient.invalidateQueries({ queryKey: queryKeys.posts.detail(postId) })
      void queryClient.invalidateQueries({ queryKey: queryKeys.posts.saved })
    },
  })
}
