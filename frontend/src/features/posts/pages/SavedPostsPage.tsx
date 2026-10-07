import { HeartFilled } from '@ant-design/icons'
import { useQuery } from '@tanstack/react-query'
import { Button } from 'antd'
import { postsApi } from '../../../api/posts'
import { queryKeys } from '../../../api/queryKeys'
import { PageTitle } from '../../../components/shared/PageTitle'
import { PostGrid } from '../../../components/shared/PostGrid'
import { ErrorResult, LoadingBlock } from '../../../components/shared/QueryState'
import { useSavePost } from '../hooks/usePostOwnerActions'

export default function SavedPostsPage() {
  const query = useQuery({ queryKey: queryKeys.posts.saved, queryFn: postsApi.saved })
  const savePost = useSavePost()

  return (
    <>
      <PageTitle title="Tin đã lưu" />
      {query.isLoading ? (
        <LoadingBlock />
      ) : query.isError ? (
        <ErrorResult error={query.error} onRetry={() => void query.refetch()} />
      ) : (
        <PostGrid
          posts={query.data ?? []}
          emptyText="Bạn chưa lưu tin nào"
          renderActions={(post) => [
            <Button
              key="unsave"
              type="text"
              size="small"
              icon={<HeartFilled style={{ color: '#eb2f96' }} />}
              loading={savePost.isPending && savePost.variables?.postId === post.postId}
              onClick={() => savePost.mutate({ postId: post.postId, save: false })}
            >
              Bỏ lưu
            </Button>,
          ]}
        />
      )}
    </>
  )
}
