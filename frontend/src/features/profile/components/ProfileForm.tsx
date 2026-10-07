import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Button, Col, DatePicker, Form, Input, Rate, Row, Select } from 'antd'
import type { Dayjs } from 'dayjs'
import { profileApi } from '../../../api/profile'
import { queryKeys } from '../../../api/queryKeys'
import { useLandmarks } from '../../../hooks/useCatalog'
import {
  GENDERS,
  OCCUPATIONS,
  SLEEP_SCHEDULES,
  type Gender,
  type Occupation,
  type ProfileDto,
  type SleepSchedule,
} from '../../../types'
import { dayjs, toDateOnly, todayLocal } from '../../../utils/datetime'
import { feedback } from '../../../utils/feedback'
import {
  CLEANLINESS_LABELS,
  GENDER_LABELS,
  LANDMARK_TYPE_LABELS,
  OCCUPATION_LABELS,
  SLEEP_SCHEDULE_LABELS,
  toOptions,
} from '../../../utils/labels'
import { fullNameRules, phoneRules, SHORT_TEXT_MAX } from '../../../utils/validation'

type YesNo = 'yes' | 'no'

interface ProfileFormValues {
  fullName: string
  phone?: string
  gender?: Gender
  dateOfBirth?: Dayjs | null
  occupation?: Occupation
  schoolOrCompany?: string
  landmarkId?: number
  bio?: string
  sleepSchedule?: SleepSchedule
  isSmoker?: YesNo
  hasPet?: YesNo
  cleanlinessLevel?: number
}

const YES_NO_OPTIONS = [
  { value: 'yes', label: 'Có' },
  { value: 'no', label: 'Không' },
]

const toYesNo = (v: boolean | null): YesNo | undefined => (v === null ? undefined : v ? 'yes' : 'no')
const fromYesNo = (v: YesNo | undefined): boolean | null => (v === undefined ? null : v === 'yes')

function toFormValues(p: ProfileDto): ProfileFormValues {
  return {
    fullName: p.fullName,
    phone: p.phone ?? undefined,
    gender: p.gender ?? undefined,
    dateOfBirth: p.dateOfBirth ? dayjs(p.dateOfBirth) : null,
    occupation: p.occupation ?? undefined,
    schoolOrCompany: p.schoolOrCompany ?? undefined,
    landmarkId: p.landmarkId ?? undefined,
    bio: p.bio ?? undefined,
    sleepSchedule: p.sleepSchedule ?? undefined,
    isSmoker: toYesNo(p.isSmoker),
    hasPet: toYesNo(p.hasPet),
    cleanlinessLevel: p.cleanlinessLevel ?? undefined,
  }
}

