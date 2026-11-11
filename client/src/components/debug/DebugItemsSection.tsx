import { Autocomplete, Button, Stack, TextField } from '@mui/material'
import { useState } from 'react'
import useSWR from 'swr'
import { getDebugEquipments, getDebugItems, grantDebugEquipment, grantDebugItem } from '@/api/debug'
import DebugSection from '@/components/debug/DebugSection'
import { useDebugAction } from '@/hooks/useDebugAction'
import { debugGrantEquipmentRequestSchema, debugGrantItemRequestSchema } from '@/schema/debug'
import type { DebugEquipmentListResponse, DebugItemListResponse } from '@/schema/debug'
import locale from '../../../locale/debug/Debug.json'

type DebugItemsSectionProps = {
  accessToken: string
  onChanged: () => Promise<void>
}

type ItemOption = DebugItemListResponse[number]
type EquipmentOption = DebugEquipmentListResponse[number]

export default function DebugItemsSection({ accessToken, onChanged }: DebugItemsSectionProps) {
  const { data: items = [], isLoading: isItemsLoading } = useSWR(['debug-items'], () => getDebugItems(accessToken), {
    revalidateOnFocus: false,
  })
  const { data: equipments = [], isLoading: isEquipmentsLoading } = useSWR(
    ['debug-equipments'],
    () => getDebugEquipments(accessToken),
    { revalidateOnFocus: false },
  )
  const [selectedItem, setSelectedItem] = useState<ItemOption | null>(null)
  const [quantity, setQuantity] = useState('1')
  const [selectedEquipment, setSelectedEquipment] = useState<EquipmentOption | null>(null)
  const [count, setCount] = useState('1')
  const [plusValue, setPlusValue] = useState('0')
  const { isRunning, message, run } = useDebugAction()

  async function handleGrantItem(): Promise<void> {
    if (!selectedItem) {
      return
    }

    let owned = 0
    await run(
      async () => {
        const parsed = debugGrantItemRequestSchema.safeParse({
          itemId: selectedItem.itemId,
          quantity: Number(quantity),
        })
        if (!parsed.success) {
          throw new Error(parsed.error.issues[0]?.message)
        }

        owned = await grantDebugItem(parsed.data, accessToken)
        await onChanged()
      },
      () => locale.items.itemDone.replace('{name}', selectedItem.name).replace('{quantity}', String(owned)),
    )
  }

  async function handleGrantEquipment(): Promise<void> {
    if (!selectedEquipment) {
      return
    }

    let granted = 0
    await run(
      async () => {
        const parsed = debugGrantEquipmentRequestSchema.safeParse({
          equipmentId: selectedEquipment.equipmentId,
          count: Number(count),
          plusValue: Number(plusValue),
        })
        if (!parsed.success) {
          throw new Error(parsed.error.issues[0]?.message)
        }

        granted = await grantDebugEquipment(parsed.data, accessToken)
        await onChanged()
      },
      () => locale.items.equipmentDone.replace('{name}', selectedEquipment.name).replace('{count}', String(granted)),
    )
  }

  return (
    <DebugSection title={locale.items.title} description={locale.items.description} message={message}>
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
        <Autocomplete
          options={items}
          loading={isItemsLoading}
          value={selectedItem}
          onChange={(_, value) => setSelectedItem(value)}
          getOptionLabel={(option) =>
            `${option.itemId}: ${option.name}（${locale.items.maxStack.replace('{maxStack}', String(option.maxStack))}）`
          }
          isOptionEqualToValue={(option, value) => option.itemId === value.itemId}
          renderInput={(params) => <TextField {...params} label={locale.items.itemLabel} size="small" />}
          sx={{ flex: 1, minWidth: 240 }}
        />
        <TextField
          label={locale.items.quantityLabel}
          type="number"
          size="small"
          value={quantity}
          onChange={(event) => setQuantity(event.target.value)}
          slotProps={{ htmlInput: { min: 1, step: 1 } }}
          sx={{ width: 120 }}
        />
        <Button variant="contained" onClick={() => void handleGrantItem()} disabled={!selectedItem || isRunning}>
          {locale.items.itemSubmit}
        </Button>
      </Stack>

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
        <Autocomplete
          options={equipments}
          loading={isEquipmentsLoading}
          value={selectedEquipment}
          onChange={(_, value) => setSelectedEquipment(value)}
          getOptionLabel={(option) => `${option.equipmentId}: ${option.name}（${option.equipmentType}）`}
          isOptionEqualToValue={(option, value) => option.equipmentId === value.equipmentId}
          renderInput={(params) => <TextField {...params} label={locale.items.equipmentLabel} size="small" />}
          sx={{ flex: 1, minWidth: 240 }}
        />
        <TextField
          label={locale.items.countLabel}
          type="number"
          size="small"
          value={count}
          onChange={(event) => setCount(event.target.value)}
          slotProps={{ htmlInput: { min: 1, max: 20, step: 1 } }}
          sx={{ width: 140 }}
        />
        <TextField
          label={locale.items.plusLabel}
          type="number"
          size="small"
          value={plusValue}
          onChange={(event) => setPlusValue(event.target.value)}
          slotProps={{ htmlInput: { min: 0, max: 100, step: 1 } }}
          sx={{ width: 120 }}
        />
        <Button
          variant="contained"
          onClick={() => void handleGrantEquipment()}
          disabled={!selectedEquipment || isRunning}
        >
          {locale.items.equipmentSubmit}
        </Button>
      </Stack>
    </DebugSection>
  )
}
