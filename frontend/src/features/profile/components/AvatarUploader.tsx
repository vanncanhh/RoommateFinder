import { CameraOutlined } from '@ant-design/icons'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Button, Upload } from 'antd'
import { profileApi } from '../../../api/profile'
import { queryKeys } from '../../../api/queryKeys'
import { feedback } from '../../../utils/feedback'
import { IMAGE_ACCEPT, validateImageFile } from '../../../utils/validation'

/** Nút đổi ảnh đại diện (multipart field `file`). */
export function AvatarUploader() {
  const queryClient = useQueryClient()
  const mutation = useMutation({
    mutationFn: profileApi.uploadAvatar,
    onSuccess: (profile) => {
      queryClient.setQueryData(queryKeys.profile, profile)
      void queryClient.invalidateQueries({ queryKey: queryKeys.me })
      feedback.message.success('Đã cập nhật ảnh đại diện')
    },
  })

  return (
    <Upload
      accept={IMAGE_ACCEPT}
      showUploadList={false}
      beforeUpload={(file) => {
        const error = validateImageFile(file)
        if (error) feedback.message.error(error)
        else mutation.mutate(file)
        return false
      }}
    >
      <Button icon={<CameraOutlined />} loading={mutation.isPending}>
        Đổi ảnh đại diện
      </Button>
    </Upload>
  )
}
