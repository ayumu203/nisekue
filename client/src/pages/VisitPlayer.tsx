import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Container,
  IconButton,
  Paper,
  Stack,
  SvgIcon,
  Tab,
  Tabs,
  TextField,
  Typography,
  type SvgIconProps,
} from '@mui/material'
import { useState } from 'react'
import { useParams } from 'react-router-dom'
import useSWR from 'swr'
import { getChatRoom, postChatMessage } from '@/api/chat'
import { getItems } from '@/api/item'
import { createPlayer, getPlayer, getPlayerById, sendPlayerGift } from '@/api/player'
import ChatForm from '@/components/chat/ChatForm'
import ChatMessages from '@/components/chat/ChatMessages'
import HomeNavIconButton from '@/components/common/HomeNavIconButton'
import Status from '@/components/home/Status'
import { useAuth } from '@/contexts/useAuth'
import { innerSurfaceSx, outerPagePaperSx, topNavigationIconButtonSx, twoColumnContentGridSx } from '@/constants/styles'
import { INITIAL_PLAYER_NAME } from '@/lib/player'
import type { ItemEquipmentView, ItemStackView } from '@/schema/item'
import locale from '../../locale/visit-player/VisitPlayer.json'

function ChatPanelIcon(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 24 24">
      <path d="M4 5.5A2.5 2.5 0 0 1 6.5 3h11A2.5 2.5 0 0 1 20 5.5v7A2.5 2.5 0 0 1 17.5 15H9.41L5 19.41V15.5A2.5 2.5 0 0 1 4 13.5zm2.5-.5a.5.5 0 0 0-.5.5v8h.59L8.59 13h8.91a.5.5 0 0 0 .5-.5v-7a.5.5 0 0 0-.5-.5z" />
    </SvgIcon>
  )
}

function GiftPanelIcon(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 24 24">
      <path d="M20 7h-2.18A2.99 2.99 0 0 0 12 4.35 2.99 2.99 0 0 0 6.18 7H4a1 1 0 0 0-1 1v3a1 1 0 0 0 1 1h1v7a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2v-7h1a1 1 0 0 0 1-1V8a1 1 0 0 0-1-1M9 6a1 1 0 0 1 1 1H8a1 1 0 0 1 1-1m4 0a1 1 0 0 1 1 1h-2a1 1 0 0 1 1-1M5 9h6v2H5zm2 10v-7h4v7zm6 0v-7h4v7zm6-8h-6V9h6z" />
    </SvgIcon>
  )
}

