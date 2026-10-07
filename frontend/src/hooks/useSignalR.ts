import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
  type HubConnection,
  type IRetryPolicy,
} from '@microsoft/signalr'
import { useEffect, useMemo, useState } from 'react'
import { API_BASE_URL } from '../api/client'
import { tokenStorage } from '../utils/tokenStorage'

/** Thử lại mãi mãi: 0s, 2s, 5s, 10s rồi cứ 15s một lần. */
const retryPolicy: IRetryPolicy = {
  nextRetryDelayInMilliseconds: ({ previousRetryCount }) => [0, 2000, 5000, 10000][previousRetryCount] ?? 15000,
}

export interface HubState {
  connection: HubConnection | null
  /** `true` khi kết nối đang ở trạng thái Connected. */
  isConnected: boolean
  /** Tăng mỗi lần (tái) kết nối thành công — dùng làm tín hiệu tải lại dữ liệu bị lỡ. */
  connectedVersion: number
}

interface ConnectionStatus {
  connection: HubConnection | null
  connected: boolean
  version: number
}

/**
 * Mở một kết nối SignalR tới `path` khi đã đăng nhập (`accessToken` khác null), tự kết nối lại,
 * đóng kết nối khi unmount hoặc khi token đổi (đăng xuất / đăng nhập tài khoản khác).
 * Token truyền qua `accessTokenFactory` (server đọc `access_token` trên query string).
 */
export function useSignalR(path: string, accessToken: string | null): HubState {
  const connection = useMemo(
    () =>
      accessToken === null
        ? null
        : new HubConnectionBuilder()
            .withUrl(`${API_BASE_URL}${path}`, { accessTokenFactory: () => tokenStorage.get() ?? '' })
            .withAutomaticReconnect(retryPolicy)
            .configureLogging(import.meta.env.DEV ? LogLevel.Warning : LogLevel.Error)
            .build(),
    [path, accessToken],
  )

  // Trạng thái gắn với đúng instance kết nối → khi đổi kết nối, trạng thái cũ tự bị bỏ qua.
  const [status, setStatus] = useState<ConnectionStatus>({ connection: null, connected: false, version: 0 })

  useEffect(() => {
    if (!connection) return undefined
    let disposed = false
    let retryTimer: ReturnType<typeof setTimeout> | undefined

    const update = (connected: boolean, bumpVersion: boolean) =>
      setStatus((prev) => ({
        connection,
        connected,
        version: (prev.connection === connection ? prev.version : 0) + (bumpVersion ? 1 : 0),
      }))

    // Lần kết nối đầu tiên thất bại thì withAutomaticReconnect không áp dụng → tự thử lại.
    const start = async (attempt: number) => {
      if (disposed) return
      try {
        await connection.start()
        if (!disposed) update(true, true)
      } catch {
        if (disposed) return
        update(false, false)
        retryTimer = setTimeout(() => void start(attempt + 1), Math.min(30000, 2000 * (attempt + 1)))
      }
    }

    // Callback đăng ký trên instance không gỡ được → kiểm tra `disposed` (StrictMode chạy effect 2 lần).
    connection.onreconnecting(() => {
      if (!disposed) update(false, false)
    })
    connection.onreconnected(() => {
      if (!disposed) update(true, true)
    })
    connection.onclose(() => {
      if (disposed) return
      update(false, false)
      // Hết lượt tự kết nối lại (hoặc server đóng) → bắt đầu lại từ đầu.
      retryTimer = setTimeout(() => void start(0), 5000)
    })

    // Hoãn sang tick sau: StrictMode (dev) chạy effect → cleanup → effect ngay lập tức; cleanup kịp hủy timer này
    // nên không có kết nối nào bị dừng giữa lúc negotiate ("The connection was stopped during negotiation").
    retryTimer = setTimeout(() => void start(0), 0)

    return () => {
      disposed = true
      if (retryTimer) clearTimeout(retryTimer)
      if (connection.state !== HubConnectionState.Disconnected) void connection.stop()
    }
  }, [connection])

  const current = status.connection === connection
  return {
    connection,
    isConnected: current && status.connected,
    connectedVersion: current ? status.version : 0,
  }
}
