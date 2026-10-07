import { HomeOutlined } from '@ant-design/icons'
import { Flex, Image } from 'antd'
import { useState } from 'react'
import type { PostImageDto } from '../../../types'
import { resolveFileUrl } from '../../../utils/format'

/** Ảnh lớn + dải ảnh nhỏ; bấm ảnh lớn để xem toàn màn hình (PreviewGroup). */
export function ImageGallery({ images, title }: { images: PostImageDto[]; title: string }) {
  const sorted = [...images].sort((a, b) => a.sortOrder - b.sortOrder)
  const [current, setCurrent] = useState(0)
  const [previewOpen, setPreviewOpen] = useState(false)

  if (sorted.length === 0) {
    return (
      <Flex
        align="center"
        justify="center"
        style={{ aspectRatio: '16 / 9', background: '#f0f2f5', borderRadius: 8, color: '#bfbfbf', fontSize: 64 }}
      >
        <HomeOutlined />
      </Flex>
    )
  }

  const index = Math.min(current, sorted.length - 1)

  return (
    <div>
      <div
        style={{ aspectRatio: '16 / 9', background: '#000', borderRadius: 8, overflow: 'hidden', cursor: 'zoom-in' }}
        onClick={() => setPreviewOpen(true)}
      >
        <Image
          src={resolveFileUrl(sorted[index].imageUrl)}
          alt={title}
          width="100%"
          height="100%"
          preview={false}
          style={{ objectFit: 'contain' }}
          wrapperStyle={{ width: '100%', height: '100%' }}
        />
      </div>
      <Image.PreviewGroup
        items={sorted.map((img) => resolveFileUrl(img.imageUrl) ?? '')}
        preview={{
          visible: previewOpen,
          onVisibleChange: setPreviewOpen,
          current: index,
          onChange: (c: number) => setCurrent(c),
        }}
      />
      {sorted.length > 1 && (
        <Flex gap={8} style={{ marginTop: 8, overflowX: 'auto', paddingBottom: 4 }}>
          {sorted.map((img, i) => (
            <button
              key={img.imageId}
              type="button"
              onClick={() => setCurrent(i)}
              aria-label={`Ảnh ${i + 1}`}
              style={{
                padding: 0,
                border: i === index ? '2px solid #1677ff' : '2px solid transparent',
                borderRadius: 6,
                overflow: 'hidden',
                cursor: 'pointer',
                flex: '0 0 auto',
                background: 'none',
              }}
            >
              <img
                src={resolveFileUrl(img.imageUrl)}
                alt=""
                style={{ width: 80, height: 60, objectFit: 'cover', display: 'block' }}
              />
            </button>
          ))}
        </Flex>
      )}
    </div>
  )
}
