import { Alert, Box, Button, Checkbox, FormControlLabel, Stack, Typography } from '@mui/material'
import { useEffect, useMemo, useRef, useState } from 'react'
import type { ThreadImageAttachment } from '@/schema/thread'
import { THREAD_IMAGE_ACCEPTED_TYPES } from '@/schema/thread'
import { isAcceptedThreadImageType, shrinkThreadImageIfNeeded } from '@/lib/threadImage'
import locale from '../../../locale/threads/Threads.json'

const imageLocale = locale.form.image

const secondaryButtonSx = {
  borderRadius: 2,
  minHeight: 36,
  borderColor: '#8b7a64',
  color: '#4f392c',
  backgroundColor: 'transparent',
  boxShadow: 'none',
  '&:hover': {
    borderColor: '#8b7a64',
    backgroundColor: 'transparent',
    boxShadow: 'none',
  },
} as const

type ThreadImagePickerProps = {
  value: ThreadImageAttachment | null
  onChange: (value: ThreadImageAttachment | null) => void
  maxBytes: number
  disabled?: boolean
}

function toMegabytes(bytes: number): string {
  return String(Math.floor(bytes / 1024 / 1024))
}

export default function ThreadImagePicker({ value, onChange, maxBytes, disabled = false }: ThreadImagePickerProps) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [error, setError] = useState<string | null>(null)
  const [isPreparing, setIsPreparing] = useState(false)
  const previewUrl = useMemo(() => (value ? URL.createObjectURL(value.file) : null), [value])

  useEffect(() => {
    return () => {
      if (previewUrl) {
        URL.revokeObjectURL(previewUrl)
      }
    }
  }, [previewUrl])

  const handleSelect = async (file: File | undefined) => {
    if (!file) {
      return
    }

    setError(null)
    if (!isAcceptedThreadImageType(file.type)) {
      setError(imageLocale.invalidType)
      return
    }

    setIsPreparing(true)
    try {
      const prepared = await shrinkThreadImageIfNeeded(file, maxBytes)
      if (prepared.size > maxBytes) {
        setError(imageLocale.tooLarge.replace('{size}', toMegabytes(maxBytes)))
        return
      }

      onChange({ file: prepared, isSensitive: value?.isSensitive ?? false })
    } catch {
      setError(imageLocale.readFailed)
    } finally {
      setIsPreparing(false)
    }
  }

  return (
    <Stack spacing={1}>
      <input
        ref={inputRef}
        type="file"
        accept={THREAD_IMAGE_ACCEPTED_TYPES.join(',')}
        hidden
        onChange={(event) => {
          void handleSelect(event.target.files?.[0])
          // 同じファイルを選び直しても onChange が呼ばれるように戻す
          event.target.value = ''
        }}
      />

      {previewUrl ? (
        <Box
          component="img"
          src={previewUrl}
          alt={imageLocale.previewAlt}
          sx={{
            display: 'block',
            maxWidth: '100%',
            maxHeight: 220,
            objectFit: 'contain',
            borderRadius: 2,
            border: '1px solid #94704c',
            backgroundColor: '#fff7ea',
            filter: value?.isSensitive ? 'blur(12px)' : 'none',
          }}
        />
      ) : null}

      <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
        <Button
          variant="outlined"
          sx={secondaryButtonSx}
          disabled={disabled || isPreparing}
          onClick={() => inputRef.current?.click()}
        >
          {value ? imageLocale.change : imageLocale.select}
        </Button>
        {value ? (
          <Button
            variant="outlined"
            sx={secondaryButtonSx}
            disabled={disabled || isPreparing}
            onClick={() => {
              setError(null)
              onChange(null)
            }}
          >
            {imageLocale.remove}
          </Button>
        ) : null}
      </Stack>

      {value ? (
        <FormControlLabel
          control={
            <Checkbox
              checked={value.isSensitive}
              disabled={disabled}
              onChange={(event) => onChange({ ...value, isSensitive: event.target.checked })}
              sx={{ color: '#5b4030', '&.Mui-checked': { color: '#5b4030' } }}
            />
          }
          label={
            <Typography variant="body2" sx={{ color: '#4f392c' }}>
              {imageLocale.sensitive}
            </Typography>
          }
        />
      ) : null}

      <Typography variant="caption" sx={{ color: 'rgba(79, 57, 44, 0.7)' }}>
        {imageLocale.helper.replace('{size}', toMegabytes(maxBytes))}
      </Typography>

      {error ? <Alert severity="warning">{error}</Alert> : null}
    </Stack>
  )
}
