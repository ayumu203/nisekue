import { applyPlusValue, formatPlusValue, formatStatusBonus } from './itemUtils'
import locale from '../../../locale/items/Items.json'
import type { InventoryItemView, ItemEquipmentView, MarketListingView } from '@/schema/item'

export function getInventoryKey(item: InventoryItemView): string {
  return item.kind === 'item' ? item.itemStackId : item.playerEquipmentId
}

// 強化値の上限（サーバーの PlayerEquipment.MaxPlusValue）
export const MAX_PLUS_VALUE = 100

// 同じ装備（equipmentId）の個体をまとめたもの。強化値の高い順
export type ForgeGroup = {
  equipmentId: number
  copies: ItemEquipmentView[]
}

// 合成先は強化値がいちばん高い個体。素材はそれ以外（強化値の低い順）
export function getForgeTarget(group: ForgeGroup): ItemEquipmentView {
  return group.copies[0]
}

export function getForgeMaterials(group: ForgeGroup): ItemEquipmentView[] {
  return group.copies.slice(1).reverse()
}

export function canForge(group: ForgeGroup): boolean {
  return group.copies.length >= 2 && getForgeTarget(group).plusValue < MAX_PLUS_VALUE
}

const categoryLabelMap: Record<MarketListingView['listingCategory'], string> = {
  Weapon: locale.categoryWeapon,
  Armor: locale.categoryArmor,
  Item: locale.categoryItem,
  Map: locale.categoryMap,
}

export function getListingTitle(listing: MarketListingView): string {
  const plus = listing.equipmentDetail ? formatPlusValue(listing.equipmentDetail.plusValue) : ''
  return `${listing.itemName} ${plus}`.trim()
}

// 「アイテム」「武器・STR+32」のような種類と効果
export function getListingSummary(listing: MarketListingView): string {
  const category = categoryLabelMap[listing.listingCategory] ?? listing.listingCategory
  const detail = listing.equipmentDetail
  if (!detail) {
    return category
  }
  const effect = formatStatusBonus({ statusBonus: applyPlusValue(detail.statusBonus, detail.plusValue) })
  return effect === '-' ? category : `${category}・${effect}`
}
