import { message as staticMessage, notification as staticNotification } from 'antd'
import type { MessageInstance } from 'antd/es/message/interface'
import type { NotificationInstance } from 'antd/es/notification/interface'

/**
 * Cầu nối để code ngoài cây React (QueryClient, interceptor) hiển thị thông báo antd
 * mà vẫn nhận theme/locale từ <App>. `AntdFeedbackBridge` gán instance khi ứng dụng mount.
 */
export const feedback: { message: MessageInstance; notification: NotificationInstance } = {
  message: staticMessage,
  notification: staticNotification,
}
