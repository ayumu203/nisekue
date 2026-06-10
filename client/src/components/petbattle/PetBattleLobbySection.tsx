import { Box, Button, Chip, Paper, Stack, Typography } from '@mui/material'
import { useState } from 'react'
import { cyberButtonSx, cyberColors, cyberDangerButtonSx, cyberPanelSx } from '@/components/petbattle/petBattleStyles'
import { resolvePublicAssetPath } from '@/lib/assets'
import locale from '../../../locale/pet/PetBattle.json'
import type { PlayerPetView } from '@/schema/pet'
import type { BattleColumn, BattleRow, PetBattleRoom } from '@/schema/petBattle'

type PetBattleLobbySectionProps = {
  room: PetBattleRoom
  pets: PlayerPetView[]
  isAssigning: boolean
  isStarting: boolean
  onAssignSlot: (petId: string, row: BattleRow, column: BattleColumn) => void | Promise<void>
  onRemoveSlot: (petId: string) => void | Promise<void>
  onStartBattle: () => void | Promise<void>
}

const rowOrder: BattleRow[] = ['Front', 'Middle', 'Back']
const columnOrder: BattleColumn[] = ['Left', 'Right']

export default function PetBattleLobbySection({
  room,
  pets,
  isAssigning,
  isStarting,
  onAssignSlot,
  onRemoveSlot,
  onStartBattle,
}: PetBattleLobbySectionProps) {
  const [selectedPetId, setSelectedPetId] = useState<string | null>(null)

  const petsById = new Map(pets.map((pet) => [pet.petId, pet]))
  const placedPetIds = new Set(room.slots.map((slot) => slot.petId))
  const slotByPosition = new Map(room.slots.map((slot) => [`${slot.row}:${slot.column}`, slot]))
  const canStart = room.slots.length > 0 && !isStarting && !isAssigning

  return (
    <Stack spacing={2}>
      <Paper variant="outlined" sx={{ ...cyberPanelSx, p: { xs: 1.5, sm: 2 } }}>
        <Stack spacing={1}>
          <Typography variant="overline" sx={{ color: cyberColors.accentDim, letterSpacing: '0.18em' }}>
            {locale.lobbyTitle}
          </Typography>
          <Stack direction="row" spacing={1} alignItems="center" useFlexGap flexWrap="wrap">
            <Typography variant="body2" sx={{ color: cyberColors.textDim }}>
              {locale.opponentLabel}:
            </Typography>
            <Typography variant="subtitle1" sx={{ color: cyberColors.accent, fontWeight: 800 }}>
              {room.opponentPlayerName ?? locale.unknownPlayer}
            </Typography>
          </Stack>
          <Typography variant="body2" sx={{ color: cyberColors.textDim }}>
            {locale.lobbyDescription}
          </Typography>
        </Stack>
      </Paper>

      <Paper variant="outlined" sx={{ ...cyberPanelSx, p: { xs: 1.5, sm: 2 } }}>
        <Stack spacing={1.5}>
          <Typography variant="subtitle2" sx={{ color: cyberColors.text, fontWeight: 800 }}>
            {locale.myPetsTitle}
          </Typography>

          {pets.length === 0 ? (
            <Typography variant="body2" sx={{ color: cyberColors.textDim }}>
              {locale.noPets}
            </Typography>
          ) : (
            <Box
              sx={{
                display: 'grid',
                gap: 1,
                gridTemplateColumns: { xs: 'repeat(2, minmax(0, 1fr))', sm: 'repeat(3, minmax(0, 1fr))' },
              }}
            >
              {pets.map((pet) => {
                const isPlaced = placedPetIds.has(pet.petId)
                const isSelected = selectedPetId === pet.petId
                const spriteSrc = pet.imagePath ? resolvePublicAssetPath(pet.imagePath) : null

                return (
                  <Paper
                    key={pet.petId}
                    variant="outlined"
                    onClick={() => setSelectedPetId(isSelected ? null : pet.petId)}
                    sx={{
                      p: 1,
                      borderRadius: 2,
                      cursor: 'pointer',
                      backgroundColor: cyberColors.panelLight,
                      borderColor: isSelected ? cyberColors.accent : cyberColors.panelBorder,
                      boxShadow: isSelected ? `0 0 12px ${cyberColors.accentDim}` : 'none',
                      opacity: isPlaced && !isSelected ? 0.6 : 1,
                    }}
                  >
                    <Stack spacing={0.5} alignItems="center">
                      {spriteSrc ? (
                        <Box component="img" src={spriteSrc} alt={pet.name} sx={{ height: 48, objectFit: 'contain' }} />
                      ) : null}
                      <Typography
                        variant="caption"
                        noWrap
                        title={pet.name}
                        sx={{ color: cyberColors.text, fontWeight: 700, maxWidth: '100%' }}
                      >
                        {pet.name} Lv{pet.level}
                      </Typography>
                      {isSelected ? (
                        <Chip
                          size="small"
                          label={locale.selectedChip}
                          sx={{ height: 18, fontSize: '0.6rem', color: '#031007', backgroundColor: cyberColors.accent }}
                        />
                      ) : isPlaced ? (
                        <Chip
                          size="small"
                          label={locale.placedChip}
                          sx={{
                            height: 18,
                            fontSize: '0.6rem',
                            color: cyberColors.accent,
                            backgroundColor: cyberColors.accentFaint,
                          }}
                        />
                      ) : null}
                    </Stack>
                  </Paper>
                )
              })}
            </Box>
          )}
        </Stack>
      </Paper>

      <Paper variant="outlined" sx={{ ...cyberPanelSx, p: { xs: 1.5, sm: 2 } }}>
        <Stack spacing={1}>
          {rowOrder.map((row) => (
            <Stack key={row} direction="row" spacing={1} alignItems="stretch">
              <Box
                sx={{
                  width: 44,
                  display: 'grid',
                  placeItems: 'center',
                  borderRadius: 1.5,
                  border: `1px solid ${cyberColors.panelBorder}`,
                  backgroundColor: cyberColors.accentFaint,
                }}
              >
                <Typography variant="caption" sx={{ color: cyberColors.accent, fontWeight: 800 }}>
                  {locale.rows[row]}
                </Typography>
              </Box>

              {columnOrder.map((column) => {
                const slot = slotByPosition.get(`${row}:${column}`)
                const slotPet = slot ? petsById.get(slot.petId) : null

                return (
                  <Paper
                    key={column}
                    variant="outlined"
                    onClick={() => {
                      if (isAssigning) {
                        return
                      }

                      if (selectedPetId != null) {
                        void onAssignSlot(selectedPetId, row, column)
                        setSelectedPetId(null)
                      }
                    }}
                    sx={{
                      flex: 1,
                      minWidth: 0,
                      minHeight: 84,
                      p: 0.75,
                      borderRadius: 2,
                      borderStyle: slot ? 'solid' : 'dashed',
                      borderColor: selectedPetId != null ? cyberColors.accent : cyberColors.panelBorder,
                      backgroundColor: slot ? cyberColors.panelLight : 'transparent',
                      cursor: selectedPetId != null && !isAssigning ? 'pointer' : 'default',
                      display: 'grid',
                      placeItems: 'center',
                    }}
                  >
                    {slot != null ? (
                      <Stack spacing={0.4} alignItems="center">
                        {slotPet?.imagePath ? (
                          <Box
                            component="img"
                            src={resolvePublicAssetPath(slotPet.imagePath)}
                            alt={slotPet.name}
                            sx={{ height: 36, objectFit: 'contain' }}
                          />
                        ) : null}
                        <Typography
                          variant="caption"
                          noWrap
                          sx={{ color: cyberColors.text, fontWeight: 700, maxWidth: '100%' }}
                        >
                          {slotPet?.name ?? slot.petId}
                        </Typography>
                        <Button
                          size="small"
                          disabled={isAssigning}
                          onClick={(event) => {
                            event.stopPropagation()
                            void onRemoveSlot(slot.petId)
                          }}
                          sx={{
                            ...cyberDangerButtonSx,
                            '&&': { ...cyberDangerButtonSx['&&'], minHeight: 24, px: 1, fontSize: '0.62rem' },
                          }}
                        >
                          {locale.removeSlot}
                        </Button>
                      </Stack>
                    ) : (
                      <Typography variant="caption" sx={{ color: cyberColors.textDim }}>
                        {locale.emptySlot}
                      </Typography>
                    )}
                  </Paper>
                )
              })}
            </Stack>
          ))}

          <Button onClick={() => void onStartBattle()} disabled={!canStart} sx={cyberButtonSx}>
            {isStarting ? locale.starting : locale.startBattle}
          </Button>
        </Stack>
      </Paper>
    </Stack>
  )
}
