import { useEffect } from 'react'

const APP_TITLE = 'RoommateFinder – Tìm người ở ghép'

/** Đặt tiêu đề tab trình duyệt: "<trang> | RoommateFinder – Tìm người ở ghép". */
export function useDocumentTitle(title?: string | null): void {
  useEffect(() => {
    document.title = title ? `${title} | ${APP_TITLE}` : APP_TITLE
    return () => {
      document.title = APP_TITLE
    }
  }, [title])
}
