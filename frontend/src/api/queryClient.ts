import { MutationCache, QueryClient } from '@tanstack/react-query'
import { getErrorCode, getErrorMessage, getErrorStatus } from '../utils/errors'
import { feedback } from '../utils/feedback'

interface AppMutationMeta extends Record<string, unknown> {
  /** Bỏ qua thông báo lỗi chung (khi component tự hiển thị lỗi). */
  skipGlobalError?: boolean
}

declare module '@tanstack/react-query' {
  interface Register {
    mutationMeta: AppMutationMeta
  }
}

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      refetchOnWindowFocus: false,
      // Không thử lại lỗi 4xx (dữ liệu không tồn tại / không có quyền).
      retry: (failureCount, error) => {
        const status = getErrorStatus(error)
        if (status !== null && status >= 400 && status < 500) return false
        return failureCount < 1
      },
    },
  },
  mutationCache: new MutationCache({
    onError: (error, _variables, _context, mutation) => {
      if (mutation.meta?.skipGlobalError) return
      // 401 / ACCOUNT_LOCKED đã được xử lý ở interceptor (đăng xuất + chuyển trang).
      if (getErrorStatus(error) === 401 || getErrorCode(error) === 'ACCOUNT_LOCKED') return
      feedback.message.error(getErrorMessage(error))
    },
  }),
})
