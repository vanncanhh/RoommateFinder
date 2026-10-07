import dayjs, { type Dayjs } from 'dayjs'
import 'dayjs/locale/vi'
import relativeTime from 'dayjs/plugin/relativeTime'
import timezone from 'dayjs/plugin/timezone'
import utc from 'dayjs/plugin/utc'

dayjs.extend(utc)
dayjs.extend(timezone)
dayjs.extend(relativeTime)
dayjs.locale('vi')

export const APP_TIMEZONE = 'Asia/Ho_Chi_Minh'

type DateInput = string | null | undefined

/** Chuyển chuỗi ISO UTC từ API sang Dayjs ở múi giờ GMT+7. */
export function toLocal(iso: string): Dayjs {
  return dayjs.utc(iso).tz(APP_TIMEZONE)
}

/** `14:05 06/10/2026` (GMT+7) */
export function formatDateTime(iso: DateInput): string {
  return iso ? toLocal(iso).format('HH:mm DD/MM/YYYY') : '—'
}

/** `06/10/2026` (GMT+7) */
export function formatDate(iso: DateInput): string {
  return iso ? toLocal(iso).format('DD/MM/YYYY') : '—'
}

/** Giờ ngắn cho tin nhắn: hôm nay → `14:05`, ngày khác → `14:05 06/10`. */
export function formatChatTime(iso: DateInput): string {
  if (!iso) return ''
  const t = toLocal(iso)
  const now = dayjs().tz(APP_TIMEZONE)
  if (t.isSame(now, 'day')) return t.format('HH:mm')
  if (t.isSame(now, 'year')) return t.format('HH:mm DD/MM')
  return t.format('HH:mm DD/MM/YYYY')
}

/** `3 giờ trước` */
export function fromNow(iso: DateInput): string {
  return iso ? dayjs.utc(iso).fromNow() : ''
}

/** DateOnly `yyyy-MM-dd` → `dd/MM/yyyy` (không đổi múi giờ). */
export function formatDateOnly(value: DateInput): string {
  return value ? dayjs(value).format('DD/MM/YYYY') : '—'
}

/** Dayjs → DateOnly `yyyy-MM-dd` để gửi API. */
export function toDateOnly(value: Dayjs | null | undefined): string | null {
  return value ? value.format('YYYY-MM-DD') : null
}

/** Ngày hôm nay theo GMT+7 (Dayjs, không giờ). */
export function todayLocal(): Dayjs {
  return dayjs(dayjs().tz(APP_TIMEZONE).format('YYYY-MM-DD'))
}

export { dayjs }
