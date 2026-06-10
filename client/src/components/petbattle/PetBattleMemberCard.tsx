import { Box, Chip, LinearProgress, Paper, Stack, Typography } from '@mui/material'
import { cyberColors } from '@/components/petbattle/petBattleStyles'
import { resolvePublicAssetPath } from '@/lib/assets'
import locale from '../../../locale/pet/PetBattle.json'
import type { PetBattleMember } from '@/schema/petBattle'

type PetBattleMemberCardProps = {
  member: PetBattleMember
  isTargeted?: boolean
  isSubmitted?: boolean
}

function getStatusRate(current: number, max: number): number {
  if (max <= 0) {
    return 0
  }

  return Math.max(0, Math.min(100, (current / max) * 100))
}

function StatusBar({ label, current, max, color }: { label: string; current: number; max: number; color: string }) {
  return (
    <Stack spacing={0.2}>
      <Stack direction="row" justifyContent="space-between">
        <Typography variant="caption" sx={{ color: cyberColors.textDim, fontSize: '0.6rem', lineHeight: 1.1 }}>
          {label}
        </Typography>
        <Typography variant="caption" sx={{ color: cyberColors.text, fontSize: '0.6rem', lineHeight: 1.1 }}>
          {current}/{max}
        </Typography>
      </Stack>
      <LinearProgress
        variant="determinate"
        value={getStatusRate(current, max)}
        sx={{
          height: 5,
          borderRadius: 999,
          backgroundColor: 'rgba(255, 255, 255, 0.08)',
          '& .MuiLinearProgress-bar': { backgroundColor: color, borderRadius: 999 },
        }}
      />
    </Stack>
  )
}

export default function PetBattleMemberCard({
  member,
  isTargeted = false,
  isSubmitted = false,
}: PetBattleMemberCardProps) {
  const spriteSrc = member.imagePath ? resolvePublicAssetPath(member.imagePath) : null

  return (
    <Paper
      variant="outlined"
      sx={{
        p: { xs: 0.75, sm: 1 },
        borderRadius: 2,
        backgroundColor: cyberColors.panelLight,
        borderColor: isTargeted ? cyberColors.accent : cyberColors.panelBorder,
        boxShadow: isTargeted ? `0 0 12px ${cyberColors.accentDim}` : 'none',
        opacity: member.isDead ? 0.45 : 1,
        minWidth: 0,
      }}
    >
      <Stack spacing={0.5}>
        <Stack direction="row" spacing={0.5} alignItems="center" justifyContent="space-between">
          <Typography
            variant="caption"
            noWrap
            title={member.displayName}
            sx={{ fontWeight: 800, color: cyberColors.text, fontSize: { xs: '0.66rem', sm: '0.78rem' } }}
          >
            {member.displayName}
          </Typography>
          {member.isDead ? (
            <Chip
              size="small"
              label={locale.deadChip}
              sx={{
                height: 16,
                fontSize: '0.55rem',
                color: cyberColors.danger,
                backgroundColor: 'rgba(255, 56, 96, 0.14)',
              }}
            />
          ) : isSubmitted ? (
            <Chip
              size="small"
              label={locale.submittedChip}
              sx={{
                height: 16,
                fontSize: '0.55rem',
                color: cyberColors.accent,
                backgroundColor: cyberColors.accentFaint,
              }}
            />
          ) : null}
        </Stack>

        <Box sx={{ display: 'flex', justifyContent: 'center', minHeight: { xs: 40, sm: 56 } }}>
          {spriteSrc ? (
            <Box
              component="img"
              src={spriteSrc}
              alt={member.displayName}
              sx={{
                height: { xs: 40, sm: 56 },
                objectFit: 'contain',
                filter: member.isDead ? 'grayscale(1)' : `drop-shadow(0 0 6px ${cyberColors.accentDim})`,
              }}
            />
          ) : null}
        </Box>

        <StatusBar label="HP" current={member.currentHp} max={member.maxHp} color={cyberColors.accent} />
        <StatusBar label="MP" current={member.currentMp} max={member.maxMp} color={cyberColors.mp} />

        {member.activeEffects.length > 0 ? (
          <Stack direction="row" spacing={0.4} useFlexGap flexWrap="wrap">
            {member.activeEffects.map((effect, index) => (
              <Chip
                key={`${effect.effectType}-${index}`}
                size="small"
                label={`${effect.effectType}(${effect.remainingTurns})`}
                sx={{
                  height: 16,
                  fontSize: '0.52rem',
                  color: cyberColors.warn,
                  backgroundColor: 'rgba(255, 209, 102, 0.12)',
                }}
              />
            ))}
          </Stack>
        ) : null}
      </Stack>
    </Paper>
  )
}
