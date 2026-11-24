import { Box, Pagination, Stack, Typography } from '@mui/material'
import { useRef } from 'react'
import { homePanelColors, trainingPanelColors } from '@/constants/styles'
import { resolveCharacterAssetPath } from '@/lib/assets'
import type { PlayerSummary } from '@/schema/player'
import locale from '../../../locale/training/Training.json'
import TrainingSelectCard from './TrainingSelectCard'
import { trainingCardGridSx } from './trainingSelectStyles'

type TrainingOpponentSelectProps = {
  opponents: PlayerSummary[]
  selectedOpponentId: string | null
  isSelectionDisabled: boolean
  page: number
  pageCount: number
  compact?: boolean
  onPageChange: (page: number) => void
  onSelect: (opponent: PlayerSummary) => void
}

export default function TrainingOpponentSelect({
  opponents,
  selectedOpponentId,
  isSelectionDisabled,
  page,
  pageCount,
  compact = false,
  onPageChange,
  onSelect,
}: TrainingOpponentSelectProps) {
  const topRef = useRef<HTMLDivElement | null>(null)

  if (opponents.length === 0) {
    return (
      <Typography variant="body2" sx={{ color: homePanelColors.label, textAlign: 'center', py: 2 }}>
        {locale.noOpponentsAvailable}
      </Typography>
    )
  }

  function handlePageChange(_: React.ChangeEvent<unknown>, nextPage: number) {
    onPageChange(nextPage)
    topRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' })
  }

  return (
    <Stack spacing={2} ref={topRef}>
      <Box role="radiogroup" aria-label={locale.enemySelectTitle} sx={trainingCardGridSx(compact)}>
        {opponents.map((opponent) => {
          const name = opponent.userName ?? locale.anonymousPlayer
          return (
            <TrainingSelectCard
              key={opponent.userId}
              radioName="training-opponent"
              name={name}
              imageSrc={resolveCharacterAssetPath(opponent.imagePath) ?? null}
              levelLabel={locale.enemyLevel.replace('{{level}}', String(opponent.level))}
              subLabel={opponent.job.displayName}
              backgroundKey={opponent.userId}
              isSelected={opponent.userId === selectedOpponentId}
              isDisabled={isSelectionDisabled}
              compact={compact}
              onSelect={() => onSelect(opponent)}
            />
          )
        })}
      </Box>
      {pageCount > 1 && (
        <Stack alignItems="center">
          <Pagination
            page={Math.min(Math.max(1, page), pageCount)}
            count={pageCount}
            onChange={handlePageChange}
            sx={{
              '& .MuiPaginationItem-root': {
                color: '#5a3a10',
                fontWeight: 700,
                border: '1px solid #d8c9a6',
              },
              '& .MuiPaginationItem-root.Mui-selected, & .MuiPaginationItem-root.Mui-selected:hover': {
                color: '#ffffff',
                backgroundColor: trainingPanelColors.band,
                borderColor: trainingPanelColors.band,
              },
              '& .MuiPaginationItem-ellipsis': {
                border: 'none',
              },
            }}
          />
        </Stack>
      )}
    </Stack>
  )
}
