import { Descriptions } from 'antd'
import type { PublicProfileDto } from '../../../types'
import {
  CLEANLINESS_LABELS,
  GENDER_LABELS,
  OCCUPATION_LABELS,
  SLEEP_SCHEDULE_LABELS,
  labelOf,
  yesNoLabel,
} from '../../../utils/labels'

type LifestyleInfo = Pick<
  PublicProfileDto,
  'gender' | 'occupation' | 'schoolOrCompany' | 'sleepSchedule' | 'isSmoker' | 'hasPet' | 'cleanlinessLevel'
>

const NOT_SET = 'Chưa cập nhật'

/** Thông tin sinh hoạt (giờ giấc, hút thuốc, thú cưng, mức gọn gàng). */
export function LifestyleDescriptions({ info }: { info: LifestyleInfo }) {
  return (
    <Descriptions column={{ xs: 1, sm: 2 }} size="small" bordered>
      <Descriptions.Item label="Giới tính">{labelOf(GENDER_LABELS, info.gender, NOT_SET)}</Descriptions.Item>
      <Descriptions.Item label="Nghề nghiệp">{labelOf(OCCUPATION_LABELS, info.occupation, NOT_SET)}</Descriptions.Item>
      <Descriptions.Item label="Trường / Công ty">{info.schoolOrCompany || NOT_SET}</Descriptions.Item>
      <Descriptions.Item label="Giờ giấc">{labelOf(SLEEP_SCHEDULE_LABELS, info.sleepSchedule, NOT_SET)}</Descriptions.Item>
      <Descriptions.Item label="Hút thuốc">{yesNoLabel(info.isSmoker)}</Descriptions.Item>
      <Descriptions.Item label="Nuôi thú cưng">{yesNoLabel(info.hasPet)}</Descriptions.Item>
      <Descriptions.Item label="Mức độ gọn gàng">
        {info.cleanlinessLevel ? `${info.cleanlinessLevel}/5 – ${CLEANLINESS_LABELS[info.cleanlinessLevel] ?? ''}` : NOT_SET}
      </Descriptions.Item>
    </Descriptions>
  )
}
