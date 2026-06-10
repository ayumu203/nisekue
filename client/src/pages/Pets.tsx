import { Alert, Box, Button, Chip, CircularProgress, Container, Paper, Stack, Typography } from '@mui/material'
import { useState } from 'react'
import useSWR from 'swr'
import { activatePet, deactivatePet, getPets, releasePet, trainPet } from '@/api/pet'
import { getPlayer } from '@/api/player'
import HomeNavIconButton from '@/components/common/HomeNavIconButton'
import { innerSurfaceSx, mutedGreenButtonSx, outerPagePaperSx, softGreenButtonSx } from '@/constants/styles'
import { useAuth } from '@/contexts/useAuth'
import { resolvePublicAssetPath } from '@/lib/assets'
import locale from '../../locale/pet/Pets.json'
import type { PetStatus, PlayerPetView } from '@/schema/pet'

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
              <Typography component="span" variant="caption" sx={{ color: '#2e6a49', ml: 0.5 }}>
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
      await mutatePets()
    } catch (error) {
      setActionError(error instanceof Error ? error.message : locale.actionFailed)
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

  async function handleToggleActive(pet: PlayerPetView): Promise<void> {
    await runPetAction(pet.petId, async () => {
      if (pet.isActive) {
        await deactivatePet(pet.petId, session!.access_token)
      } else {
        await activatePet(pet.petId, session!.access_token)
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
    <Container maxWidth="md" sx={{ py: { xs: 2, sm: 8 } }}>
      <Paper variant="outlined" sx={{ ...outerPagePaperSx, p: { xs: 2, sm: 3 } }}>
        <Stack spacing={2}>
          <Stack direction="row" spacing={1} alignItems="center">
            <HomeNavIconButton ariaLabel={locale.backToHome} />
            <Typography variant="h5" sx={{ fontWeight: 800, color: '#365f3c' }}>
              {locale.title}
            </Typography>
          </Stack>

          <Typography variant="body2" sx={{ color: '#5b4523' }}>
            {locale.description}
          </Typography>
          <Typography variant="body2" sx={{ color: '#5b4523' }}>
            {locale.summonNote}
          </Typography>

          <Stack direction="row" spacing={1} useFlexGap flexWrap="wrap">
            <Chip
              label={`${locale.capacityLabel}: ${petsResponse?.pets.length ?? 0} / ${petsResponse?.maxPetCount ?? 3}`}
            />
            {player != null ? <Chip label={`${locale.goldLabel}: ${player.gold.toLocaleString()}G`} /> : null}
          </Stack>

          {actionError ? <Alert severity="error">{actionError}</Alert> : null}
          {actionMessage ? <Alert severity="success">{actionMessage}</Alert> : null}
          {petsError ? <Alert severity="error">{locale.loadFailed}</Alert> : null}

          {isLoading || isPetsLoading ? (
            <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
              <CircularProgress />
            </Box>
          ) : petsResponse == null || petsResponse.pets.length === 0 ? (
            <Paper variant="outlined" sx={{ ...innerSurfaceSx, p: 2.5, borderRadius: 3 }}>
              <Typography variant="body2" sx={{ color: '#5b4523' }}>
                {locale.emptyMessage}
              </Typography>
            </Paper>
          ) : (
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
                  <Paper key={pet.petId} variant="outlined" sx={{ ...innerSurfaceSx, p: 1.5, borderRadius: 3 }}>
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
                          color={pet.isActive ? 'success' : 'default'}
                          label={pet.isActive ? locale.activeChip : locale.inactiveChip}
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
                          sx={softGreenButtonSx}
                        >
                          {isBusy
                            ? locale.training
                            : `${locale.train} (${trainingCostGold.toLocaleString()}${locale.trainCostSuffix})`}
                        </Button>
                        <Button
                          size="small"
                          variant="contained"
                          disabled={isBusy}
                          onClick={() => void handleToggleActive(pet)}
                          sx={mutedGreenButtonSx}
                        >
                          {pet.isActive ? locale.deactivate : locale.activate}
                        </Button>
                        <Button
                          size="small"
                          color="error"
                          variant="outlined"
                          disabled={isBusy}
                          onClick={() => void handleRelease(pet)}
                        >
                          {locale.release}
                        </Button>
                      </Stack>
                    </Stack>
                  </Paper>
                )
              })}
            </Box>
          )}
        </Stack>
      </Paper>
    </Container>
  )
}
