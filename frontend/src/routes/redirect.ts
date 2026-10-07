/**
 * Lấy returnUrl an toàn từ query string: chỉ chấp nhận đường dẫn nội bộ (bắt đầu bằng "/" nhưng không phải "//")
 * để tránh open-redirect.
 */
export function getSafeReturnUrl(search: URLSearchParams, fallback = '/'): string {
  const value = search.get('returnUrl')
  if (!value || !value.startsWith('/') || value.startsWith('//') || value.startsWith('/login')) return fallback
  return value
}
