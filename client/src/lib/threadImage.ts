import { THREAD_IMAGE_ACCEPTED_TYPES } from '@/schema/thread'

// 送る前に縮小するときの長辺。サーバーでさらに 1280px まで縮小・再エンコードする
const CLIENT_SHRINK_LONG_EDGE = 2560
const JPEG_QUALITY = 0.9

export function isAcceptedThreadImageType(type: string): boolean {
  return (THREAD_IMAGE_ACCEPTED_TYPES as readonly string[]).includes(type)
}

/**
 * サイズの上限を超える画像（スマホの写真など）は、送る前にブラウザで縮小する。
 * 形式は元のまま。縮小しても上限を超える場合や、縮小できない場合は元のファイルを返す（サーバーで弾かれる）。
 */
export async function shrinkThreadImageIfNeeded(file: File, maxBytes: number): Promise<File> {
  if (file.size <= maxBytes || typeof createImageBitmap !== 'function') {
    return file
  }

  try {
    const bitmap = await createImageBitmap(file)
    const scale = Math.min(1, CLIENT_SHRINK_LONG_EDGE / Math.max(bitmap.width, bitmap.height))
    const width = Math.max(1, Math.round(bitmap.width * scale))
    const height = Math.max(1, Math.round(bitmap.height * scale))

    const canvas = document.createElement('canvas')
    canvas.width = width
    canvas.height = height
    const context = canvas.getContext('2d')
    if (!context) {
      bitmap.close()
      return file
    }

    context.drawImage(bitmap, 0, 0, width, height)
    bitmap.close()

    const blob = await new Promise<Blob | null>((resolve) => canvas.toBlob(resolve, file.type, JPEG_QUALITY))
    if (!blob || blob.size >= file.size) {
      return file
    }

    return new File([blob], file.name, { type: file.type, lastModified: file.lastModified })
  } catch {
    return file
  }
}
