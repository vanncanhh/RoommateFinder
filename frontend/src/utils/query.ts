/** Chuyển giá trị query string sang số, trả undefined nếu rỗng/không hợp lệ. */
export function toNumber(value: string | null | undefined): number | undefined {
  if (value === null || value === undefined || value.trim() === '') return undefined
  const n = Number(value)
  return Number.isFinite(n) ? n : undefined
}

/** Kiểm tra giá trị có thuộc tập giá trị liệt kê không. */
export function oneOf<T extends string>(values: readonly T[], value: string | null | undefined): T | undefined {
  return value !== null && value !== undefined && (values as readonly string[]).includes(value) ? (value as T) : undefined
}

/** Bỏ các khóa có giá trị rỗng để không gửi lên API. */
export function compact<T extends object>(obj: T): Partial<T> {
  const result: Partial<T> = {}
  for (const key of Object.keys(obj) as (keyof T)[]) {
    const v = obj[key]
    if (v === undefined || v === null || v === '') continue
    if (Array.isArray(v) && v.length === 0) continue
    result[key] = v
  }
  return result
}
