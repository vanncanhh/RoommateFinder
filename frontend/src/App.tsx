import { QueryClientProvider } from '@tanstack/react-query'
import { App as AntdApp, ConfigProvider } from 'antd'
import viVN from 'antd/locale/vi_VN'
import { useEffect } from 'react'
import { BrowserRouter } from 'react-router-dom'
import { queryClient } from './api/queryClient'
import { AuthProvider } from './contexts/AuthContext'
import { AppRoutes } from './routes/AppRoutes'
import { feedback } from './utils/feedback'
import './utils/datetime' // khởi tạo dayjs: locale 'vi', plugin utc/timezone

/** Gán instance message/notification có context (theme, locale) cho code ngoài React. */
function AntdFeedbackBridge() {
  const { message, notification } = AntdApp.useApp()
  useEffect(() => {
    feedback.message = message
    feedback.notification = notification
  }, [message, notification])
  return null
}

export default function App() {
  return (
    <ConfigProvider
      locale={viVN}
      theme={{
        token: {
          colorPrimary: '#1677ff',
          borderRadius: 8,
          fontFamily:
            "-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, 'Noto Sans', sans-serif",
        },
      }}
    >
      <AntdApp>
        <AntdFeedbackBridge />
        <QueryClientProvider client={queryClient}>
          <BrowserRouter>
            <AuthProvider>
              <AppRoutes />
            </AuthProvider>
          </BrowserRouter>
        </QueryClientProvider>
      </AntdApp>
    </ConfigProvider>
  )
}
