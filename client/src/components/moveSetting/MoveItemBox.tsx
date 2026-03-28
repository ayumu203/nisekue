import { Box, Chip, Paper, Stack, Typography } from '@mui/material'
import { resolveCharacterAssetPath } from '@/lib/assets'
import type { GetPlayerResponse } from '@/schema/player'
import locale from '../../../locale/player-setting/PlayerSetting.json'
import MoveItem from '@/components/moveSetting/MoveItem'

type MoveItemBoxProps = {
  player: GetPlayerResponse
}

export default function MoveItemBox({ player }: MoveItemBoxProps) {
  const equippedMoves = player.moveSlots.filter((slot) => slot.moveId !== null)
  const attackCount = equippedMoves.filter((slot) => slot.category === 'Attack').length
  const supportCount = equippedMoves.filter((slot) => slot.category === 'Support').length
  const hybridCount = equippedMoves.filter((slot) => slot.category === 'Hybrid').length
  const playerImageSrc = resolveCharacterAssetPath(player.imagePath)

  return (
    <Paper
      variant="outlined"
      sx={{
        borderRadius: 3,
        p: { xs: 2, sm: 2.5 },
        backgroundColor: '#f6f0de',
        borderColor: '#cdb884',
      }}
    >
      <Stack spacing={2}>
        <Paper
          variant="outlined"
          sx={{
            borderRadius: 3,
            p: { xs: 2, sm: 2.5 },
            borderColor: '#b8ab7a',
            backgroundColor: '#44644a',
          }}
        >
          <Stack spacing={1.5}>
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} alignItems={{ xs: 'stretch', sm: 'center' }}>
              <Box
                sx={{
                  width: { xs: '100%', sm: 140 },
                  minWidth: { sm: 140 },
                  aspectRatio: '338 / 350',
                  borderRadius: 3,
                  border: '2px solid #b8ab7a',
                  backgroundColor: 'rgba(255, 249, 232, 0.9)',
                  overflow: 'hidden',
                  display: 'grid',
                  placeItems: 'center',
                  p: 1,
                }}
              >
                {playerImageSrc ? (
                  <Box
                    component="img"
                    src={playerImageSrc}
                    alt={player.userName ?? 'player'}
                    sx={{
                      width: '100%',
                      height: '100%',
                      objectFit: 'contain',
                      objectPosition: 'center bottom',
                      display: 'block',
                    }}
                  />
                ) : (
                  <Typography variant="body2" color="text.secondary">
                    画像なし
                  </Typography>
                )}
              </Box>

              <Stack spacing={1.5} sx={{ flex: 1, minWidth: 0 }}>
                <Stack spacing={0.5}>
                  <Typography variant="overline" sx={{ letterSpacing: '0.18em', color: 'rgba(242, 235, 207, 0.8)' }}>
                    TACTICAL LOADOUT
                  </Typography>
                  <Typography variant="h4" fontWeight={900} lineHeight={1.1} color="#fff8ea">
                    {locale.moveListTitle}
                  </Typography>
                </Stack>

                <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
                  <Chip
                    size="small"
                    label={`レベル ${player.level}`}
                    sx={{ fontWeight: 700, bgcolor: 'rgba(255, 249, 232, 0.94)', color: '#35513a' }}
                  />
                  <Chip
                    size="small"
                    label={`職業Lv ${player.jobLevel}`}
                    sx={{ fontWeight: 700, bgcolor: 'rgba(255, 249, 232, 0.94)', color: '#35513a' }}
                  />
                  <Chip
                    size="small"
                    label={`${player.job.displayName}`}
                    sx={{ fontWeight: 700, bgcolor: 'rgba(239, 226, 183, 0.96)', color: '#5f4a18' }}
                  />
                </Stack>
              </Stack>
            </Stack>

            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: { xs: 'repeat(2, minmax(0, 1fr))', sm: 'repeat(4, minmax(0, 1fr))' },
                gap: 1,
              }}
            >
              <Paper
                variant="outlined"
                sx={{ borderRadius: 2.5, p: 1.25, borderColor: '#d2c08b', bgcolor: 'rgba(255, 249, 232, 0.92)' }}
              >
                <Typography variant="caption" color="text.secondary">
                  装備済み
                </Typography>
                <Typography variant="h6" fontWeight={900} color="#324c36">
                  {equippedMoves.length} / {player.moveSlots.length}
                </Typography>
              </Paper>
              <Paper
                variant="outlined"
                sx={{ borderRadius: 2.5, p: 1.25, borderColor: '#d8baa3', bgcolor: 'rgba(255, 246, 242, 0.92)' }}
              >
                <Typography variant="caption" color="text.secondary">
                  攻撃
                </Typography>
                <Typography variant="h6" fontWeight={900} color="#8a2f24">
                  {attackCount}
                </Typography>
              </Paper>
              <Paper
                variant="outlined"
                sx={{ borderRadius: 2.5, p: 1.25, borderColor: '#b5c79d', bgcolor: 'rgba(242, 248, 235, 0.94)' }}
              >
                <Typography variant="caption" color="text.secondary">
                  補助
                </Typography>
                <Typography variant="h6" fontWeight={900} color="#456132">
                  {supportCount}
                </Typography>
              </Paper>
              <Paper
                variant="outlined"
                sx={{ borderRadius: 2.5, p: 1.25, borderColor: '#d8c07d', bgcolor: 'rgba(255, 248, 224, 0.92)' }}
              >
                <Typography variant="caption" color="text.secondary">
                  複合
                </Typography>
                <Typography variant="h6" fontWeight={900} color="#7b5a15">
                  {hybridCount}
                </Typography>
              </Paper>
            </Box>
          </Stack>
        </Paper>

        <Stack spacing={1}>
          <Typography variant="h6" fontWeight={900}>
            スロット編成
          </Typography>
        </Stack>

        <Stack
          sx={{
            display: 'grid',
            gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, minmax(0, 1fr))' },
            gap: 1.5,
          }}
        >
          {player.moveSlots.map((slot) => (
            <MoveItem key={slot.slot} slot={slot} />
          ))}
        </Stack>
      </Stack>
    </Paper>
  )
}
