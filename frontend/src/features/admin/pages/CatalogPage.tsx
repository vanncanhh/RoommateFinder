import { useQuery } from '@tanstack/react-query'
import { Col, Form, Input, InputNumber, Row, Select, Tabs } from 'antd'
import { useSearchParams } from 'react-router-dom'
import { adminApi } from '../../../api/admin'
import { queryKeys } from '../../../api/queryKeys'
import { PageTitle } from '../../../components/shared/PageTitle'
import {
  LANDMARK_TYPES,
  REASON_APPLIES_TO,
  type AmenityDto,
  type AmenityUpsertRequest,
  type AreaDto,
  type AreaUpsertRequest,
  type LandmarkDto,
  type LandmarkType,
  type LandmarkUpsertRequest,
  type ReasonAppliesTo,
  type ReportReasonDto,
  type ReportReasonUpsertRequest,
} from '../../../types'
import { APPLIES_TO_LABELS, LANDMARK_TYPE_LABELS, labelOf, toOptions } from '../../../utils/labels'
import { CatalogTab } from '../components/CatalogTab'

const nameRule = (max: number) => [
  { required: true, whitespace: true, message: 'Không được để trống' },
  { max, message: `Tối đa ${max} ký tự` },
]

const blankToNull = (v: string | undefined) => v?.trim() || null

// ---------------------------------------------------------------- Khu vực
interface AreaForm {
  name: string
  district?: string
  city: string
  latitude?: number | null
  longitude?: number | null
}

function AreasTab() {
  return (
    <CatalogTab<AreaDto, AreaUpsertRequest, AreaForm>
      entityName="khu vực"
      queryKey={queryKeys.admin.areas}
      api={adminApi.areas}
      rowKey="areaId"
      getId={(r) => r.areaId}
      columns={[
        { title: 'ID', dataIndex: 'areaId', width: 60 },
        { title: 'Tên khu vực', dataIndex: 'name' },
        { title: 'Quận/huyện', dataIndex: 'district', render: (v: string | null) => v ?? '—' },
        { title: 'Thành phố', dataIndex: 'city' },
        {
          title: 'Tọa độ tâm',
          key: 'coords',
          render: (_, r) => (r.latitude !== null && r.longitude !== null ? `${r.latitude}, ${r.longitude}` : '—'),
        },
      ]}
      defaultValues={{ city: 'TP. Hồ Chí Minh' }}
      toFormValues={(r) => ({
        name: r.name,
        district: r.district ?? undefined,
        city: r.city,
        latitude: r.latitude,
        longitude: r.longitude,
      })}
      toUpsert={(v, isActive) => ({
        name: v.name.trim(),
        district: blankToNull(v.district),
        city: v.city.trim(),
        latitude: v.latitude ?? null,
        longitude: v.longitude ?? null,
        isActive,
      })}
      rowToUpsert={(r, isActive) => ({
        name: r.name,
        district: r.district,
        city: r.city,
        latitude: r.latitude,
        longitude: r.longitude,
        isActive,
      })}
      renderFields={() => (
        <>
          <Form.Item name="name" label="Tên khu vực" rules={nameRule(100)}>
            <Input maxLength={100} />
          </Form.Item>
          <Row gutter={12}>
            <Col xs={24} sm={12}>
              <Form.Item name="district" label="Quận/huyện" rules={[{ max: 100, message: 'Tối đa 100 ký tự' }]}>
                <Input maxLength={100} />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item name="city" label="Thành phố" rules={nameRule(100)}>
                <Input maxLength={100} />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item
                name="latitude"
                label="Vĩ độ"
                dependencies={['longitude']}
                rules={[
                  ({ getFieldValue }) => ({
                    validator: (_, value: number | null | undefined) =>
                      (value == null) === (getFieldValue('longitude') == null)
                        ? Promise.resolve()
                        : Promise.reject(new Error('Cần nhập đủ vĩ độ và kinh độ')),
                  }),
                ]}
              >
                <InputNumber min={-90} max={90} precision={6} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="longitude" label="Kinh độ" dependencies={['latitude']}>
                <InputNumber min={-180} max={180} precision={6} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
          </Row>
        </>
      )}
    />
  )
}

// ---------------------------------------------------------------- Địa điểm mốc
interface LandmarkForm {
  name: string
  type: LandmarkType
  areaId: number
  address?: string
  latitude: number
  longitude: number
}

