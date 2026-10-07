import axios, { AxiosHeaders, isAxiosError } from 'axios'
import { getErrorCode, getErrorMessage } from '../utils/errors'
import { tokenStorage } from '../utils/tokenStorage'

export const API_BASE_URL: string = import.meta.env.VITE_API_URL ?? ''

/** Lý do phiên đăng nhập bị kết thúc từ phía server. */
export type AuthFailureReason = 'unauthorized' | 'locked'

type AuthFailureHandler = (reason: AuthFailureReason, message: string) => void

let authFailureHandler: AuthFailureHandler | null = null

/** AuthProvider đăng ký hàm xử lý (đăng xuất + chuyển trang) khi API báo 401 / ACCOUNT_LOCKED. */
export function setAuthFailureHandler(handler: AuthFailureHandler | null): void {
  authFailureHandler = handler
}

/** Các endpoint mà 401/403 là lỗi nghiệp vụ hiển thị tại form, không phải hết phiên. */
const AUTH_FORM_ENDPOINTS = ['/api/auth/login', '/api/auth/register', '/api/auth/forgot-password', '/api/auth/reset-password']

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30_000,
  // amenityIds=1&amenityIds=3 (ASP.NET bind List<int> từ khóa lặp lại)
  paramsSerializer: { indexes: null },
})

apiClient.interceptors.request.use((config) => {
  const token = tokenStorage.get()
  if (token) {
    const headers = AxiosHeaders.from(config.headers)
    headers.set('Authorization', `Bearer ${token}`)
    config.headers = headers
  }
  return config
})

apiClient.interceptors.response.use(
  (response) => response,
  (error: unknown) => {
    if (isAxiosError(error) && error.response) {
      const url = error.config?.url ?? ''
      const isAuthForm = AUTH_FORM_ENDPOINTS.some((p) => url.startsWith(p))
      const status = error.response.status
      const hadToken = tokenStorage.get() !== null

      // Đổi mật khẩu: 401 có body là "sai mật khẩu hiện tại" (lỗi nghiệp vụ), 401 không body mới là hết phiên.
      const isBusiness401 = url.startsWith('/api/auth/change-password') && getErrorCode(error) !== null

      if (!isAuthForm && hadToken) {
        if (status === 401 && !isBusiness401) {
          tokenStorage.clear()
          authFailureHandler?.('unauthorized', 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.')
        } else if (status === 403 && getErrorCode(error) === 'ACCOUNT_LOCKED') {
          tokenStorage.clear()
          authFailureHandler?.('locked', getErrorMessage(error, 'Tài khoản của bạn đã bị khóa.'))
        }
      }
    }
    return Promise.reject(error)
  },
)

/** Thêm giá trị vào FormData, bỏ qua null/undefined/chuỗi rỗng; mảng được append lặp lại cùng tên khóa. */
export function appendFormValue(fd: FormData, key: string, value: unknown): void {
  if (value === undefined || value === null || value === '') return
  if (Array.isArray(value)) {
    value.forEach((v) => appendFormValue(fd, key, v))
    return
  }
  if (value instanceof Blob) {
    fd.append(key, value)
    return
  }
  fd.append(key, String(value))
}
