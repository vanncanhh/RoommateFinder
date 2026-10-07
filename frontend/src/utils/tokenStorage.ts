const TOKEN_KEY = 'rf_access_token'

function safeGet(storage: Storage, key: string): string | null {
  try {
    return storage.getItem(key)
  } catch {
    return null
  }
}

function safeSet(storage: Storage, key: string, value: string | null): void {
  try {
    if (value === null) storage.removeItem(key)
    else storage.setItem(key, value)
  } catch {
    // Trình duyệt chặn storage (chế độ riêng tư...) — bỏ qua.
  }
}

export const tokenStorage = {
  get: (): string | null => safeGet(localStorage, TOKEN_KEY),
  set: (token: string): void => safeSet(localStorage, TOKEN_KEY, token),
  clear: (): void => safeSet(localStorage, TOKEN_KEY, null),
}

