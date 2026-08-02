import { Alert, Box, Button, Chip, CircularProgress, Container, Paper, Stack, Typography } from '@mui/material'
import { useState } from 'react'
import useSWR from 'swr'
import { clearStandbyPet, getPets, releasePet, standbyPet, trainPet } from '@/api/pet'
import { getPlayer } from '@/api/player'
import HomeNavIconButton from '@/components/common/HomeNavIconButton'
import { ControlFrame } from '@/components/items/ItemsLayout'
import { useAuth } from '@/contexts/useAuth'
import { resolvePublicAssetPath } from '@/lib/assets'
import locale from '../../locale/pet/Pets.json'
import type { PetStatus, PlayerPetView } from '@/schema/pet'

const pokeRed = '#b83040'
const pokeDeepRed = '#7c1828'
const pokeRedBorder = '#a02838'
const pokeCardBorder = '#c04058'

const petFrameSx = {
  borderRadius: 2,
  border: `1px solid ${pokeRedBorder}`,
  boxShadow: '0 8px 20px rgba(80, 0, 10, 0.18)',
  p: { xs: 1.5, sm: 2 },
  background: pokeDeepRed,
} as const

const petTrainButtonSx = {
  '&&': {
    borderRadius: 2,
    minHeight: 38,
    background: '#a02838',
    color: '#ffffff',
    boxShadow: 'none',
    transition: 'none',
  },
  '&&:hover': { background: '#a02838', boxShadow: 'none' },
} as const

const petStandbyButtonSx = {
  '&&': {
    borderRadius: 2,
    minHeight: 38,
    background: '#f3d9de',
    color: '#5c1320',
    border: '1px solid #f9ebee',
    fontWeight: 800,
    boxShadow: 'none',
  },
  '&&:hover': { background: '#fae9ed', boxShadow: 'none' },
} as const

const petReleaseButtonSx = {
  '&&': {
    borderRadius: 2,
    minHeight: 34,
    color: '#ffffff',
    borderColor: '#9a1f30',
    backgroundColor: '#b83040',
    boxShadow: 'none',
    transition: 'none',
  },
  '&&:hover': {
    borderColor: '#9a1f30',
    backgroundColor: '#b83040',
    boxShadow: 'none',
  },
} as const

const petCardSx = {
  backgroundColor: '#ffffff',
  borderColor: pokeCardBorder,
  boxShadow: 'inset 0 1px 0 rgba(255,255,255,0.75)',
} as const

const statusKeys: Array<keyof PetStatus> = ['maxHp', 'maxMp', 'strength', 'defense', 'intelligence', 'luck', 'speed']

function PetStatusTable({ pet }: { pet: PlayerPetView }) {
  return (
    <Stack spacing={0.25}>
      {statusKeys.map((key) => (
        <Stack key={key} direction="row" justifyContent="space-between" spacing={1}>
          <Typography variant="caption" sx={{ color: '#5b4523' }}>
            {locale.statusNames[key]}
          </Typography>
          <Typography variant="caption" sx={{ color: '#3f2f16', fontWeight: 700 }}>
            {pet.totalStatus[key]}
            {pet.bonusStatus[key] > 0 ? (
              <Typography component="span" variant="caption" sx={{ color: '#a02838', ml: 0.5 }}>
                (+{pet.bonusStatus[key]})
              </Typography>
            ) : null}
          </Typography>
        </Stack>
      ))}
    </Stack>
  )
}

