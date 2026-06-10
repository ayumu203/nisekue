import { Box, Button, Paper, Stack, Typography } from '@mui/material'
import { cyberButtonSx, cyberColors, cyberPanelSx } from '@/components/petbattle/petBattleStyles'
import { resolveCharacterAssetPath, resolvePublicAssetPath } from '@/lib/assets'
import locale from '../../../locale/pet/PetBattle.json'
import type { PetBattleRun, PetBattleStats } from '@/schema/petBattle'

type PetBattleResultPanelProps = {
  run: PetBattleRun
  ownerImagePath: string | null
  ownerName: string | null
  stats: PetBattleStats | null
  ratingChange: number | null
  onBackToPets: () => void
}

function formatRatingChange(ratingChange: number): string {
  if (ratingChange > 0) {
    return `+${ratingChange}`
  }

  return ratingChange.toString()
}

export default function PetBattleResultPanel({
  run,
  ownerImagePath,
  ownerName,
  stats,
  ratingChange,
  onBackToPets,
}: PetBattleResultPanelProps) {
  if (run.status === 'InProgress') {
    return null
  }

  const isWin = run.status === 'OwnerWon'
  const ownerImageSrc = resolveCharacterAssetPath(ownerImagePath)
  const ratingChangeColor =
    ratingChange == null ? cyberColors.text : ratingChange >= 0 ? cyberColors.accent : cyberColors.danger
  const topMember = run.ownerMembers[0] ?? null
  const bottomMembers = run.ownerMembers.slice(1, 3)

  function renderMemberCard(participantId: string, displayName: string, imagePath: string | null | undefined) {
    const spriteSrc = imagePath ? resolvePublicAssetPath(imagePath) : null

    return (
      <Box
        key={participantId}
        sx={{
          minWidth: 0,
          p: 1,
          borderRadius: 2,
          border: `1px solid ${cyberColors.panelBorder}`,
          backgroundColor: cyberColors.panelLight,
        }}
      >
        <Stack spacing={0.4} alignItems="center">
          {spriteSrc ? (
            <Box component="img" src={spriteSrc} alt={displayName} sx={{ height: 48, objectFit: 'contain' }} />
          ) : null}
          <Typography variant="caption" noWrap sx={{ color: cyberColors.text, fontWeight: 700, maxWidth: '100%' }}>
            {displayName}
          </Typography>
        </Stack>
      </Box>
    )
  }

  return (
    <Stack spacing={2}>
      <Paper variant="outlined" sx={{ ...cyberPanelSx, p: { xs: 2, sm: 3 }, textAlign: 'center' }}>
        <Stack spacing={2} alignItems="center">
          <Typography
            variant="h4"
            sx={{
              fontWeight: 900,
              letterSpacing: '0.1em',
              color: isWin ? cyberColors.accent : cyberColors.danger,
              textShadow: isWin ? `0 0 18px ${cyberColors.accentDim}` : '0 0 18px rgba(255, 56, 96, 0.5)',
            }}
          >
            {locale.resultTitle[run.status]}
          </Typography>

          <Stack spacing={0.8} alignItems="center" sx={{ width: '100%' }}>
            {ownerImageSrc ? (
              <Box
                component="img"
                src={ownerImageSrc}
                alt={ownerName ?? 'player'}
                sx={{
                  width: 108,
                  height: 108,
                  objectFit: 'contain',
                  filter: `drop-shadow(0 0 6px ${cyberColors.accentDim})`,
                }}
              />
            ) : null}
            {ownerName ? (
              <Typography variant="body2" sx={{ color: cyberColors.text, fontWeight: 800 }}>
                {ownerName}
              </Typography>
            ) : null}

            <Stack direction="row" spacing={3} useFlexGap flexWrap="wrap" justifyContent="center" alignItems="baseline">
              <Stack spacing={0.2} alignItems="center">
                <Typography variant="caption" sx={{ color: cyberColors.textDim, letterSpacing: '0.08em' }}>
                  {locale.ratingChangeLabel}
                </Typography>
                <Typography
                  variant="h5"
                  sx={{
                    color: ratingChangeColor,
                    fontWeight: 900,
                    lineHeight: 1.2,
                    textShadow: `0 0 12px ${ratingChangeColor}`,
                  }}
                >
                  {ratingChange == null ? '--' : formatRatingChange(ratingChange)}
                </Typography>
              </Stack>

              <Stack spacing={0.2} alignItems="center">
                <Typography variant="caption" sx={{ color: cyberColors.textDim, letterSpacing: '0.08em' }}>
                  {locale.resultRatingLabel}
                </Typography>
                <Typography variant="h5" sx={{ color: cyberColors.text, fontWeight: 900, lineHeight: 1.2 }}>
                  {stats == null ? '--' : stats.rating}
                </Typography>
              </Stack>
            </Stack>
          </Stack>

          <Stack spacing={1} alignItems="center" sx={{ width: '100%' }}>
            {topMember ? (
              <Box sx={{ width: { xs: '72%', sm: 220 }, maxWidth: '100%' }}>
                {renderMemberCard(topMember.participantId, topMember.displayName, topMember.imagePath)}
              </Box>
            ) : null}

            {bottomMembers.length > 0 ? (
              <Box
                sx={{
                  width: '100%',
                  display: 'grid',
                  gap: 1,
                  gridTemplateColumns: { xs: 'repeat(2, minmax(0, 1fr))', sm: 'repeat(2, minmax(0, 220px))' },
                  justifyContent: 'center',
                }}
              >
                {bottomMembers.map((member) =>
                  renderMemberCard(member.participantId, member.displayName, member.imagePath),
                )}
              </Box>
            ) : null}
          </Stack>

          <Button onClick={onBackToPets} sx={cyberButtonSx}>
            {locale.backToPets}
          </Button>
        </Stack>
      </Paper>
    </Stack>
  )
}
