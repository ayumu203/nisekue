import {
  Alert,
  Box,
  CircularProgress,
  Container,
  Pagination,
  Paper,
  Stack,
  Tab,
  Tabs,
  TextField,
  Typography,
} from '@mui/material'
import { useEffect, useMemo, useState } from 'react'
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
import { createPlayer, updatePlayerEquipment } from '@/api/player'
import BeginnerGuide from '@/components/common/BeginnerGuide'
import HomeNavIconButton from '@/components/common/HomeNavIconButton'
import { ConsumableCard } from '@/components/items/ConsumableCard'
import { EquipmentCard } from '@/components/items/EquipmentCard'
import { ItemSummary } from '@/components/items/ItemSummary'
import { ControlFrame, PageFrame, SectionFrame } from '@/components/items/ItemsLayout'
import { itemInnerPanelSx, inputSx, tabsPaperSx, tabsSx } from '@/components/items/ItemsConstants'
import { normalizeText, parsePositiveInteger } from '@/components/items/itemUtils'
import { MarketCard } from '@/components/items/MarketCard'
import { useAuth } from '@/contexts/useAuth'
import { outerPagePaperSx } from '@/constants/styles'
import { beginnerGuides } from '@/lib/beginnerGuides'
import { INITIAL_PLAYER_NAME } from '@/lib/player'
import locale from '../../locale/items/Items.json'
import type { InventoryItemView, ItemEquipmentView, ItemStackView, MarketListingView } from '@/schema/item'

const PAGE_SIZE = 30

type PrimaryTab = 'items' | 'market'
type MarketTab = 'public' | 'mine'

function paginate<T>(items: T[], page: number): T[] {
  const start = (page - 1) * PAGE_SIZE
  return items.slice(start, start + PAGE_SIZE)
}

function matchesMarketSearch(listing: MarketListingView, searchText: string): boolean {
  if (!searchText) {
    return true
  }

  return [listing.itemName, listing.flavorText, listing.sellerName ?? ''].some((value) =>
    normalizeText(value).includes(searchText),
  )
}

