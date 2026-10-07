import { Empty, Tooltip, Typography } from 'antd'
import type { DailyCountDto } from '../../../types'
import { dayjs } from '../../../utils/datetime'

interface DailyBarChartProps {
  data: DailyCountDto[]
  /** Đơn vị hiển thị trong tooltip, ví dụ "tin" / "người dùng". */
  unit: string
  color?: string
  height?: number
}

const PAD = { top: 12, right: 8, bottom: 24, left: 32 }
const VIEW_WIDTH = 600

/** Lấy mốc trục Y "đẹp" (1, 2, 5 × 10^n). */
function niceMax(value: number): number {
  if (value <= 4) return 4
  const pow = 10 ** Math.floor(Math.log10(value))
  const n = value / pow
  const step = n <= 1 ? 1 : n <= 2 ? 2 : n <= 5 ? 5 : 10
  return step * pow
}

/**
 * Biểu đồ cột theo ngày bằng SVG thuần (một chuỗi số liệu, một trục).
 * Cột bo tròn đầu, khe 2px giữa các cột, lưới mờ, tooltip khi rê chuột / chạm.
 */
export function DailyBarChart({ data, unit, color = '#1677ff', height = 220 }: DailyBarChartProps) {
  if (data.length === 0) return <Empty description="Không có dữ liệu" image={Empty.PRESENTED_IMAGE_SIMPLE} />

  const max = niceMax(Math.max(...data.map((d) => d.count)))
  const innerW = VIEW_WIDTH - PAD.left - PAD.right
  const innerH = height - PAD.top - PAD.bottom
  const slot = innerW / data.length
  const barW = Math.max(1, slot - 2)
  const ticks = [0, max / 2, max]
  const labelEvery = Math.ceil(data.length / 8)
  const total = data.reduce((s, d) => s + d.count, 0)
  const y = (v: number) => PAD.top + innerH - (v / max) * innerH

  return (
    <div>
      <svg
        viewBox={`0 0 ${VIEW_WIDTH} ${height}`}
        width="100%"
        role="img"
        aria-label={`Biểu đồ số ${unit} theo ngày, tổng ${total}`}
        style={{ display: 'block', height: 'auto' }}
      >
        {ticks.map((t) => (
          <g key={t}>
            <line x1={PAD.left} x2={VIEW_WIDTH - PAD.right} y1={y(t)} y2={y(t)} stroke="#f0f0f0" strokeWidth={1} />
            <text x={PAD.left - 6} y={y(t) + 4} textAnchor="end" fontSize={11} fill="rgba(0,0,0,0.45)">
              {Math.round(t)}
            </text>
          </g>
        ))}
        {data.map((d, i) => {
          const x = PAD.left + i * slot + 1
          const h = (d.count / max) * innerH
          const r = Math.min(4, barW / 2, h)
          const top = PAD.top + innerH - h
          const bottom = PAD.top + innerH
          // Cột bo tròn 2 góc trên, đáy phẳng neo vào trục.
          const path =
            h <= 0
              ? ''
              : `M${x},${bottom} V${top + r} Q${x},${top} ${x + r},${top} H${x + barW - r} Q${x + barW},${top} ${x + barW},${top + r} V${bottom} Z`
          return (
            <Tooltip key={d.date} title={`${dayjs(d.date).format('DD/MM/YYYY')}: ${d.count} ${unit}`}>
              <g style={{ cursor: 'default' }}>
                {/* Vùng bắt chuột rộng hơn cột */}
                <rect x={PAD.left + i * slot} y={PAD.top} width={slot} height={innerH} fill="transparent" />
                {path && <path d={path} fill={color} />}
                {i % labelEvery === 0 && (
                  <text x={x + barW / 2} y={height - 6} textAnchor="middle" fontSize={11} fill="rgba(0,0,0,0.45)">
                    {dayjs(d.date).format('DD/MM')}
                  </text>
                )}
              </g>
            </Tooltip>
          )
        })}
        <line x1={PAD.left} x2={VIEW_WIDTH - PAD.right} y1={y(0)} y2={y(0)} stroke="#d9d9d9" strokeWidth={1} />
      </svg>
      <Typography.Text type="secondary" style={{ fontSize: 12 }}>
        Tổng: {total.toLocaleString('vi-VN')} {unit}
      </Typography.Text>
    </div>
  )
}
