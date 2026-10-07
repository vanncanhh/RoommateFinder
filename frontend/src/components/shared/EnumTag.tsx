import { Tag } from 'antd'
import { labelInfoOf, type LabelInfo } from '../../utils/labels'

interface EnumTagProps<K extends string> {
  map: Record<K, LabelInfo>
  value: string | null | undefined
}

/** Tag hiển thị giá trị liệt kê bằng tiếng Việt kèm màu. */
export function EnumTag<K extends string>({ map, value }: EnumTagProps<K>) {
  const info = labelInfoOf(map, value)
  return <Tag color={info.color}>{info.label}</Tag>
}