export default function Pets() {
  const { session, isLoading } = useAuth()
  const [actionError, setActionError] = useState<string | null>(null)
  const [actionMessage, setActionMessage] = useState<string | null>(null)
  const [busyPetId, setBusyPetId] = useState<string | null>(null)

  const petsSWRKey = session?.user.id ? (['pets', session.user.id] as const) : null
  const {
    data: petsResponse,
    error: petsError,
    isLoading: isPetsLoading,
    mutate: mutatePets,
  } = useSWR(petsSWRKey, async () => {
    if (!session?.access_token) {
      throw new Error(locale.sessionInfoMissing)
    }

    return getPets(session.access_token)
  })

  const playerSWRKey = session?.user.id ? (['pets-player', session.user.id] as const) : null
  const { data: player, mutate: mutatePlayer } = useSWR(playerSWRKey, async () => {
    if (!session?.access_token) {
      return null
    }

    return getPlayer(session.access_token)
  })

  async function runPetAction(petId: string, action: () => Promise<void>): Promise<void> {
    if (!session?.access_token) {
      setActionError(locale.sessionInfoMissing)
      return
    }

    setBusyPetId(petId)
    setActionError(null)
    setActionMessage(null)

    try {
      await action()
    } catch (error) {
      setActionError(error instanceof Error ? error.message : locale.actionFailed)
      setBusyPetId(null)
      return
    }

    try {
      await mutatePets()
    } catch (error) {
      setActionError(error instanceof Error ? error.message : locale.loadFailed)
    } finally {
      setBusyPetId(null)
    }
  }

  async function handleTrain(pet: PlayerPetView): Promise<void> {
    await runPetAction(pet.petId, async () => {
      await trainPet(pet.petId, session!.access_token)
      await mutatePlayer()
      setActionMessage(`${pet.name}${locale.trainSucceeded}`)
    })
  }

  async function handleToggleStandby(pet: PlayerPetView): Promise<void> {
    await runPetAction(pet.petId, async () => {
      if (pet.isStandby) {
        await clearStandbyPet(pet.petId, session!.access_token)
      } else {
        await standbyPet(pet.petId, session!.access_token)
      }
    })
  }

  async function handleRelease(pet: PlayerPetView): Promise<void> {
    if (!window.confirm(`${pet.name}${locale.releaseConfirm}`)) {
      return
    }

    await runPetAction(pet.petId, async () => {
      await releasePet(pet.petId, session!.access_token)
      setActionMessage(locale.releaseSucceeded)
    })
  }

  const trainingCostGold = petsResponse?.trainingCostGold ?? 100000
  const canAffordTraining = player != null && player.gold >= trainingCostGold

  return (
    <Container maxWidth="lg" sx={{ py: { xs: 2, sm: 6 } }}>
      <Paper
        elevation={2}
        sx={{
          backgroundColor: pokeRed,
          border: '1px solid',
          borderColor: '#902030',
          borderRadius: { xs: 3, sm: 4 },
          p: '2%',
        }}
      >
        <Stack spacing={2.5}>
          <Box sx={petFrameSx}>
            <Stack spacing={2}>
              <Box
                sx={{
                  backgroundColor: 'rgba(255,255,255,0.08)',
                  border: '1px solid rgba(255,255,255,0.12)',
                  borderRadius: 2,
                }}
              >
                <ControlFrame>
                  <Stack spacing={1.5}>
                    <Box
                      sx={{
                        '& .MuiIconButton-root': {
                          backgroundColor: 'rgba(255,255,255,0.12)',
                          '&:hover': { backgroundColor: 'rgba(255,255,255,0.22)' },
                        },
                      }}
                    >
                      <HomeNavIconButton ariaLabel={locale.backToHome} />
                    </Box>
                    <Box
                      sx={{
                        display: 'grid',
                        gridTemplateColumns: { xs: '1fr', sm: 'auto 1fr' },
                        gap: { xs: 1.5, sm: 2 },
                        alignItems: 'start',
                        px: { xs: 1, sm: 2 },
                      }}
                    >
                      <Box sx={{ minWidth: 0, textAlign: { xs: 'center', sm: 'left' } }}>
                        <Typography
                          variant="overline"
                          sx={{ letterSpacing: '0.16em', color: 'rgba(255,255,255,0.66)' }}
                        >
                          PET CONTROL
                        </Typography>
                        <Typography variant="h4" fontWeight={900} color="#ffffff">
                          {locale.title}
                        </Typography>
                      </Box>
                      <Stack spacing={1} alignItems={{ xs: 'flex-start', sm: 'flex-end' }}>
                        <Stack
                          direction="row"
                          spacing={1}
                          useFlexGap
                          flexWrap="wrap"
                          justifyContent={{ xs: 'flex-start', sm: 'flex-end' }}
                        >
                          <Chip
                            label={`${locale.capacityLabel}: ${petsResponse?.pets.length ?? 0} / ${petsResponse?.maxPetCount ?? 3}`}
                            sx={{ backgroundColor: 'rgba(255,255,255,0.18)', color: '#ffffff', fontWeight: 700 }}
                          />
                          {player != null ? (
                            <Chip
                              label={`${locale.goldLabel}: ${player.gold.toLocaleString()}G`}
                              sx={{ backgroundColor: 'rgba(255,255,255,0.18)', color: '#ffffff', fontWeight: 700 }}
                            />
                          ) : null}
                        </Stack>
                        <Typography
                          variant="body2"
                          sx={{ color: 'rgba(255,255,255,0.82)', textAlign: { xs: 'left', sm: 'right' } }}
                        >
                          {locale.description}
                        </Typography>
                        <Typography
                          variant="body2"
                          sx={{ color: 'rgba(255,255,255,0.7)', textAlign: { xs: 'left', sm: 'right' } }}
                        >
                          {locale.summonNote}
                        </Typography>
                      </Stack>
                    </Box>
                  </Stack>
                </ControlFrame>
              </Box>

              {actionError ? <Alert severity="error">{actionError}</Alert> : null}
              {actionMessage ? <Alert severity="success">{actionMessage}</Alert> : null}
              {petsError ? <Alert severity="error">{locale.loadFailed}</Alert> : null}

              <Box sx={{ px: { xs: 0.25, sm: 0.5 }, py: 0.25 }}>
                {isLoading || isPetsLoading ? (
                  <Stack direction="row" spacing={1} alignItems="center">
                    <CircularProgress size={16} sx={{ color: 'rgba(255,255,255,0.7)' }} />
                    <Typography variant="body2" color="rgba(255,255,255,0.8)">
                      {locale.loading}
                    </Typography>
                  </Stack>
                ) : petsResponse == null || petsResponse.pets.length === 0 ? (
                  <Paper variant="outlined" sx={{ ...petCardSx, p: 2.5, borderRadius: 3 }}>
                    <Typography variant="body2" sx={{ color: '#5b4523' }}>
                      {locale.emptyMessage}
                    </Typography>
                  </Paper>
                ) : (
                  <Paper variant="outlined" sx={{ ...petCardSx, p: { xs: 1.5, sm: 2 }, borderRadius: 3 }}>
                    <Box
                      sx={{
                        display: 'grid',
                        gap: 1.5,
                        gridTemplateColumns: { xs: '1fr', sm: 'repeat(3, minmax(0, 1fr))' },
                      }}
                    >
                      {petsResponse.pets.map((pet) => {
                        const isBusy = busyPetId === pet.petId
                        const spriteSrc = pet.imagePath ? resolvePublicAssetPath(pet.imagePath) : null

                        return (
                          <Paper key={pet.petId} variant="outlined" sx={{ ...petCardSx, p: 1.5, borderRadius: 3 }}>
                            <Stack spacing={1}>
                              <Stack direction="row" spacing={1} alignItems="center" justifyContent="space-between">
                                <Typography
                                  variant="subtitle2"
                                  noWrap
                                  title={pet.name}
                                  sx={{ fontWeight: 800, color: '#3f2f16' }}
                                >
                                  {pet.name}
                                </Typography>
                                <Chip
                                  size="small"
                                  color={pet.isStandby ? 'error' : 'default'}
                                  label={pet.isStandby ? locale.activeChip : locale.inactiveChip}
                                />
                              </Stack>

                              <Box sx={{ display: 'flex', justifyContent: 'center', minHeight: 84 }}>
                                {spriteSrc ? (
                                  <Box
                                    component="img"
                                    src={spriteSrc}
                                    alt={pet.name}
                                    sx={{ height: 84, objectFit: 'contain' }}
                                  />
                                ) : null}
                              </Box>

                              <Typography variant="caption" sx={{ color: '#5b4523' }}>
                                {locale.levelLabel}
                                {pet.level}
                              </Typography>

                              <PetStatusTable pet={pet} />

                              <Stack spacing={0.75}>
                                <Button
                                  size="small"
                                  variant="contained"
                                  disabled={isBusy || !canAffordTraining}
                                  onClick={() => void handleTrain(pet)}
                                  sx={petTrainButtonSx}
                                >
                                  {isBusy
                                    ? locale.training
                                    : `${locale.train} (${trainingCostGold.toLocaleString()}${locale.trainCostSuffix})`}
                                </Button>
                                <Button
                                  size="small"
                                  variant="contained"
                                  disabled={isBusy}
                                  onClick={() => void handleToggleStandby(pet)}
                                  sx={petStandbyButtonSx}
                                >
                                  {pet.isStandby ? locale.deactivate : locale.activate}
                                </Button>
                                <Button
                                  size="small"
                                  color="error"
                                  variant="outlined"
                                  disabled={isBusy}
                                  onClick={() => void handleRelease(pet)}
                                  sx={petReleaseButtonSx}
                                >
                                  {locale.release}
                                </Button>
                              </Stack>
                            </Stack>
                          </Paper>
                        )
                      })}
                    </Box>
                  </Paper>
                )}
              </Box>
            </Stack>
          </Box>
        </Stack>
      </Paper>
    </Container>
  )
}