/** Form sửa hồ sơ cá nhân + thông tin sinh hoạt. */
export function ProfileForm({ profile }: { profile: ProfileDto }) {
  const queryClient = useQueryClient()
  const landmarks = useLandmarks()
  const maxBirthDate = todayLocal().subtract(16, 'year')

  const mutation = useMutation({
    mutationFn: profileApi.update,
    onSuccess: (updated) => {
      queryClient.setQueryData(queryKeys.profile, updated)
      void queryClient.invalidateQueries({ queryKey: queryKeys.me })
      feedback.message.success('Đã cập nhật hồ sơ')
    },
  })

  const onFinish = (v: ProfileFormValues) =>
    mutation.mutate({
      fullName: v.fullName.trim(),
      phone: v.phone?.trim() || null,
      gender: v.gender ?? null,
      dateOfBirth: toDateOnly(v.dateOfBirth),
      occupation: v.occupation ?? null,
      schoolOrCompany: v.schoolOrCompany?.trim() || null,
      landmarkId: v.landmarkId ?? null,
      bio: v.bio?.trim() || null,
      sleepSchedule: v.sleepSchedule ?? null,
      isSmoker: fromYesNo(v.isSmoker),
      hasPet: fromYesNo(v.hasPet),
      cleanlinessLevel: v.cleanlinessLevel || null,
    })

  return (
    <Form<ProfileFormValues> layout="vertical" initialValues={toFormValues(profile)} onFinish={onFinish}>
      <Row gutter={16}>
        <Col xs={24} md={12}>
          <Form.Item name="fullName" label="Họ và tên" rules={fullNameRules}>
            <Input maxLength={100} />
          </Form.Item>
        </Col>
        <Col xs={24} md={12}>
          <Form.Item label="Email">
            <Input value={profile.email} disabled />
          </Form.Item>
        </Col>
        <Col xs={24} md={12}>
          <Form.Item name="phone" label="Số điện thoại" rules={phoneRules}>
            <Input inputMode="tel" maxLength={11} placeholder="0912345678" />
          </Form.Item>
        </Col>
        <Col xs={12} md={6}>
          <Form.Item name="gender" label="Giới tính">
            <Select allowClear placeholder="Chọn" options={toOptions(GENDERS, GENDER_LABELS)} />
          </Form.Item>
        </Col>
        <Col xs={12} md={6}>
          <Form.Item name="dateOfBirth" label="Ngày sinh" tooltip="Người dùng phải từ 16 tuổi">
            <DatePicker
              format="DD/MM/YYYY"
              style={{ width: '100%' }}
              placeholder="dd/mm/yyyy"
              disabledDate={(d) => d.isAfter(maxBirthDate, 'day')}
              defaultPickerValue={maxBirthDate.subtract(4, 'year')}
            />
          </Form.Item>
        </Col>
        <Col xs={12} md={6}>
          <Form.Item name="occupation" label="Nghề nghiệp">
            <Select allowClear placeholder="Chọn" options={toOptions(OCCUPATIONS, OCCUPATION_LABELS)} />
          </Form.Item>
        </Col>
        <Col xs={12} md={6}>
          <Form.Item name="sleepSchedule" label="Giờ giấc">
            <Select allowClear placeholder="Chọn" options={toOptions(SLEEP_SCHEDULES, SLEEP_SCHEDULE_LABELS)} />
          </Form.Item>
        </Col>
        <Col xs={24} md={12}>
          <Form.Item name="schoolOrCompany" label="Trường / Công ty" rules={[{ max: 150, message: 'Tối đa 150 ký tự' }]}>
            <Input maxLength={150} />
          </Form.Item>
        </Col>
        <Col xs={24} md={12}>
          <Form.Item
            name="landmarkId"
            label="Địa điểm mốc (trường / công ty)"
            tooltip="Dùng để gợi ý tin gần nơi bạn học / làm việc"
          >
            <Select
              allowClear
              showSearch
              optionFilterProp="label"
              placeholder="Chọn địa điểm mốc"
              loading={landmarks.isLoading}
              options={(landmarks.data ?? []).map((l) => ({
                value: l.landmarkId,
                label: `${l.name} (${LANDMARK_TYPE_LABELS[l.type] ?? l.type} – ${l.areaName})`,
              }))}
            />
          </Form.Item>
        </Col>
        <Col xs={12} md={6}>
          <Form.Item name="isSmoker" label="Hút thuốc">
            <Select allowClear placeholder="Chưa cập nhật" options={YES_NO_OPTIONS} />
          </Form.Item>
        </Col>
        <Col xs={12} md={6}>
          <Form.Item name="hasPet" label="Nuôi thú cưng">
            <Select allowClear placeholder="Chưa cập nhật" options={YES_NO_OPTIONS} />
          </Form.Item>
        </Col>
        <Col xs={24} md={12}>
          <Form.Item name="cleanlinessLevel" label="Mức độ gọn gàng">
            <Rate count={5} tooltips={[1, 2, 3, 4, 5].map((n) => CLEANLINESS_LABELS[n])} />
          </Form.Item>
        </Col>
        <Col xs={24}>
          <Form.Item name="bio" label="Giới thiệu bản thân" rules={[{ max: SHORT_TEXT_MAX, message: `Tối đa ${SHORT_TEXT_MAX} ký tự` }]}>
            <Input.TextArea rows={4} showCount maxLength={SHORT_TEXT_MAX} />
          </Form.Item>
        </Col>
      </Row>
      <Button type="primary" htmlType="submit" loading={mutation.isPending}>
        Lưu hồ sơ
      </Button>
    </Form>
  )
}
