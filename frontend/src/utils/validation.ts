import type { Rule } from 'antd/es/form'

/**
 * Quy tắc kiểm tra phía client — phản chiếu ràng buộc trong docs/API.md và các Service backend
 * (backend vẫn là nơi kiểm tra cuối cùng).
 */

export const PASSWORD_POLICY_MESSAGE = 'Mật khẩu phải có ít nhất 8 ký tự, gồm chữ hoa, chữ thường và chữ số.'

export const passwordRules: Rule[] = [
  { required: true, message: 'Vui lòng nhập mật khẩu' },
  {
    validator: (_, value: string | undefined) => {
      if (!value) return Promise.resolve()
      const ok = value.length >= 8 && value.length <= 100 && /[A-Z]/.test(value) && /[a-z]/.test(value) && /\d/.test(value)
      return ok ? Promise.resolve() : Promise.reject(new Error(PASSWORD_POLICY_MESSAGE))
    },
  },
]

export const emailRules: Rule[] = [
  { required: true, message: 'Vui lòng nhập email' },
  { type: 'email', message: 'Email không hợp lệ' },
  { max: 100, message: 'Email tối đa 100 ký tự' },
]

export const fullNameRules: Rule[] = [
  { required: true, whitespace: true, message: 'Vui lòng nhập họ tên' },
  { min: 2, max: 100, message: 'Họ tên phải từ 2 đến 100 ký tự' },
]

export const phoneRules: Rule[] = [
  { pattern: /^0\d{9,10}$/, message: 'Số điện thoại không hợp lệ (10–11 chữ số, bắt đầu bằng 0)' },
]

/** Xác nhận mật khẩu khớp với trường `field`. */
export function confirmPasswordRules(field: string): Rule[] {
  return [
    { required: true, message: 'Vui lòng nhập lại mật khẩu' },
    ({ getFieldValue }) => ({
      validator: (_, value: string | undefined) =>
        !value || getFieldValue(field) === value
          ? Promise.resolve()
          : Promise.reject(new Error('Mật khẩu nhập lại không khớp')),
    }),
  ]
}

// Tin đăng
export const POST_TITLE_MIN = 10
export const POST_TITLE_MAX = 150
export const POST_DESCRIPTION_MAX = 2000
export const POST_ADDRESS_MAX = 255
export const POST_PRICE_MAX = 1_000_000_000
export const POST_OCCUPANTS_MAX = 20
export const POST_MAX_IMAGES = 10
export const IMAGE_MAX_SIZE_MB = 5
export const IMAGE_ACCEPT = '.jpg,.jpeg,.png,.webp,image/jpeg,image/png,image/webp'
const IMAGE_MIME = ['image/jpeg', 'image/png', 'image/webp']

/** Trả về thông điệp lỗi nếu ảnh không hợp lệ (định dạng, dung lượng), ngược lại null. */
export function validateImageFile(file: File, maxMb = IMAGE_MAX_SIZE_MB): string | null {
  if (!IMAGE_MIME.includes(file.type)) return `Tệp "${file.name}" không phải ảnh JPG, PNG hoặc WEBP.`
  if (file.size > maxMb * 1024 * 1024) return `Ảnh "${file.name}" vượt quá ${maxMb} MB.`
  return null
}

// Khác
export const CHAT_MESSAGE_MAX = 1000
export const SHORT_TEXT_MAX = 500
export const REJECT_REASON_MIN = 5
export const SUSPEND_DAYS_MAX = 365
