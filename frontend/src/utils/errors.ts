import { isAxiosError } from 'axios'
import type { ApiErrorBody } from '../types'

export const GENERIC_ERROR = 'Đã có lỗi xảy ra. Vui lòng thử lại sau.'

const STATUS_FALLBACK: Record<number, string> = {
  400: 'Dữ liệu không hợp lệ.',
  401: 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.',
  403: 'Bạn không có quyền thực hiện thao tác này.',
  404: 'Không tìm thấy dữ liệu yêu cầu.',
  409: 'Thao tác không thực hiện được do trạng thái dữ liệu đã thay đổi.',
  413: 'Tệp tải lên quá lớn.',
  423: 'Tài khoản đang bị khóa tạm thời.',
  429: 'Bạn thao tác quá nhanh, vui lòng thử lại sau.',
  500: 'Lỗi hệ thống. Vui lòng thử lại sau.',
}

function isApiErrorBody(data: unknown): data is ApiErrorBody {
  return (
    typeof data === 'object' &&
    data !== null &&
    typeof (data as Record<string, unknown>).message === 'string'
  )
}

/** Mã lỗi nghiệp vụ (`code`) trong body, nếu có. */
export function getErrorCode(error: unknown): string | null {
  if (isAxiosError(error) && isApiErrorBody(error.response?.data)) {
    const code = (error.response.data as ApiErrorBody).code
    return typeof code === 'string' ? code : null
  }
  return null
}

export function getErrorStatus(error: unknown): number | null {
  return isAxiosError(error) ? (error.response?.status ?? null) : null
}

/** Thông điệp tiếng Việt để hiển thị: ưu tiên `message` từ backend, sau đó là thông điệp chung theo mã HTTP. */
export function getErrorMessage(error: unknown, fallback: string = GENERIC_ERROR): string {
  if (isAxiosError(error)) {
    const data: unknown = error.response?.data
    if (isApiErrorBody(data) && data.message.trim() !== '') return data.message
    if (!error.response) return 'Không thể kết nối tới máy chủ. Vui lòng kiểm tra mạng.'
    return STATUS_FALLBACK[error.response.status] ?? fallback
  }
  return fallback
}
