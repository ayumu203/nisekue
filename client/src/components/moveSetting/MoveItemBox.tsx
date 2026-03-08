import { Paper, Stack, Typography } from '@mui/material'
import type { GetPlayerResponse } from '@/schema/player'
import locale from '../../../locale/player-setting/PlayerSetting.json'
import MoveItem from '@/components/moveSetting/MoveItem'

type MoveItemBoxProps = {
  player: GetPlayerResponse
}

export default function MoveItemBox({ player }: MoveItemBoxProps) {
  return (
    <Paper
      variant="outlined"
      sx={{
        borderRadius: 3,
        p: 2,
      }}
    >
      <Stack spacing={1.5}>
        <Typography variant="h6">{locale.moveListTitle}</Typography>
        <Stack
          sx={{
            display: 'grid',
            gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, minmax(0, 1fr))' },
            gap: 1.25,
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