export default function Items() {
  const { session, isLoading } = useAuth()
  const { mutate: mutateCache } = useSWRConfig()
  const [primaryTab, setPrimaryTab] = useState<PrimaryTab>('items')
  const [marketTab, setMarketTab] = useState<MarketTab>('public')
  const [marketPage, setMarketPage] = useState(1)
  const [myMarketPage, setMyMarketPage] = useState(1)
  const [marketSearch, setMarketSearch] = useState('')
  const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; message: string } | null>(null)
  const [busyKey, setBusyKey] = useState<string | null>(null)
  const [quantities, setQuantities] = useState<Record<string, string>>({})
  const [unitPrices, setUnitPrices] = useState<Record<string, string>>({})

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

  const equippedItems = useMemo(() => items?.equippedItems ?? [], [items])
  const normalizedMarketSearch = useMemo(() => normalizeText(marketSearch), [marketSearch])

  const inventoryEquipments = useMemo(() => {
    return (items?.inventoryItems ?? []).filter((item): item is ItemEquipmentView => item.kind === 'equipment')
  }, [items])

  const inventoryConsumables = useMemo(() => {
    return (items?.inventoryItems ?? []).filter((item): item is ItemStackView => item.kind === 'item')
  }, [items])

  const publicMarketListings = useMemo(
    () => (marketListings ?? []).filter((item) => matchesMarketSearch(item, normalizedMarketSearch)),
    [marketListings, normalizedMarketSearch],
  )
  const ownMarketListings = useMemo(
    () => (myListings ?? []).filter((item) => matchesMarketSearch(item, normalizedMarketSearch)),
    [myListings, normalizedMarketSearch],
  )

  const currentMarketListings = marketTab === 'public' ? publicMarketListings : ownMarketListings
  const marketPageCount = Math.max(1, Math.ceil(currentMarketListings.length / PAGE_SIZE))
  const visibleMarketPage = marketTab === 'public' ? marketPage : myMarketPage
  const pagedMarketListings = useMemo(
    () => paginate(currentMarketListings, visibleMarketPage),
    [currentMarketListings, visibleMarketPage],
  )

  useEffect(() => {
    if (marketPage > Math.max(1, Math.ceil(publicMarketListings.length / PAGE_SIZE))) {
      setMarketPage(1)
    }
  }, [marketPage, publicMarketListings.length])

  useEffect(() => {
    if (myMarketPage > Math.max(1, Math.ceil(ownMarketListings.length / PAGE_SIZE))) {
      setMyMarketPage(1)
    }
  }, [myMarketPage, ownMarketListings.length])

  async function refreshAll(): Promise<void> {
    await Promise.all([
      mutateItems(),
      mutateMarket(),
      mutateMyListings(),
      session?.user.id ? mutateCache([`player`, session.user.id]) : Promise.resolve(),
    ])
  }

  function setQuantityValue(key: string, value: string): void {
    setQuantities((current) => ({ ...current, [key]: value }))
  }

  function setUnitPriceValue(key: string, value: string): void {
    setUnitPrices((current) => ({ ...current, [key]: value }))
  }

  async function runAction(actionKey: string, action: () => Promise<{ message: string }>): Promise<void> {
    setBusyKey(actionKey)
    setFeedback(null)
    try {
      const result = await action()
      setFeedback({ type: 'success', message: result.message })
      await refreshAll()
    } catch (error) {
      setFeedback({
        type: 'error',
        message: error instanceof Error ? error.message : locale.actionFailed,
      })
    } finally {
      setBusyKey(null)
    }
  }

  async function handleUseItem(item: ItemStackView): Promise<void> {
    if (!session?.access_token) return

    await runAction(`use:${item.itemStackId}`, () =>
      consumeItem(
        item.itemStackId,
        { quantity: parsePositiveInteger(quantities[item.itemStackId], 1) },
        session.access_token,
      ),
    )
  }

  async function handleDeleteItem(item: ItemStackView): Promise<void> {
    if (!session?.access_token) return

    await runAction(`delete-item:${item.itemStackId}`, () =>
      deleteItemStack(
        item.itemStackId,
        { quantity: parsePositiveInteger(quantities[`delete:${item.itemStackId}`] ?? quantities[item.itemStackId], 1) },
        session.access_token,
      ),
    )
  }

  async function handleDeleteEquipment(item: ItemEquipmentView): Promise<void> {
    if (!session?.access_token) return

    await runAction(`delete-equipment:${item.playerEquipmentId}`, () =>
      deleteEquipment(item.playerEquipmentId, session.access_token),
    )
  }

  async function handleSynthesize(item: ItemEquipmentView): Promise<void> {
    if (!session?.access_token) return

    await runAction(`synthesize:${item.playerEquipmentId}`, () =>
      synthesizeEquipment(item.playerEquipmentId, session.access_token),
    )
  }

  async function handleEquip(item: ItemEquipmentView): Promise<void> {
    if (!session?.access_token) return

    await runAction(`equip:${item.playerEquipmentId}`, () =>
      updatePlayerEquipment(
        {
          equipmentType: item.equipmentType,
          playerEquipmentId: item.playerEquipmentId,
        },
        session.access_token,
      ),
    )
  }

  async function handleListInventoryItem(item: InventoryItemView): Promise<void> {
    if (!session?.access_token) return

    const key = item.kind === 'item' ? item.itemStackId : item.playerEquipmentId
    await runAction(`list:${key}`, () =>
      createMarketListing(
        item.kind === 'item'
          ? {
              itemStackId: item.itemStackId,
              playerEquipmentId: null,
              quantity: parsePositiveInteger(quantities[key], 1),
              unitPrice: parsePositiveInteger(unitPrices[`price:${key}`], 10),
            }
          : {
              playerEquipmentId: item.playerEquipmentId,
              itemStackId: null,
              quantity: 1,
              unitPrice: parsePositiveInteger(unitPrices[`price:${key}`], 10),
            },
        session.access_token,
      ),
    )
  }

  async function handlePurchase(listing: MarketListingView): Promise<void> {
    if (!session?.access_token) return

    await runAction(`purchase:${listing.listingId}`, () =>
      purchaseMarketListing(
        listing.listingId,
        { quantity: parsePositiveInteger(quantities[`purchase:${listing.listingId}`], 1) },
        session.access_token,
      ),
    )
  }

  if (isLoading) {
    return (
      <Box minHeight="100vh" display="grid" sx={{ placeItems: 'center' }}>
        <CircularProgress size={20} />
      </Box>
    )
  }

  return (
    <Container maxWidth="lg" sx={{ py: { xs: 2, sm: 6 } }}>
      <Paper elevation={2} sx={outerPagePaperSx}>
        <Stack spacing={2.5}>
          <PageFrame>
            <ControlFrame>
              <Stack spacing={2}>
                <Stack direction="row" alignItems="center" spacing={1.5}>
                  <HomeNavIconButton ariaLabel={locale.backToHome} />
                  <Box>
                    <Typography variant="overline" sx={{ letterSpacing: '0.16em', color: 'rgba(255,255,255,0.66)' }}>
                      ITEM CONTROL
                    </Typography>
                    <Typography variant="h4" fontWeight={900} color="#ffffff">
                      {locale.title}
                    </Typography>
                  </Box>
                </Stack>
                <BeginnerGuide userId={session?.user.id} guide={beginnerGuides.items} inverted />
              </Stack>
            </ControlFrame>

            {feedback ? <Alert severity={feedback.type}>{feedback.message}</Alert> : null}
            {itemsError ? <Alert severity="warning">{itemsError.message}</Alert> : null}
            {marketError ? <Alert severity="warning">{marketError.message}</Alert> : null}
            {myListingsError ? <Alert severity="warning">{myListingsError.message}</Alert> : null}

            <ControlFrame>
              <Stack spacing={2}>
                <ItemSummary usedSlots={items?.usedSlots} capacity={items?.capacity} gold={items?.gold} />

                <Paper variant="outlined" sx={tabsPaperSx}>
                  <Tabs
                    value={primaryTab}
                    onChange={(_, value: PrimaryTab) => setPrimaryTab(value)}
                    variant="fullWidth"
                    sx={tabsSx}
                  >
                    <Tab value="items" label={locale.itemsTab} />
                    <Tab value="market" label={locale.marketTab} />
                  </Tabs>
                </Paper>
              </Stack>
            </ControlFrame>

            {primaryTab === 'items' ? (
              <Stack spacing={2}>
                <SectionFrame title={locale.equippedTitle}>
                  <Stack spacing={2}>
                    {isItemsLoading ? (
                      <Stack direction="row" spacing={1} alignItems="center">
                        <CircularProgress size={16} />
                        <Typography variant="body2">{locale.loading}</Typography>
                      </Stack>
                    ) : equippedItems.length > 0 ? (
                      <Box
                        sx={{
                          display: 'grid',
                          gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, minmax(0, 1fr))' },
                          gap: 1.5,
                        }}
                      >
                        {equippedItems.map((item) => (
                          <EquipmentCard
                            key={item.playerEquipmentId}
                            item={item}
                            quantityValue="1"
                            unitPriceValue="10"
                            onQuantityChange={() => undefined}
                            onUnitPriceChange={() => undefined}
                            onSynthesize={() => undefined}
                            onDelete={() => undefined}
                            onList={() => undefined}
                            actionDisabled
                            showActions={false}
                          />
                        ))}
                      </Box>
                    ) : (
                      <Typography variant="body2">{locale.emptyEquipped}</Typography>
                    )}
                  </Stack>
                </SectionFrame>

                <SectionFrame title={locale.inventoryEquipmentsTitle}>
                  <Stack spacing={2}>
                    {isItemsLoading ? (
                      <Stack direction="row" spacing={1} alignItems="center">
                        <CircularProgress size={16} />
                        <Typography variant="body2">{locale.loading}</Typography>
                      </Stack>
                    ) : inventoryEquipments.length > 0 ? (
                      <Box
                        sx={{
                          display: 'grid',
                          gridTemplateColumns: {
                            xs: 'repeat(2, minmax(0, 1fr))',
                            sm: 'repeat(3, minmax(0, 1fr))',
                            xl: 'repeat(4, minmax(0, 1fr))',
                          },
                          gap: 1.25,
                        }}
                      >
                        {inventoryEquipments.map((item) => {
                          const key = item.playerEquipmentId
                          return (
                            <EquipmentCard
                              key={key}
                              item={item}
                              quantityValue={quantities[key] ?? '1'}
                              unitPriceValue={unitPrices[`price:${key}`] ?? '10'}
                              onQuantityChange={(value) => setQuantityValue(key, value)}
                              onUnitPriceChange={(value) => setUnitPriceValue(`price:${key}`, value)}
                              onSynthesize={() => void handleSynthesize(item)}
                              onDelete={() => void handleDeleteEquipment(item)}
                              onList={() => void handleListInventoryItem(item)}
                              onEquip={() => void handleEquip(item)}
                              actionDisabled={busyKey !== null}
                            />
                          )
                        })}
                      </Box>
                    ) : (
                      <Typography variant="body2">{locale.emptyInventory}</Typography>
                    )}
                  </Stack>
                </SectionFrame>

                <SectionFrame title={locale.inventoryConsumablesTitle}>
                  <Stack spacing={2}>
                    {isItemsLoading ? (
                      <Stack direction="row" spacing={1} alignItems="center">
                        <CircularProgress size={16} />
                        <Typography variant="body2">{locale.loading}</Typography>
                      </Stack>
                    ) : inventoryConsumables.length > 0 ? (
                      <Box
                        sx={{
                          display: 'grid',
                          gridTemplateColumns: {
                            xs: 'repeat(2, minmax(0, 1fr))',
                            sm: 'repeat(3, minmax(0, 1fr))',
                            xl: 'repeat(4, minmax(0, 1fr))',
                          },
                          gap: 1.25,
                        }}
                      >
                        {inventoryConsumables.map((item) => {
                          const key = item.itemStackId
                          return (
                            <ConsumableCard
                              key={key}
                              item={item}
                              quantityValue={quantities[key] ?? '1'}
                              unitPriceValue={unitPrices[`price:${key}`] ?? '10'}
                              onQuantityChange={(value) => setQuantityValue(key, value)}
                              onUnitPriceChange={(value) => setUnitPriceValue(`price:${key}`, value)}
                              onUse={() => void handleUseItem(item)}
                              onDelete={() => void handleDeleteItem(item)}
                              onList={() => void handleListInventoryItem(item)}
                              actionDisabled={busyKey !== null}
                            />
                          )
                        })}
                      </Box>
                    ) : (
                      <Typography variant="body2">{locale.emptyInventory}</Typography>
                    )}
                  </Stack>
                </SectionFrame>
              </Stack>
            ) : (
              <Stack spacing={2}>
                <ControlFrame>
                  <Stack spacing={2}>
                    <Paper variant="outlined" sx={{ ...itemInnerPanelSx, p: 2, borderRadius: 3 }}>
                      <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.5}>
                        <TextField
                          label={locale.searchLabel}
                          placeholder={locale.searchPlaceholderMarket}
                          value={marketSearch}
                          onChange={(event) => {
                            setMarketSearch(event.target.value)
                            setMarketPage(1)
                            setMyMarketPage(1)
                          }}
                          fullWidth
                          sx={inputSx}
                        />
                      </Stack>
                    </Paper>

                    <Paper variant="outlined" sx={tabsPaperSx}>
                      <Tabs
                        value={marketTab}
                        onChange={(_, value: MarketTab) => {
                          setMarketTab(value)
                        }}
                        variant="fullWidth"
                        sx={tabsSx}
                      >
                        <Tab value="public" label={locale.publicMarketTab} />
                        <Tab value="mine" label={locale.myListingsTab} />
                      </Tabs>
                    </Paper>
                  </Stack>
                </ControlFrame>

                <SectionFrame title={marketTab === 'public' ? locale.marketTitle : locale.myListingsTitle}>
                  <Stack spacing={2}>
                    {(marketTab === 'public' ? isMarketLoading : isMyListingsLoading) ? (
                      <Stack direction="row" spacing={1} alignItems="center">
                        <CircularProgress size={16} />
                        <Typography variant="body2">{locale.marketLoading}</Typography>
                      </Stack>
                    ) : pagedMarketListings.length > 0 ? (
                      <Box
                        sx={{
                          display: 'grid',
                          gridTemplateColumns: {
                            xs: 'repeat(2, minmax(0, 1fr))',
                            sm: 'repeat(3, minmax(0, 1fr))',
                            xl: 'repeat(4, minmax(0, 1fr))',
                          },
                          gap: 1.25,
                        }}
                      >
                        {pagedMarketListings.map((listing) => (
                          <MarketCard
                            key={listing.listingId}
                            listing={listing}
                            quantityValue={quantities[`purchase:${listing.listingId}`] ?? '1'}
                            onQuantityChange={(value) => setQuantityValue(`purchase:${listing.listingId}`, value)}
                            onPurchase={marketTab === 'public' ? () => void handlePurchase(listing) : undefined}
                            actionDisabled={busyKey !== null}
                            showSeller={marketTab === 'public'}
                          />
                        ))}
                      </Box>
                    ) : (
                      <Typography variant="body2">
                        {normalizedMarketSearch ? locale.noSearchResults : locale.emptyMarket}
                      </Typography>
                    )}

                    {currentMarketListings.length > PAGE_SIZE ? (
                      <Stack alignItems="center">
                        <Pagination
                          page={visibleMarketPage}
                          count={marketPageCount}
                          color="primary"
                          onChange={(_, nextPage) => {
                            if (marketTab === 'public') {
                              setMarketPage(nextPage)
                            } else {
                              setMyMarketPage(nextPage)
                            }
                          }}
                        />
                      </Stack>
                    ) : null}
                  </Stack>
                </SectionFrame>
              </Stack>
            )}
          </PageFrame>
        </Stack>
      </Paper>
    </Container>
  )
}