function VisitPlayer() {
  const { playerId } = useParams<{ playerId: string }>()
  const { session, isLoading, isAnonymous } = useAuth()
  const [activePanel, setActivePanel] = useState<'chat' | 'gift'>('chat')
  const [giftTab, setGiftTab] = useState<'equipments' | 'items'>('items')
  const [giftQuantities, setGiftQuantities] = useState<Record<string, string>>({})
  const [giftBusyKey, setGiftBusyKey] = useState<string | null>(null)
  const [giftFeedback, setGiftFeedback] = useState<{ type: 'success' | 'error'; message: string } | null>(null)

  const currentPlayerSWRKey = session?.user.id ? (['player', session.user.id] as const) : null
  const {
    data: currentPlayer,
    error: currentPlayerError,
    isLoading: isCurrentPlayerLoading,
  } = useSWR(currentPlayerSWRKey, async () => {
    if (!session?.access_token) {
      throw new Error(locale.sessionInfoMissing)
    }

    try {
      return await getPlayer(session.access_token)
    } catch (error) {
      const message = error instanceof Error ? error.message : ''

      if (!message.includes('プレイヤーが見つかりません')) {
        throw error
      }

      await createPlayer({ userName: INITIAL_PLAYER_NAME }, session.access_token)
      return getPlayer(session.access_token)
    }
  })

  const giftInventorySWRKey = session?.user.id ? (['visit-player-gifts', session.user.id] as const) : null
  const {
    data: giftInventory,
    error: giftInventoryError,
    isLoading: isGiftInventoryLoading,
    mutate: mutateGiftInventory,
  } = useSWR(giftInventorySWRKey, async () => {
    if (!session?.access_token) {
      throw new Error(locale.sessionInfoMissing)
    }

    try {
      return await getItems(session.access_token)
    } catch (error) {
      const message = error instanceof Error ? error.message : ''
      if (!message.includes('プレイヤーが見つかりません')) {
        throw error
      }

      await createPlayer({ userName: INITIAL_PLAYER_NAME }, session.access_token)
      return getItems(session.access_token)
    }
  })

  const visitedPlayerSWRKey = session?.access_token && playerId ? (['player-visit', playerId] as const) : null
  const {
    data: visitedPlayer,
    error: visitedPlayerError,
    isLoading: isVisitedPlayerLoading,
  } = useSWR(visitedPlayerSWRKey, async () => {
    if (!session?.access_token || !playerId) {
      throw new Error(locale.playerNotFound)
    }

    return getPlayerById(playerId, session.access_token)
  }, { dedupingInterval: 300000 })

  const chatSWRKey = session?.access_token && playerId ? (['chat-room-visit', playerId] as const) : null
  const {
    data: chatRoom,
    error: chatError,
    isLoading: isChatLoading,
    isValidating: isChatValidating,
    mutate: mutateChatRoom,
  } = useSWR(chatSWRKey, async () => {
    if (!session?.access_token || !playerId) {
      throw new Error(locale.playerNotFound)
    }

    return getChatRoom({ ownerId: playerId }, session.access_token)
  })

  const giftEquipments = (giftInventory?.inventoryItems ?? []).filter(
    (item): item is ItemEquipmentView => item.kind === 'equipment',
  )
  const giftItems = (giftInventory?.inventoryItems ?? []).filter((item): item is ItemStackView => item.kind === 'item')
  const visibleGiftItems = giftTab === 'equipments' ? giftEquipments : giftItems
  const actionButtonSx = (isActive: boolean) => ({
    ...topNavigationIconButtonSx,
    backgroundColor: isActive ? '#5f7f67' : topNavigationIconButtonSx.backgroundColor,
    '&:hover': {
      backgroundColor: isActive ? '#56755f' : topNavigationIconButtonSx['&:hover'].backgroundColor,
    },
  })

  if (isLoading) {
    return (
      <Box minHeight="100vh" display="grid" sx={{ placeItems: 'center' }}>
        <Stack direction="row" spacing={1} alignItems="center">
          <CircularProgress size={20} />
          <Typography>{locale.loading}</Typography>
        </Stack>
      </Box>
    )
  }

  return (
    <Container maxWidth="lg" sx={{ py: { xs: 2, sm: 8 } }}>
      <Paper elevation={2} sx={outerPagePaperSx}>
        <Stack spacing={{ xs: 2, sm: 2 }}>
          <Box sx={twoColumnContentGridSx}>
            <Stack spacing={{ xs: 1.5, sm: 2 }}>
              {isVisitedPlayerLoading ? (
                <Stack direction="row" spacing={1} alignItems="center">
                  <CircularProgress size={16} />
                  <Typography variant="body2">{locale.loading}</Typography>
                </Stack>
              ) : visitedPlayerError ? (
                <Alert severity="warning">{visitedPlayerError.message}</Alert>
              ) : (
                <Status
                  player={visitedPlayer}
                  showDesktopActions={false}
                  topAction={
                    <Stack direction="row" spacing={1}>
                      <HomeNavIconButton ariaLabel={locale.backToHome} />
                      <IconButton
                        aria-label={locale.chatButtonAriaLabel}
                        onClick={() => setActivePanel('chat')}
                        sx={actionButtonSx(activePanel === 'chat')}
                      >
                        <ChatPanelIcon />
                      </IconButton>
                      <IconButton
                        aria-label={locale.giftButtonAriaLabel}
                        onClick={() => setActivePanel('gift')}
                        sx={actionButtonSx(activePanel === 'gift')}
                      >
                        <GiftPanelIcon />
                      </IconButton>
                    </Stack>
                  }
                />
              )}
            </Stack>

            <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: { xs: 2, sm: 2.5 } }}>
              <Stack spacing={{ xs: 1.5, sm: 2 }}>
                <Typography variant="h5">
                  {activePanel === 'chat'
                    ? visitedPlayer?.userName
                      ? `${visitedPlayer.userName}${locale.chatTitleSuffix}`
                      : locale.loading
                    : locale.giftTitle}
                </Typography>
                {activePanel === 'gift' ? (
                  <Paper variant="outlined" sx={{ borderRadius: 2.5, p: 2 }}>
                    <Stack spacing={1.5}>
                      <Typography variant="body2" color="text.secondary">
                        {locale.giftDescription}
                      </Typography>
                      {giftFeedback ? <Alert severity={giftFeedback.type}>{giftFeedback.message}</Alert> : null}
                      {visitedPlayer?.userId === currentPlayer?.userId ? (
                        <Alert severity="info">{locale.giftSelfDisabled}</Alert>
                      ) : isGiftInventoryLoading ? (
                        <Stack direction="row" spacing={1} alignItems="center">
                          <CircularProgress size={16} />
                          <Typography variant="body2">{locale.giftSending}</Typography>
                        </Stack>
                      ) : giftInventoryError ? (
                        <Alert severity="warning">{giftInventoryError.message || locale.giftListLoadFailed}</Alert>
                      ) : (
                        <Stack spacing={1.5}>
                          <Tabs
                            value={giftTab}
                            onChange={(_, value: 'equipments' | 'items') => setGiftTab(value)}
                            variant="fullWidth"
                          >
                            <Tab value="equipments" label={locale.giftEquipmentsTab} />
                            <Tab value="items" label={locale.giftItemsTab} />
                          </Tabs>
                          {visibleGiftItems.length === 0 ? (
                            <Typography variant="body2">{locale.giftEmpty}</Typography>
                          ) : (
                            <Stack spacing={1}>
                              {visibleGiftItems.map((item) => {
                                const key = item.kind === 'equipment' ? item.playerEquipmentId : item.itemStackId
                                return (
                                  <Paper key={key} variant="outlined" sx={{ borderRadius: 2, p: 1.5 }}>
                                    <Stack
                                      direction={{ xs: 'column', sm: 'row' }}
                                      spacing={1.5}
                                      justifyContent="space-between"
                                      alignItems={{ sm: 'center' }}
                                    >
                                      <Box sx={{ minWidth: 0 }}>
                                        <Typography fontWeight={800}>{item.name}</Typography>
                                        <Typography variant="body2" color="text.secondary">
                                          {item.flavorText || '-'}
                                        </Typography>
                                      </Box>
                                      <Stack
                                        direction={{ xs: 'column', sm: 'row' }}
                                        spacing={1}
                                        alignItems={{ sm: 'center' }}
                                      >
                                        {item.kind === 'item' ? (
                                          <TextField
                                            size="small"
                                            label={locale.giftQuantity}
                                            value={giftQuantities[key] ?? '1'}
                                            onChange={(event) =>
                                              setGiftQuantities((current) => ({
                                                ...current,
                                                [key]: event.target.value,
                                              }))
                                            }
                                            inputProps={{ inputMode: 'numeric', min: 1, max: item.quantity }}
                                            sx={{ width: 104 }}
                                          />
                                        ) : (
                                          <Typography variant="body2">x1</Typography>
                                        )}
                                        <Button
                                          variant="contained"
                                          disabled={
                                            giftBusyKey !== null || !visitedPlayer?.userId || !session?.access_token
                                          }
                                          onClick={async () => {
                                            if (!visitedPlayer?.userId || !session?.access_token) {
                                              return
                                            }

                                            setGiftBusyKey(key)
                                            setGiftFeedback(null)
                                            try {
                                              const response = await sendPlayerGift(
                                                visitedPlayer.userId,
                                                item.kind === 'equipment'
                                                  ? {
                                                      playerEquipmentId: item.playerEquipmentId,
                                                      itemStackId: null,
                                                      quantity: 1,
                                                    }
                                                  : {
                                                      playerEquipmentId: null,
                                                      itemStackId: item.itemStackId,
                                                      quantity: Number.parseInt(giftQuantities[key] ?? '1', 10) || 1,
                                                    },
                                                session.access_token,
                                              )
                                              setGiftFeedback({
                                                type: 'success',
                                                message: response.message || locale.giftSuccess,
                                              })
                                              await Promise.all([mutateGiftInventory(), mutateChatRoom()])
                                            } catch (error) {
                                              setGiftFeedback({
                                                type: 'error',
                                                message:
                                                  error instanceof Error ? error.message : locale.giftListLoadFailed,
                                              })
                                            } finally {
                                              setGiftBusyKey(null)
                                            }
                                          }}
                                        >
                                          {giftBusyKey === key ? locale.giftSending : locale.giftSend}
                                        </Button>
                                      </Stack>
                                    </Stack>
                                  </Paper>
                                )
                              })}
                            </Stack>
                          )}
                        </Stack>
                      )}
                    </Stack>
                  </Paper>
                ) : (
                  <>
                    {isCurrentPlayerLoading ? (
                      <Stack direction="row" spacing={1} alignItems="center">
                        <CircularProgress size={16} />
                        <Typography variant="body2">{locale.loading}</Typography>
                      </Stack>
                    ) : currentPlayerError ? (
                      <Alert severity="warning">{currentPlayerError.message}</Alert>
                    ) : isChatLoading ? (
                      <Stack direction="row" spacing={1} alignItems="center">
                        <CircularProgress size={16} />
                        <Typography variant="body2">{locale.loading}</Typography>
                      </Stack>
                    ) : chatError ? (
                      <Alert severity="warning">{chatError.message}</Alert>
                    ) : !currentPlayer || !playerId ? (
                      <Alert severity="warning">{locale.sessionInfoMissing}</Alert>
                    ) : (
                      <Stack spacing={2}>
                        <ChatForm
                          isSubmitting={isChatValidating}
                          disabled={isAnonymous}
                          disabledReason={isAnonymous ? locale.anonymousPostingRestricted : null}
                          onSubmit={async (text) => {
                            if (!session?.access_token) {
                              throw new Error(locale.sessionInfoMissing)
                            }

                            const updated = await postChatMessage(
                              {
                                ownerId: playerId,
                                text,
                              },
                              session.access_token,
                            )
                            await mutateChatRoom(updated, { revalidate: false })
                          }}
                        />
                        <ChatMessages messages={chatRoom?.messages ?? []} currentPlayerId={currentPlayer.userId} />
                      </Stack>
                    )}
                  </>
                )}
              </Stack>
            </Paper>
          </Box>
        </Stack>
      </Paper>
    </Container>
  )
}

export default VisitPlayer
