const numberFormatter = new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 0 })

/** 3500000 → `3.500.000 đ` */
export function formatMoney(value: number | null | undefined): string {
  if (value === null || value === undefined || Number.isNaN(value)) return '—'
  return `${numberFormatter.format(value)} đ`
}

/** Dùng cho InputNumber: 3500000 → `3.500.000` */
export function formatNumberInput(value: number | string | undefined): string {
  if (value === undefined || value === '') return ''
  return String(value).replace(/\B(?=(\d{3})+(?!\d))/g, '.')
}

/**
 * Ngược lại với formatNumberInput: `3.500.000` → 3500000.
 * Ô trống (không có chữ số) trả về chuỗi rỗng: rc-input-number coi đó là giá trị "empty" và gọi onChange(null)
 * (trả 0 thì xóa ô sẽ thành 0; trả NaN thì InputNumber bỏ qua và giữ nguyên giá trị cũ).
 */
export function parseNumberInput(value: string | undefined): number {
  const digits = (value ?? '').replace(/[^\d]/g, '')
  return (digits === '' ? '' : Number(digits)) as number
}

export function formatDistance(km: number | null | undefined): string | null {
  if (km === null || km === undefined) return null
  if (km < 1) return `${Math.round(km * 1000)} m`
  return `${km.toLocaleString('vi-VN', { maximumFractionDigits: 1 })} km`
}

export function formatRating(rating: number | null | undefined): string {
  return (rating ?? 0).toLocaleString('vi-VN', { minimumFractionDigits: 1, maximumFractionDigits: 1 })
}

export function formatPercent(value: number | null | undefined): string {
  if (value === null || value === undefined) return '—'
  // DashboardDto.approvalRate là phần trăm 0–100 (ví dụ 90.9).
  return `${value.toLocaleString('vi-VN', { maximumFractionDigits: 1 })}%`
}

const API_BASE = import.meta.env.VITE_API_URL ?? ''

/** URL ảnh trong DTO là đường dẫn tương đối `/uploads/...` → ghép với VITE_API_URL nếu có. */
export function resolveFileUrl(path: string | null | undefined): string | undefined {
  if (!path) return undefined
  if (/^(https?:|data:|blob:)/i.test(path)) return path
  return `${API_BASE}${path.startsWith('/') ? '' : '/'}${path}`
}