function LandmarksTab() {
  const areas = useQuery({ queryKey: queryKeys.admin.areas, queryFn: adminApi.areas.list })
  return (
    <CatalogTab<LandmarkDto, LandmarkUpsertRequest, LandmarkForm>
      entityName="địa điểm mốc"
      queryKey={queryKeys.admin.landmarks}
      api={adminApi.landmarks}
      rowKey="landmarkId"
      getId={(r) => r.landmarkId}
      columns={[
        { title: 'ID', dataIndex: 'landmarkId', width: 60 },
        { title: 'Tên', dataIndex: 'name' },
        { title: 'Loại', dataIndex: 'type', width: 110, render: (v: string) => labelOf(LANDMARK_TYPE_LABELS, v) },
        { title: 'Khu vực', dataIndex: 'areaName' },
        { title: 'Tọa độ', key: 'coords', render: (_, r) => `${r.latitude}, ${r.longitude}` },
      ]}
      defaultValues={{ type: 'school' }}
      toFormValues={(r) => ({
        name: r.name,
        type: r.type,
        areaId: r.areaId,
        address: r.address ?? undefined,
        latitude: r.latitude,
        longitude: r.longitude,
      })}
      toUpsert={(v, isActive) => ({
        name: v.name.trim(),
        type: v.type,
        areaId: v.areaId,
        address: blankToNull(v.address),
        latitude: v.latitude,
        longitude: v.longitude,
        isActive,
      })}
      rowToUpsert={(r, isActive) => ({
        name: r.name,
        type: r.type,
        areaId: r.areaId,
        address: r.address,
        latitude: r.latitude,
        longitude: r.longitude,
        isActive,
      })}
      renderFields={() => (
        <>
          <Form.Item name="name" label="Tên địa điểm" rules={nameRule(150)}>
            <Input maxLength={150} placeholder="VD: Đại học Bách Khoa TP.HCM" />
          </Form.Item>
          <Row gutter={12}>
            <Col xs={24} sm={12}>
              <Form.Item name="type" label="Loại" rules={[{ required: true }]}>
                <Select options={toOptions(LANDMARK_TYPES, LANDMARK_TYPE_LABELS)} />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12}>
              <Form.Item name="areaId" label="Khu vực" rules={[{ required: true, message: 'Chọn khu vực' }]}>
                <Select
                  showSearch
                  optionFilterProp="label"
                  loading={areas.isLoading}
                  options={(areas.data ?? []).map((a) => ({ value: a.areaId, label: a.name }))}
                />
              </Form.Item>
            </Col>
          </Row>
          <Form.Item name="address" label="Địa chỉ" rules={[{ max: 255, message: 'Tối đa 255 ký tự' }]}>
            <Input maxLength={255} />
          </Form.Item>
          <Row gutter={12}>
            <Col span={12}>
              <Form.Item name="latitude" label="Vĩ độ" rules={[{ required: true, message: 'Bắt buộc' }]}>
                <InputNumber min={-90} max={90} precision={6} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="longitude" label="Kinh độ" rules={[{ required: true, message: 'Bắt buộc' }]}>
                <InputNumber min={-180} max={180} precision={6} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
          </Row>
        </>
      )}
    />
  )
}

// ---------------------------------------------------------------- Tiện ích
interface AmenityForm {
  name: string
}

function AmenitiesTab() {
  return (
    <CatalogTab<AmenityDto, AmenityUpsertRequest, AmenityForm>
      entityName="tiện ích"
      queryKey={queryKeys.admin.amenities}
      api={adminApi.amenities}
      rowKey="amenityId"
      getId={(r) => r.amenityId}
      columns={[
        { title: 'ID', dataIndex: 'amenityId', width: 60 },
        { title: 'Tên tiện ích', dataIndex: 'name' },
      ]}
      defaultValues={{}}
      toFormValues={(r) => ({ name: r.name })}
      toUpsert={(v, isActive) => ({ name: v.name.trim(), isActive })}
      rowToUpsert={(r, isActive) => ({ name: r.name, isActive })}
      renderFields={() => (
        <Form.Item name="name" label="Tên tiện ích" rules={nameRule(100)}>
          <Input maxLength={100} placeholder="VD: Máy lạnh" />
        </Form.Item>
      )}
    />
  )
}

// ---------------------------------------------------------------- Lý do báo cáo
interface ReasonForm {
  name: string
  appliesTo: ReasonAppliesTo
}

function ReportReasonsTab() {
  return (
    <CatalogTab<ReportReasonDto, ReportReasonUpsertRequest, ReasonForm>
      entityName="lý do báo cáo"
      queryKey={queryKeys.admin.reportReasons}
      api={adminApi.reportReasons}
      rowKey="reasonId"
      getId={(r) => r.reasonId}
      columns={[
        { title: 'ID', dataIndex: 'reasonId', width: 60 },
        { title: 'Lý do', dataIndex: 'name' },
        { title: 'Áp dụng cho', dataIndex: 'appliesTo', width: 130, render: (v: string) => labelOf(APPLIES_TO_LABELS, v) },
      ]}
      defaultValues={{ appliesTo: 'both' }}
      toFormValues={(r) => ({ name: r.name, appliesTo: r.appliesTo })}
      toUpsert={(v, isActive) => ({ name: v.name.trim(), appliesTo: v.appliesTo, isActive })}
      rowToUpsert={(r, isActive) => ({ name: r.name, appliesTo: r.appliesTo, isActive })}
      renderFields={() => (
        <>
          <Form.Item name="name" label="Lý do" rules={nameRule(150)}>
            <Input maxLength={150} />
          </Form.Item>
          <Form.Item name="appliesTo" label="Áp dụng cho" rules={[{ required: true }]}>
            <Select options={toOptions(REASON_APPLIES_TO, APPLIES_TO_LABELS)} />
          </Form.Item>
        </>
      )}
    />
  )
}

const TABS = [
  { key: 'areas', label: 'Khu vực', children: <AreasTab /> },
  { key: 'landmarks', label: 'Địa điểm mốc', children: <LandmarksTab /> },
  { key: 'amenities', label: 'Tiện ích', children: <AmenitiesTab /> },
  { key: 'report-reasons', label: 'Lý do báo cáo', children: <ReportReasonsTab /> },
]

export default function CatalogPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const tab = TABS.some((t) => t.key === searchParams.get('tab')) ? (searchParams.get('tab') ?? 'areas') : 'areas'
  return (
    <>
      <PageTitle title="Quản lý danh mục" subTitle="Danh mục không xóa được — tắt “Hiển thị” để ẩn." />
      <Tabs activeKey={tab} onChange={(k) => setSearchParams({ tab: k }, { replace: true })} items={TABS} destroyOnHidden />
    </>
  )
}
