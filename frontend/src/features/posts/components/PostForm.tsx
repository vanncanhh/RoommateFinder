import { DeleteOutlined, PlusOutlined, UndoOutlined } from '@ant-design/icons'
import {
  Alert,
  Button,
  Card,
  Checkbox,
  Col,
  Flex,
  Form,
  Input,
  InputNumber,
  Radio,
  Row,
  Select,
  Typography,
  Upload,
  type UploadFile,
} from 'antd'
import { useEffect, useMemo, useRef, useState } from 'react'
import { useAmenities, useAreas } from '../../../hooks/useCatalog'
import { PREFERRED_GENDERS, type PostDetailDto, type PostFormData, type PostType, type PreferredGender } from '../../../types'
import { feedback } from '../../../utils/feedback'
import { formatNumberInput, parseNumberInput, resolveFileUrl } from '../../../utils/format'
import { PREFERRED_GENDER_LABELS, toOptions } from '../../../utils/labels'
import {
  IMAGE_ACCEPT,
  IMAGE_MAX_SIZE_MB,
  POST_ADDRESS_MAX,
  POST_DESCRIPTION_MAX,
  POST_MAX_IMAGES,
  POST_OCCUPANTS_MAX,
  POST_PRICE_MAX,
  POST_TITLE_MAX,
  POST_TITLE_MIN,
  validateImageFile,
} from '../../../utils/validation'

interface PostFormValues {
  postType: PostType
  title: string
  description?: string
  price: number
  areaId: number
  address?: string
  latitude?: number | null
  longitude?: number | null
  currentOccupants: number
  neededOccupants: number
  preferredGender: PreferredGender
  amenityIds: number[]
}

interface PostFormProps {
  /** Tin đang sửa; bỏ trống = đăng tin mới. */
  initial?: PostDetailDto
  submitting: boolean
  onSubmit: (data: PostFormData, newImages: File[]) => void
}

const TYPE_TEXT: Record<PostType, { price: string; current: string; needed: string; imagesHint: string }> = {
  has_room: {
    price: 'Giá thuê mỗi người / tháng (đ)',
    current: 'Số người đang ở',
    needed: 'Cần tìm thêm (người)',
    imagesHint: `Bắt buộc 1–${POST_MAX_IMAGES} ảnh phòng (JPG, PNG, WEBP; mỗi ảnh ≤ ${IMAGE_MAX_SIZE_MB} MB).`,
  },
  seeking: {
    price: 'Ngân sách mỗi người / tháng (đ)',
    current: 'Số người trong nhóm của bạn',
    needed: 'Số người cần ở ghép thêm',
    imagesHint: `Không bắt buộc, tối đa ${POST_MAX_IMAGES} ảnh (JPG, PNG, WEBP; mỗi ảnh ≤ ${IMAGE_MAX_SIZE_MB} MB).`,
  },
}

function toInitialValues(post?: PostDetailDto): Partial<PostFormValues> {
  if (!post) {
    return { postType: 'has_room', currentOccupants: 1, neededOccupants: 1, preferredGender: 'any', amenityIds: [] }
  }
  return {
    postType: post.postType,
    title: post.title,
    description: post.description ?? undefined,
    price: post.price,
    areaId: post.areaId,
    address: post.address ?? undefined,
    latitude: post.latitude,
    longitude: post.longitude,
    currentOccupants: post.currentOccupants,
    neededOccupants: post.neededOccupants,
    preferredGender: post.preferredGender ?? 'any',
    amenityIds: post.amenities.map((a) => a.amenityId),
  }
}

