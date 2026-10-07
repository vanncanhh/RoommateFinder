import { Button, Checkbox, Col, Form, Input, InputNumber, Row, Select, Space } from 'antd'
import { useEffect } from 'react'
import { useAmenities, useAreas, useLandmarks } from '../../../hooks/useCatalog'
import { GENDERS, POST_TYPES, type PostSearchQuery } from '../../../types'
import { formatNumberInput, parseNumberInput } from '../../../utils/format'
import { GENDER_LABELS, LANDMARK_TYPE_LABELS, POST_TYPE_LABELS, toOptions } from '../../../utils/labels'
import { toFilterValues, type SearchFilterValues } from '../searchParams'

const RADIUS_OPTIONS = [1, 2, 3, 5, 10, 20].map((km) => ({ value: km, label: `${km} km` }))

interface SearchFiltersProps {
  query: PostSearchQuery
  onApply: (values: SearchFilterValues) => void
  onReset: () => void
}

/** Bảng bộ lọc tìm kiếm; giá trị luôn đồng bộ với URL. */
export function SearchFilters({ query, onApply, onReset }: SearchFiltersProps) {
  const [form] = Form.useForm<SearchFilterValues>()
  const areaId = Form.useWatch('areaId', form)
  const landmarkId = Form.useWatch('landmarkId', form)
  const areas = useAreas()
  const amenities = useAmenities()
  const landmarks = useLandmarks()

  // URL thay đổi (back/forward, đổi trang) → cập nhật form.
  useEffect(() => {
    form.setFieldsValue(toFilterValues(query))
  }, [form, query])

  const landmarkOptions = (landmarks.data ?? [])
    .filter((l) => !areaId || l.areaId === areaId)
    .map((l) => ({ value: l.landmarkId, label: `${l.name} (${LANDMARK_TYPE_LABELS[l.type] ?? l.type})` }))

  return (
    <Form form={form} layout="vertical" onFinish={onApply} initialValues={toFilterValues(query)}>
      <Form.Item name="keyword" label="Từ khóa">
        <Input allowClear placeholder="Tiêu đề, địa chỉ..." maxLength={100} />
      </Form.Item>
      <Form.Item name="postType" label="Loại tin">
        <Select allowClear placeholder="Tất cả" options={toOptions(POST_TYPES, POST_TYPE_LABELS)} />
      </Form.Item>
      <Form.Item name="areaId" label="Khu vực">
        <Select
          allowClear
          showSearch
          optionFilterProp="label"
          placeholder="Tất cả khu vực"
          loading={areas.isLoading}
          options={(areas.data ?? []).map((a) => ({ value: a.areaId, label: a.name }))}
        />
      </Form.Item>
      <Form.Item label="Giá mỗi người / tháng (đ)" style={{ marginBottom: 0 }}>
        <Row gutter={8}>
          <Col span={12}>
            <Form.Item name="minPrice">
              <InputNumber<number>
                min={0}
                step={500000}
                placeholder="Từ"
                style={{ width: '100%' }}
                formatter={formatNumberInput}
                parser={parseNumberInput}
              />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item
              name="maxPrice"
              dependencies={['minPrice']}
              rules={[
                ({ getFieldValue }) => ({
                  validator: (_, value: number | null) => {
                    const min = getFieldValue('minPrice') as number | null
                    return value == null || min == null || value >= min
                      ? Promise.resolve()
                      : Promise.reject(new Error('Giá tối đa phải ≥ giá tối thiểu'))
                  },
                }),
              ]}
            >
              <InputNumber<number>
                min={0}
                step={500000}
                placeholder="Đến"
                style={{ width: '100%' }}
                formatter={formatNumberInput}
                parser={parseNumberInput}
              />
            </Form.Item>
          </Col>
        </Row>
      </Form.Item>
      <Row gutter={8}>
        <Col span={12}>
          <Form.Item name="gender" label="Giới tính của bạn" tooltip="Hiển thị tin phù hợp giới tính này hoặc không yêu cầu">
            <Select allowClear placeholder="Bất kỳ" options={toOptions(GENDERS, GENDER_LABELS)} />
          </Form.Item>
        </Col>
        <Col span={12}>
          <Form.Item name="minNeeded" label="Cần ít nhất">
            <InputNumber min={1} max={20} placeholder="số người" style={{ width: '100%' }} />
          </Form.Item>
        </Col>
      </Row>
      <Form.Item name="landmarkId" label="Gần địa điểm mốc (trường / công ty)">
        <Select
          allowClear
          showSearch
          optionFilterProp="label"
          placeholder="Chọn địa điểm mốc"
          loading={landmarks.isLoading}
          options={landmarkOptions}
          onChange={(v: number | undefined) => {
            if (v && !form.getFieldValue('radiusKm')) form.setFieldValue('radiusKm', 5)
          }}
        />
      </Form.Item>
      {landmarkId !== undefined && landmarkId !== null && (
        <Form.Item name="radiusKm" label="Bán kính">
          <Select allowClear placeholder="Không giới hạn" options={RADIUS_OPTIONS} />
        </Form.Item>
      )}
      <Form.Item name="amenityIds" label="Tiện ích">
        <Checkbox.Group style={{ width: '100%' }}>
          <Row gutter={[8, 4]}>
            {(amenities.data ?? []).map((a) => (
              <Col span={12} key={a.amenityId}>
                <Checkbox value={a.amenityId}>{a.name}</Checkbox>
              </Col>
            ))}
          </Row>
        </Checkbox.Group>
      </Form.Item>
      <Space style={{ width: '100%' }} direction="vertical">
        <Button type="primary" htmlType="submit" block>
          Áp dụng bộ lọc
        </Button>
        <Button block onClick={onReset}>
          Xóa bộ lọc
        </Button>
      </Space>
    </Form>
  )
}
