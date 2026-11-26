import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material'
import { useMemo, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import useSWR, { useSWRConfig } from 'swr'
import {
  createMarketListing,
  deleteEquipment,
  deleteItemStack,
  getItems,
  getMarketListings,
  getMyMarketListings,
  purchaseMarketListing,
  synthesizeEquipment,
  useItem as consumeItem,
} from '@/api/item'
import { createPlayer, getPlayer, updatePlayerEquipment } from '@/api/player'
import { BagPanel, ItemDetailPanel, MirrorPanel, type ListingDraft } from '@/components/items/BagTab'
import { ForgeList, ForgePanel } from '@/components/items/ForgeTab'
import {
  canForge,
  getForgeMaterials,
  getForgeTarget,
  getInventoryKey,
  type ForgeGroup,
} from '@/components/items/itemsLogic'
import { ItemsTabs, type ItemsTabKey } from '@/components/items/ItemsParts'
import {
  compareStatusBonus,
  getEquipmentBonus,
  normalizeText,
  parsePositiveInteger,
} from '@/components/items/itemUtils'
import { MarketList, PurchasePanel, type MarketCategoryFilter, type MarketTabKey } from '@/components/items/MarketTab'
import HomeHeaderMenu from '@/components/home/HomeHeaderMenu'
import { HomeFilledIcon, HomeIcon } from '@/components/home/HomeIcons'
import { buildHomeMenu, type HomeMenuGroupKey, type HomeMenuItem } from '@/components/home/homeMenu'
import { itemsPanelColors as colors } from '@/constants/styles'
import { useAuth } from '@/contexts/useAuth'
import { useDebugStatus } from '@/hooks/useDebugStatus'
import { useMobileScrollToRef } from '@/hooks/useMobileScrollToRef'
import { resolveCharacterAssetPath } from '@/lib/assets'
import { INITIAL_PLAYER_NAME } from '@/lib/player'
import locale from '../../locale/items/Items.json'
import type { InventoryItemView, ItemEquipmentView, ItemStackView, MarketListingView } from '@/schema/item'

const PAGE_SIZE = 30

function matchesMarketSearch(listing: MarketListingView, searchText: string): boolean {
  if (!searchText) {
    return true
  }

  return [listing.itemName, listing.flavorText, listing.sellerName ?? ''].some((value) =>
    normalizeText(value).includes(searchText),
  )
}

function matchesMarketCategory(listing: MarketListingView, category: MarketCategoryFilter): boolean {
  return category === 'all' || listing.listingCategory === category
}

// 同じ装備の個体をまとめる（破損は合成に使えないので除く）。合成できるものを先に、名前順
function buildForgeGroups(equipments: ItemEquipmentView[]): ForgeGroup[] {
  const byId = new Map<number, ItemEquipmentView[]>()
  for (const equipment of equipments) {
    if (equipment.status === 'Broken') continue
    const copies = byId.get(equipment.equipmentId) ?? []
    copies.push(equipment)
    byId.set(equipment.equipmentId, copies)
  }

  return [...byId.entries()]
    .map(([equipmentId, copies]) => ({
      equipmentId,
      copies: [...copies].sort((a, b) => b.plusValue - a.plusValue),
    }))
    .sort((a, b) => {
      const okDiff = Number(canForge(b)) - Number(canForge(a))
      return okDiff !== 0 ? okDiff : a.copies[0].name.localeCompare(b.copies[0].name, 'ja')
    })
}

export default function Items() {
  const { session, isLoading } = useAuth()
  const theme = useTheme()
  const isDesktop = useMediaQuery(theme.breakpoints.up('sm'), { noSsr: true })
  const isWide = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true })
  const { canUseDebug } = useDebugStatus()
  const { mutate: mutateCache } = useSWRConfig()
  const [openMenuGroup, setOpenMenuGroup] = useState<HomeMenuGroupKey | null>(null)

  const [tab, setTab] = useState<ItemsTabKey>('bag')
  const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; message: string } | null>(null)
  const [busyKey, setBusyKey] = useState<string | null>(null)
  // スマホで品物を選んだら、下の札までスクロールする
  const [selectionCount, setSelectionCount] = useState(0)

  // 持ち物
  const [selectedBagKey, setSelectedBagKey] = useState<string | null>(null)
  const [listingKey, setListingKey] = useState<string | null>(null)
  const [listingDraft, setListingDraft] = useState<ListingDraft>({ unitPrice: '10', quantity: '1' })
  const [pendingUseItem, setPendingUseItem] = useState<ItemStackView | null>(null)

  // 合成
  const [selectedForgeId, setSelectedForgeId] = useState<number | null>(null)
  const [materialIndex, setMaterialIndex] = useState(0)

  // マーケット
  const [marketTab, setMarketTab] = useState<MarketTabKey>('public')
  const [marketPage, setMarketPage] = useState(1)
  const [myMarketPage, setMyMarketPage] = useState(1)
  const [marketSearch, setMarketSearch] = useState('')
  const [marketCategory, setMarketCategory] = useState<MarketCategoryFilter>('all')
  const [selectedListingId, setSelectedListingId] = useState<string | null>(null)
  const [purchaseQuantity, setPurchaseQuantity] = useState(1)

  const detailRef = useRef<HTMLDivElement | null>(null)
  useMobileScrollToRef(detailRef, { enabled: selectionCount > 0, offsetTop: 12, trigger: selectionCount })

  const itemsSWRKey = session?.user.id ? ([`items`, session.user.id] as const) : null
  const {
    data: items,
    error: itemsError,
    isLoading: isItemsLoading,
    mutate: mutateItems,
  } = useSWR(itemsSWRKey, async () => {
    if (!session?.access_token) {
      throw new Error(locale.sessionInfoMissing)
    }

    try {
      return await getItems(session.access_token)
    } catch (error) {
      const message = error instanceof Error ? error.message : ''
      if (!message.includes(locale.playerNotFoundMessage)) {
        throw error
      }

      await createPlayer({ userName: INITIAL_PLAYER_NAME }, session.access_token)
      return getItems(session.access_token)
    }
  })

  const marketSWRKey = session?.user.id ? ([`market`, session.user.id] as const) : null
  const {
    data: marketListings,
    error: marketError,
    isLoading: isMarketLoading,
    mutate: mutateMarket,
  } = useSWR(marketSWRKey, async () => {
    if (!session?.access_token) {
      throw new Error(locale.sessionInfoMissing)
    }

    return getMarketListings(session.access_token)
  })

  const myListingsSWRKey = session?.user.id ? ([`my-market`, session.user.id] as const) : null
  const {
    data: myListings,
    error: myListingsError,
    isLoading: isMyListingsLoading,
    mutate: mutateMyListings,
  } = useSWR(myListingsSWRKey, async () => {
    if (!session?.access_token) {
      throw new Error(locale.sessionInfoMissing)
    }

    return getMyMarketListings(session.access_token)
  })

  // 姿見に出すキャラの絵
  const playerSWRKey = session?.user.id ? ([`items-player`, session.user.id] as const) : null
  const { data: player } = useSWR(playerSWRKey, async () => {
    if (!session?.access_token) {
      throw new Error(locale.sessionInfoMissing)
    }

    return getPlayer(session.access_token)
  })

  const equippedItems = useMemo(() => items?.equippedItems ?? [], [items])
  const inventoryItems = useMemo(() => items?.inventoryItems ?? [], [items])
  const equippedWeapon = equippedItems.find((item) => item.equipmentType === 'Weapon')
  const equippedArmor = equippedItems.find((item) => item.equipmentType === 'Armor')

  // ---- 持ち物 ----
  // 空きマスは PC だけ（スマホは縦に長くなりすぎるため、所持数の表示だけにする）
  const emptySlotCount = isDesktop && items ? Math.max(0, items.capacity - inventoryItems.length) : 0

  // 選んだ品がなくなったら（使い切った・売ったなど）、袋の先頭の品を選び直す
  const selectedBagItem: InventoryItemView | null = useMemo(() => {
    const all: InventoryItemView[] = [...inventoryItems, ...equippedItems]
    const found = selectedBagKey ? all.find((item) => getInventoryKey(item) === selectedBagKey) : undefined
    return found ?? inventoryItems[0] ?? equippedItems[0] ?? null
  }, [equippedItems, inventoryItems, selectedBagKey])
  const selectedBagKeyResolved = selectedBagItem ? getInventoryKey(selectedBagItem) : null
  const isSelectedEquipped =
    selectedBagItem?.kind === 'equipment' &&
    equippedItems.some((item) => item.playerEquipmentId === selectedBagItem.playerEquipmentId)

  const statusChanges = useMemo(() => {
    if (!selectedBagItem || selectedBagItem.kind !== 'equipment' || isSelectedEquipped) {
      return null
    }
    const current = selectedBagItem.equipmentType === 'Weapon' ? equippedWeapon : equippedArmor
    return compareStatusBonus(current ? getEquipmentBonus(current) : null, getEquipmentBonus(selectedBagItem))
  }, [equippedArmor, equippedWeapon, isSelectedEquipped, selectedBagItem])

  function selectBagItem(item: InventoryItemView): void {
    setSelectedBagKey(getInventoryKey(item))
    setListingKey(null)
    setSelectionCount((count) => count + 1)
  }

  // ---- 合成 ----
  const forgeGroups = useMemo(
    () =>
      buildForgeGroups([
        ...equippedItems,
        ...inventoryItems.filter((item): item is ItemEquipmentView => item.kind === 'equipment'),
      ]),
    [equippedItems, inventoryItems],
  )
  const selectedForgeGroup =
    forgeGroups.find((group) => group.equipmentId === selectedForgeId && canForge(group)) ??
    forgeGroups.find((group) => canForge(group)) ??
    null
  const forgeTarget = selectedForgeGroup ? getForgeTarget(selectedForgeGroup) : undefined
  const forgeMaterials = selectedForgeGroup ? getForgeMaterials(selectedForgeGroup) : []
  const forgeMaterial = forgeMaterials.length > 0 ? forgeMaterials[materialIndex % forgeMaterials.length] : undefined

  // ---- マーケット ----
  const normalizedMarketSearch = useMemo(() => normalizeText(marketSearch), [marketSearch])
  const currentMarketListings = useMemo(
    () =>
      (marketTab === 'public' ? (marketListings ?? []) : (myListings ?? [])).filter(
        (item) => matchesMarketSearch(item, normalizedMarketSearch) && matchesMarketCategory(item, marketCategory),
      ),
    [marketCategory, marketListings, marketTab, myListings, normalizedMarketSearch],
  )
  const marketPageCount = Math.max(1, Math.ceil(currentMarketListings.length / PAGE_SIZE))
  const visibleMarketPage = Math.min(marketTab === 'public' ? marketPage : myMarketPage, marketPageCount)
  const pagedMarketListings = currentMarketListings.slice(
    (visibleMarketPage - 1) * PAGE_SIZE,
    visibleMarketPage * PAGE_SIZE,
  )
  const selectedListing =
    pagedMarketListings.find((listing) => listing.listingId === selectedListingId) ?? pagedMarketListings[0] ?? null
  const clampedPurchaseQuantity = selectedListing
    ? Math.min(Math.max(1, purchaseQuantity), Math.max(1, selectedListing.quantity))
    : 1

  function resetMarketPage(): void {
    setMarketPage(1)
    setMyMarketPage(1)
  }

  // ---- 操作 ----
  async function refreshAll(): Promise<void> {
    await Promise.all([
      mutateItems(),
      mutateMarket(),
      mutateMyListings(),
      session?.user.id ? mutateCache([`player`, session.user.id]) : Promise.resolve(),
      session?.user.id ? mutateCache([`items-player`, session.user.id]) : Promise.resolve(),
      mutateCache([`quest-stages`]),
    ])
  }

  async function runAction(actionKey: string, action: () => Promise<{ message: string }>): Promise<boolean> {
    setBusyKey(actionKey)
    setFeedback(null)
    try {
      const result = await action()
      setFeedback({ type: 'success', message: result.message })
      await refreshAll()
      return true
    } catch (error) {
      setFeedback({
        type: 'error',
        message: error instanceof Error ? error.message : locale.actionFailed,
      })
      return false
    } finally {
      setBusyKey(null)
    }
  }

  function handleUseItem(item: ItemStackView): void {
    if (item.effectType === 'StatBoost') {
      void handleUseItemConfirmed(item)
    } else {
      setPendingUseItem(item)
    }
  }

  async function handleUseItemConfirmed(item: ItemStackView): Promise<void> {
    if (!session?.access_token) return
    setPendingUseItem(null)

    await runAction(`use:${item.itemStackId}`, () =>
      consumeItem(item.itemStackId, { quantity: 1 }, session.access_token),
    )
  }

  async function handleDelete(item: InventoryItemView): Promise<void> {
    if (!session?.access_token) return

    if (item.kind === 'item') {
      await runAction(`delete-item:${item.itemStackId}`, () =>
        deleteItemStack(item.itemStackId, { quantity: 1 }, session.access_token),
      )
    } else {
      await runAction(`delete-equipment:${item.playerEquipmentId}`, () =>
        deleteEquipment(item.playerEquipmentId, session.access_token),
      )
    }
  }

  async function handleEquip(item: ItemEquipmentView): Promise<void> {
    if (!session?.access_token) return

    await runAction(`equip:${item.playerEquipmentId}`, () =>
      updatePlayerEquipment(
        { equipmentType: item.equipmentType, playerEquipmentId: item.playerEquipmentId },
        session.access_token,
      ),
    )
  }

  async function handleList(item: InventoryItemView): Promise<void> {
    if (!session?.access_token) return

    const unitPrice = parsePositiveInteger(listingDraft.unitPrice, 10)
    const succeeded = await runAction(`list:${getInventoryKey(item)}`, () =>
      createMarketListing(
        item.kind === 'item'
          ? {
              itemStackId: item.itemStackId,
              playerEquipmentId: null,
              quantity: parsePositiveInteger(listingDraft.quantity, 1),
              unitPrice,
            }
          : { playerEquipmentId: item.playerEquipmentId, itemStackId: null, quantity: 1, unitPrice },
        session.access_token,
      ),
    )
    if (succeeded) {
      setListingKey(null)
    }
  }

  async function handleSynthesize(): Promise<void> {
    if (!session?.access_token || !forgeTarget || !forgeMaterial) return

    await runAction(`synthesize:${forgeTarget.playerEquipmentId}`, () =>
      synthesizeEquipment(
        forgeTarget.playerEquipmentId,
        { sourcePlayerEquipmentId: forgeMaterial.playerEquipmentId },
        session.access_token,
      ),
    )
    setMaterialIndex(0)
  }

  async function handlePurchase(listing: MarketListingView): Promise<void> {
    if (!session?.access_token) return

    const succeeded = await runAction(`purchase:${listing.listingId}`, () =>
      purchaseMarketListing(listing.listingId, { quantity: clampedPurchaseQuantity }, session.access_token),
    )
    if (succeeded) {
      setPurchaseQuantity(1)
    }
  }

  if (isLoading) {
    return (
      <Box minHeight="100vh" display="grid" sx={{ placeItems: 'center' }}>
        <CircularProgress size={20} />
      </Box>
    )
  }

  // PC のヘッダーメニュー。ホームと同じ項目の先頭に「ホーム」を足し、今いるアイテムを帯の色で示す
  const homeMenu = buildHomeMenu({
    userId: session?.user.id ?? null,
    tutorialStep: null,
    showRebirthTutorial: false,
    showSurveyGuide: false,
    onSurveyClick: () => {},
    canUseDebug,
  })
  const homeMenuItem: HomeMenuItem = { key: 'home', label: locale.backToHome, icon: <HomeIcon />, to: '/' }

  // スマホはヘッダーがないので、タブの左にホームへ戻るボタンを置く
  const mobileHomeButton = isDesktop ? undefined : (
    <IconButton component={Link} to="/" aria-label={locale.backToHome} sx={{ color: '#ffffff', width: 44, height: 44 }}>
      <HomeFilledIcon />
    </IconButton>
  )

  const busy = busyKey !== null
  const fill = isWide

  let leftPanel
  let rightPanel
  if (tab === 'bag') {
    leftPanel = (
      <BagPanel
        items={inventoryItems}
        emptySlotCount={emptySlotCount}
        selectedKey={selectedBagKeyResolved}
        onSelect={selectBagItem}
        usedSlots={items?.usedSlots}
        capacity={items?.capacity}
        gold={items?.gold}
        isLoading={isItemsLoading}
        fill={fill}
      />
    )
    rightPanel = (
      <>
        <MirrorPanel
          weapon={equippedWeapon}
          armor={equippedArmor}
          playerImageSrc={resolveCharacterAssetPath(player?.imagePath ?? null)}
          selectedKey={selectedBagKeyResolved}
          onSelect={selectBagItem}
          changes={statusChanges}
        />
        <ItemDetailPanel
          item={selectedBagItem}
          isEquipped={isSelectedEquipped}
          isListing={listingKey !== null && listingKey === selectedBagKeyResolved}
          listingDraft={listingDraft}
          onListingDraftChange={setListingDraft}
          onEquip={() => selectedBagItem?.kind === 'equipment' && void handleEquip(selectedBagItem)}
          onUse={() => selectedBagItem?.kind === 'item' && handleUseItem(selectedBagItem)}
          onStartListing={() => {
            setListingDraft({ unitPrice: '10', quantity: '1' })
            setListingKey(selectedBagKeyResolved)
          }}
          onCancelListing={() => setListingKey(null)}
          onList={() => selectedBagItem && void handleList(selectedBagItem)}
          onDelete={() => selectedBagItem && void handleDelete(selectedBagItem)}
          busy={busy}
          fill={fill}
        />
      </>
    )
  } else if (tab === 'forge') {
    leftPanel = (
      <ForgeList
        groups={forgeGroups}
        selectedId={selectedForgeGroup?.equipmentId ?? null}
        onSelect={(group) => {
          setSelectedForgeId(group.equipmentId)
          setMaterialIndex(0)
          setSelectionCount((count) => count + 1)
        }}
        isLoading={isItemsLoading}
        fill={fill}
      />
    )
    rightPanel = (
      <ForgePanel
        target={forgeTarget}
        material={forgeMaterial}
        gold={items?.gold}
        canChangeMaterial={forgeMaterials.length > 1}
        onChangeMaterial={() => setMaterialIndex((index) => index + 1)}
        onSynthesize={() => void handleSynthesize()}
        busy={busy}
        fill={fill}
      />
    )
  } else {
    leftPanel = (
      <MarketList
        tab={marketTab}
        onTabChange={(next) => {
          setMarketTab(next)
          setSelectedListingId(null)
        }}
        search={marketSearch}
        onSearchChange={(value) => {
          setMarketSearch(value)
          resetMarketPage()
        }}
        category={marketCategory}
        onCategoryChange={(value) => {
          setMarketCategory(value)
          resetMarketPage()
        }}
        listings={pagedMarketListings}
        selectedId={selectedListing?.listingId ?? null}
        onSelect={(listing) => {
          setSelectedListingId(listing.listingId)
          setPurchaseQuantity(1)
          setSelectionCount((count) => count + 1)
        }}
        page={visibleMarketPage}
        pageCount={marketPageCount}
        onPageChange={(next) => (marketTab === 'public' ? setMarketPage(next) : setMyMarketPage(next))}
        isLoading={marketTab === 'public' ? isMarketLoading : isMyListingsLoading}
        isFiltered={normalizedMarketSearch !== '' || marketCategory !== 'all'}
        fill={fill}
      />
    )
    rightPanel = (
      <PurchasePanel
        listing={selectedListing}
        isMine={marketTab === 'mine'}
        quantity={clampedPurchaseQuantity}
        onQuantityChange={setPurchaseQuantity}
        gold={items?.gold}
        onPurchase={() => selectedListing && void handlePurchase(selectedListing)}
        busy={busy}
        fill={fill}
      />
    )
  }

  const loadError = itemsError ?? (tab === 'market' ? (marketTab === 'public' ? marketError : myListingsError) : null)

  return (
    <Box
      sx={{
        minHeight: '100dvh',
        display: 'flex',
        flexDirection: 'column',
        background: `linear-gradient(${colors.topBand}, ${colors.topBand}) top / 100% ${isDesktop ? 260 : 160}px no-repeat, ${colors.ground}`,
        fontFamily: 'inherit',
      }}
    >
      {isDesktop ? (
        <HomeHeaderMenu
          primary={[homeMenuItem, ...homeMenu.primary]}
          groups={homeMenu.groups}
          openGroup={openMenuGroup}
          lockedGroup={null}
          onToggleGroup={(key) => setOpenMenuGroup((current) => (current === key ? null : key))}
          onCloseGroup={() => setOpenMenuGroup(null)}
          currentKey="items"
          currentColor={colors.leather}
        />
      ) : null}
      <Box
        sx={{
          flex: 1,
          // PC は袋の中でスクロールさせるため、画面の高さに合わせる（低すぎる画面ではページごとスクロール）
          height: { md: 'calc(100dvh - 57px)' },
          minHeight: { md: 720 },
          boxSizing: 'border-box',
          px: { xs: 1.5, sm: 4 },
          pt: { xs: 1.5, sm: 2.5 },
          pb: { xs: 3, sm: 4 },
          display: 'flex',
          flexDirection: 'column',
          gap: 1.5,
        }}
      >
        {feedback ? <Alert severity={feedback.type}>{feedback.message}</Alert> : null}
        {loadError ? <Alert severity="warning">{loadError.message}</Alert> : null}
        <Box
          sx={{
            flex: 1,
            minHeight: 0,
            display: 'grid',
            gap: { xs: 2.5, md: 4 },
            gridTemplateColumns: { xs: 'minmax(0, 1fr)', md: 'minmax(0, 1fr) 520px', lg: 'minmax(0, 1fr) 620px' },
            alignContent: { xs: 'start', md: 'stretch' },
          }}
        >
          <Box sx={{ display: 'flex', flexDirection: 'column', minHeight: 0, minWidth: 0 }}>
            <ItemsTabs value={tab} onChange={setTab} startAction={mobileHomeButton} />
            {leftPanel}
          </Box>
          <Box
            ref={detailRef}
            sx={{
              display: 'flex',
              flexDirection: 'column',
              gap: 2.5,
              minHeight: 0,
              minWidth: 0,
              pb: 0.75,
            }}
          >
            {rightPanel}
          </Box>
        </Box>
      </Box>

      <Dialog open={pendingUseItem !== null} onClose={() => setPendingUseItem(null)} maxWidth="xs" fullWidth>
        <DialogTitle>{locale.useConfirmTitle}</DialogTitle>
        <DialogContent>
          <Typography variant="body2">
            {locale.useConfirmMessage.replace('{{name}}', pendingUseItem?.name ?? '')}
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setPendingUseItem(null)} disabled={busy}>
            {locale.actionCancel}
          </Button>
          <Button
            onClick={() => pendingUseItem && void handleUseItemConfirmed(pendingUseItem)}
            disabled={busy}
            variant="contained"
          >
            {locale.useConfirmAction}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}