/** Form dùng chung cho Đăng tin / Sửa tin. Gửi multipart: trường tin + amenityIds + keepImageIds + images. */
export function PostForm({ initial, submitting, onSubmit }: PostFormProps) {
  const isEdit = initial !== undefined
  const [form] = Form.useForm<PostFormValues>()
  const postType = Form.useWatch('postType', form) ?? initial?.postType ?? 'has_room'
  const areas = useAreas()
  const amenities = useAmenities()

  const [fileList, setFileList] = useState<UploadFile[]>([])
  const [removedImageIds, setRemovedImageIds] = useState<number[]>([])
  const existingImages = useMemo(
    () => [...(initial?.images ?? [])].sort((a, b) => a.sortOrder - b.sortOrder),
    [initial],
  )
  const keptCount = existingImages.length - removedImageIds.length
  const totalImages = keptCount + fileList.length
  const texts = TYPE_TEXT[postType]

  // Thu hồi object URL xem trước khi gỡ component.
  const fileListRef = useRef<UploadFile[]>([])
  const updateFileList = (next: UploadFile[]) => {
    fileListRef.current = next
    setFileList(next)
  }
  useEffect(
    () => () => {
      fileListRef.current.forEach((f) => {
        if (f.thumbUrl?.startsWith('blob:')) URL.revokeObjectURL(f.thumbUrl)
      })
    },
    [],
  )

  const imageError =
    totalImages > POST_MAX_IMAGES
      ? `Mỗi tin tối đa ${POST_MAX_IMAGES} ảnh.`
      : postType === 'has_room' && totalImages === 0
        ? 'Tin có phòng cần ít nhất 1 ảnh.'
        : null

  const toggleExisting = (imageId: number) =>
    setRemovedImageIds((prev) => (prev.includes(imageId) ? prev.filter((x) => x !== imageId) : [...prev, imageId]))

  const handleFinish = (v: PostFormValues) => {
    if (imageError) {
      feedback.message.error(imageError)
      return
    }
    const hasLat = v.latitude !== null && v.latitude !== undefined
    const hasLng = v.longitude !== null && v.longitude !== undefined
    const data: PostFormData = {
      postType: v.postType,
      title: v.title.trim(),
      description: v.description?.trim() || null,
      price: v.price,
      areaId: v.areaId,
      address: v.address?.trim() || null,
      latitude: hasLat && hasLng ? v.latitude : null,
      longitude: hasLat && hasLng ? v.longitude : null,
      currentOccupants: v.currentOccupants,
      neededOccupants: v.neededOccupants,
      preferredGender: v.preferredGender,
      amenityIds: v.amenityIds ?? [],
      keepImageIds: existingImages.filter((i) => !removedImageIds.includes(i.imageId)).map((i) => i.imageId),
    }
    const files = fileList.map((f) => f.originFileObj).filter((f): f is NonNullable<typeof f> => f !== undefined)
    onSubmit(data, files)
  }

  return (
    <Form<PostFormValues>
      form={form}
      layout="vertical"
      initialValues={toInitialValues(initial)}
      onFinish={handleFinish}
      scrollToFirstError
    >
      <Card title="Thông tin tin đăng" style={{ marginBottom: 16 }}>
        <Form.Item
          name="postType"
          label="Loại tin"
          rules={[{ required: true }]}
          extra={isEdit ? 'Không thể đổi loại tin sau khi đăng.' : undefined}
        >
          <Radio.Group disabled={isEdit} optionType="button" buttonStyle="solid">
            <Radio value="has_room">Có phòng, cần người ở ghép</Radio>
            <Radio value="seeking">Đang tìm phòng ở ghép</Radio>
          </Radio.Group>
        </Form.Item>
        <Form.Item
          name="title"
          label="Tiêu đề"
          rules={[
            { required: true, whitespace: true, message: 'Vui lòng nhập tiêu đề' },
            { min: POST_TITLE_MIN, max: POST_TITLE_MAX, message: `Tiêu đề phải từ ${POST_TITLE_MIN} đến ${POST_TITLE_MAX} ký tự` },
          ]}
        >
          <Input
            showCount
            maxLength={POST_TITLE_MAX}
            placeholder={postType === 'has_room' ? 'VD: Phòng 25m² gần ĐH Bách Khoa, cần 1 nữ ở ghép' : 'VD: Nam sinh viên năm 2 tìm phòng ở ghép quận 10'}
          />
        </Form.Item>
        <Form.Item name="description" label="Mô tả" rules={[{ max: POST_DESCRIPTION_MAX, message: `Tối đa ${POST_DESCRIPTION_MAX} ký tự` }]}>
          <Input.TextArea
            rows={5}
            showCount
            maxLength={POST_DESCRIPTION_MAX}
            placeholder="Mô tả phòng / bản thân, giờ giấc sinh hoạt, yêu cầu với người ở ghép..."
          />
        </Form.Item>
        <Row gutter={16}>
          <Col xs={24} md={12}>
            <Form.Item
              name="price"
              label={texts.price}
              rules={[
                { required: true, message: 'Vui lòng nhập giá' },
                {
                  validator: (_, value: number | null) =>
                    value === null || value === undefined || (value > 0 && value <= POST_PRICE_MAX)
                      ? Promise.resolve()
                      : Promise.reject(new Error('Giá phải lớn hơn 0 và không quá 1 tỷ đồng')),
                },
              ]}
            >
              <InputNumber<number>
                min={0}
                max={POST_PRICE_MAX}
                step={100000}
                precision={0}
                style={{ width: '100%' }}
                formatter={formatNumberInput}
                parser={parseNumberInput}
                suffix="đ"
              />
            </Form.Item>
          </Col>
          <Col xs={24} md={12}>
            <Form.Item name="preferredGender" label="Giới tính mong muốn" rules={[{ required: true }]}>
              <Select options={toOptions(PREFERRED_GENDERS, PREFERRED_GENDER_LABELS)} />
            </Form.Item>
          </Col>
          <Col xs={12}>
            <Form.Item
              name="currentOccupants"
              label={texts.current}
              rules={[{ required: true, message: 'Bắt buộc' }]}
            >
              <InputNumber min={0} max={POST_OCCUPANTS_MAX} precision={0} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
          <Col xs={12}>
            <Form.Item name="neededOccupants" label={texts.needed} rules={[{ required: true, message: 'Bắt buộc' }]}>
              <InputNumber min={1} max={POST_OCCUPANTS_MAX} precision={0} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
        </Row>
      </Card>

      <Card title="Vị trí" style={{ marginBottom: 16 }}>
        <Row gutter={16}>
          <Col xs={24} md={12}>
            <Form.Item name="areaId" label="Khu vực" rules={[{ required: true, message: 'Vui lòng chọn khu vực' }]}>
              <Select
                showSearch
                optionFilterProp="label"
                placeholder="Chọn khu vực"
                loading={areas.isLoading}
                options={(areas.data ?? []).map((a) => ({
                  value: a.areaId,
                  label: [a.name, a.district, a.city].filter(Boolean).join(', '),
                }))}
              />
            </Form.Item>
          </Col>
          <Col xs={24} md={12}>
            <Form.Item name="address" label="Địa chỉ cụ thể" rules={[{ max: POST_ADDRESS_MAX, message: `Tối đa ${POST_ADDRESS_MAX} ký tự` }]}>
              <Input placeholder="Số nhà, tên đường (không bắt buộc)" maxLength={POST_ADDRESS_MAX} />
            </Form.Item>
          </Col>
          <Col xs={12} md={6}>
            <Form.Item
              name="latitude"
              label="Vĩ độ"
              dependencies={['longitude']}
              rules={[
                ({ getFieldValue }) => ({
                  validator: (_, value: number | null | undefined) => {
                    const other = getFieldValue('longitude') as number | null | undefined
                    const a = value !== null && value !== undefined
                    const b = other !== null && other !== undefined
                    return a === b ? Promise.resolve() : Promise.reject(new Error('Cần nhập đủ vĩ độ và kinh độ'))
                  },
                }),
              ]}
            >
              <InputNumber min={-90} max={90} step={0.000001} precision={6} style={{ width: '100%' }} placeholder="10.772" />
            </Form.Item>
          </Col>
          <Col xs={12} md={6}>
            <Form.Item name="longitude" label="Kinh độ" dependencies={['latitude']}>
              <InputNumber min={-180} max={180} step={0.000001} precision={6} style={{ width: '100%' }} placeholder="106.657" />
            </Form.Item>
          </Col>
          <Col xs={24} md={12}>
            <Typography.Text type="secondary" style={{ fontSize: 13 }}>
              Tọa độ không bắt buộc — nếu bỏ trống, hệ thống dùng tọa độ tâm khu vực để tính khoảng cách. Có thể lấy tọa độ
              bằng cách nhấp chuột phải trên{' '}
              <a href="https://www.openstreetmap.org" target="_blank" rel="noreferrer">
                OpenStreetMap
              </a>
              .
            </Typography.Text>
          </Col>
        </Row>
      </Card>

      <Card title="Tiện ích" style={{ marginBottom: 16 }}>
        <Form.Item name="amenityIds" noStyle>
          <Checkbox.Group style={{ width: '100%' }}>
            <Row gutter={[8, 8]}>
              {(amenities.data ?? []).map((a) => (
                <Col xs={12} sm={8} md={6} key={a.amenityId}>
                  <Checkbox value={a.amenityId}>{a.name}</Checkbox>
                </Col>
              ))}
            </Row>
          </Checkbox.Group>
        </Form.Item>
      </Card>

      <Card title={`Hình ảnh (${totalImages}/${POST_MAX_IMAGES})`} style={{ marginBottom: 16 }}>
        <Typography.Paragraph type="secondary">{texts.imagesHint}</Typography.Paragraph>
        {existingImages.length > 0 && (
          <>
            <Typography.Text strong>Ảnh hiện có (bấm để bỏ / giữ lại)</Typography.Text>
            <Flex gap={8} wrap="wrap" style={{ margin: '8px 0 16px' }}>
              {existingImages.map((img) => {
                const removed = removedImageIds.includes(img.imageId)
                return (
                  <div key={img.imageId} style={{ position: 'relative', width: 104, height: 104 }}>
                    <img
                      src={resolveFileUrl(img.imageUrl)}
                      alt=""
                      style={{
                        width: '100%',
                        height: '100%',
                        objectFit: 'cover',
                        borderRadius: 8,
                        opacity: removed ? 0.3 : 1,
                        border: '1px solid #d9d9d9',
                      }}
                    />
                    <Button
                      size="small"
                      shape="circle"
                      danger={!removed}
                      icon={removed ? <UndoOutlined /> : <DeleteOutlined />}
                      aria-label={removed ? 'Giữ lại ảnh' : 'Bỏ ảnh'}
                      onClick={() => toggleExisting(img.imageId)}
                      style={{ position: 'absolute', top: 4, right: 4 }}
                    />
                  </div>
                )
              })}
            </Flex>
          </>
        )}
        <Upload
          listType="picture-card"
          accept={IMAGE_ACCEPT}
          multiple
          fileList={fileList}
          beforeUpload={(file) => {
            const error = validateImageFile(file)
            if (error) {
              feedback.message.error(error)
              return Upload.LIST_IGNORE
            }
            return false // Không tải lên ngay — gửi cùng form.
          }}
          onChange={({ fileList: next }) =>
            updateFileList(
              next.map((f) =>
                f.originFileObj && !f.thumbUrl ? { ...f, thumbUrl: URL.createObjectURL(f.originFileObj) } : f,
              ),
            )
          }
          onRemove={(file) => {
            if (file.thumbUrl?.startsWith('blob:')) URL.revokeObjectURL(file.thumbUrl)
          }}
          onPreview={(file) => {
            if (file.thumbUrl) window.open(file.thumbUrl, '_blank', 'noopener')
          }}
        >
          {totalImages < POST_MAX_IMAGES && (
            <div>
              <PlusOutlined />
              <div style={{ marginTop: 8 }}>Thêm ảnh</div>
            </div>
          )}
        </Upload>
        {imageError && <Alert type="warning" showIcon message={imageError} style={{ marginTop: 8 }} />}
      </Card>

      {isEdit && (
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 16 }}
          message="Sau khi sửa, tin sẽ chuyển về trạng thái chờ duyệt."
        />
      )}
      <Flex justify="end">
        <Button type="primary" htmlType="submit" size="large" loading={submitting}>
          {isEdit ? 'Lưu thay đổi' : 'Đăng tin'}
        </Button>
      </Flex>
    </Form>
  )
}
