import { Button, Result } from 'antd'
import { useNavigate } from 'react-router-dom'
import { useDocumentTitle } from '../../hooks/useDocumentTitle'

export default function NotFoundPage() {
  useDocumentTitle('Không tìm thấy trang')
  const navigate = useNavigate()
  return (
    <Result
      status="404"
      title="404"
      subTitle="Trang bạn tìm không tồn tại hoặc đã bị xóa."
      extra={
        <Button type="primary" onClick={() => navigate('/')}>
          Về trang chủ
        </Button>
      }
    />
  )
}
