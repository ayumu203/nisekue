import { Box, Button, Chip, LinearProgress, Paper, Stack, Typography } from '@mui/material'
import type { GetPlayerResponse, PlayerJobCode } from '@/schema/player'
import { resolveJobAssetPath } from '@/lib/assets'
import locale from '../../../locale/player-job/JobChange.json'

const baseJobCodes = new Set<PlayerJobCode>(['Warrior', 'Guardian', 'Mage', 'Priest', 'Ranger'])

function formatExpProgress(
  currentExp: number,
  requiredExp: number,
): { current: number; required: number; ratio: number } {
  const required = Math.max(1, requiredExp)
  const current = Math.max(0, currentExp)
  return {
    current,
    required,
    ratio: Math.max(0, Math.min(100, (current / required) * 100)),
  }
}

interface JobChangeTabProps {
  player: GetPlayerResponse
  isSubmittingJobValue: number | null
  onJobChange: (jobValue: number) => void
}

export default function JobChangeTab({ player, isSubmittingJobValue, onJobChange }: JobChangeTabProps) {
  const currentJobs = (player.jobProfiles ?? []).filter((job) => baseJobCodes.has(job.code))
  const unlockThreshold = 5
  const jobExpProgress = formatExpProgress(player.jobExp ?? 0, player.requiredJobExpForNextLevel ?? 1)
  const isCurrentJobMastered =
    player.job?.code != null && (player.masteredJobs ?? []).some((job) => job.code === player.job.code)

  return (
    <Stack spacing={2}>
      <Paper
        variant="outlined"
        sx={{
          borderRadius: 3,
          p: { xs: 2, sm: 2.5 },
          backgroundColor: '#44644a',
          borderColor: '#b8ab7a',
        }}
      >
        <Stack spacing={2}>
          <Stack spacing={0.25}>
            <Typography variant="overline" sx={{ letterSpacing: '0.16em', color: 'rgba(243, 238, 220, 0.72)' }}>
              JOB CHANGE
            </Typography>
            <Typography variant="h4" fontWeight={900} lineHeight={1.1} color="#fff8ea">
              {locale.title}
            </Typography>
          </Stack>
          <Stack
            direction={{ xs: 'column', sm: 'row' }}
            spacing={2}
            alignItems={{ xs: 'stretch', sm: 'center' }}
            justifyContent="space-between"
          >
            <Stack spacing={0.5}>
              <Chip
                label={player.job.displayName}
                sx={{
                  width: 'fit-content',
                  fontWeight: 900,
                  fontSize: '1rem',
                  bgcolor: 'rgba(255, 249, 232, 0.92)',
                  color: '#35513a',
                  border: '1px solid #cbb783',
                }}
              />
              <Typography variant="body2" sx={{ color: 'rgba(243, 238, 220, 0.84)' }}>
                {player.job.description}
              </Typography>
            </Stack>
          </Stack>

          <Box
            sx={{
              display: 'grid',
              gridTemplateColumns: { xs: '1fr', sm: '220px minmax(0, 1fr)' },
              gap: 2,
              alignItems: { xs: 'stretch', sm: 'start' },
            }}
          >
            <Box
              sx={{
                width: '100%',
                maxWidth: 240,
                aspectRatio: '1 / 1',
                justifySelf: 'center',
                borderRadius: 3,
                border: '2px solid',
                borderColor: '#bda86f',
                bgcolor: '#fff8ea',
                boxShadow: '0 10px 24px rgba(45, 61, 39, 0.12)',
                overflow: 'hidden',
                display: 'grid',
                placeItems: 'center',
                p: 1.5,
                backgroundImage:
                  'radial-gradient(circle at 50% 35%, rgba(232, 242, 221, 0.95), rgba(255, 248, 234, 0.88) 58%, rgba(219, 231, 199, 0.92))',
              }}
            >
              <Box
                component="img"
                src={resolveJobAssetPath(player.job.code) ?? ''}
                alt={player.job.displayName}
                sx={{
                  width: '100%',
                  height: '100%',
                  objectFit: 'contain',
                  display: 'block',
                  filter: 'drop-shadow(0 10px 14px rgba(91, 63, 16, 0.18))',
                  transform: 'scale(1.04)',
                }}
              />
            </Box>

            <Stack spacing={1.25}>
              <Box
                sx={{
                  display: 'grid',
                  gridTemplateColumns: 'repeat(2, minmax(0, 1fr))',
                  gap: 1,
                }}
              >
                <Paper
                  variant="outlined"
                  sx={{ borderRadius: 2, p: 1.5, borderColor: '#d2c08b', bgcolor: 'rgba(255, 249, 232, 0.92)' }}
                >
                  <Typography variant="caption" color="text.secondary">
                    {locale.currentLevel.replace('{{level}}', '')}
                  </Typography>
                  <Typography variant="h6" fontWeight={800} color="#324c36">
                    Lv.{player.level}
                  </Typography>
                </Paper>
                <Paper
                  variant="outlined"
                  sx={{ borderRadius: 2, p: 1.5, borderColor: '#d2c08b', bgcolor: 'rgba(255, 249, 232, 0.92)' }}
                >
                  <Typography variant="caption" color="text.secondary">
                    {locale.currentJobLevel.replace('{{level}}', '')}
                  </Typography>
                  <Typography variant="h6" fontWeight={800} color="#324c36">
                    {isCurrentJobMastered ? 'マスター' : `Lv.${player.jobLevel}`}
                  </Typography>
                </Paper>
              </Box>

              <Stack spacing={0.75}>
                <Stack direction="row" justifyContent="space-between" spacing={1}>
                  <Typography variant="body2" sx={{ color: 'rgba(243, 238, 220, 0.84)' }}>
                    {locale.jobExpProgress
                      .replace('{{current}}', String(jobExpProgress.current))
                      .replace('{{required}}', String(jobExpProgress.required))}
                  </Typography>
                  <Typography variant="body2" fontWeight={700} color="#f0ddb0">
                    {Math.round(jobExpProgress.ratio)}%
                  </Typography>
                </Stack>
                <LinearProgress
                  variant="determinate"
                  value={jobExpProgress.ratio}
                  sx={{
                    height: 10,
                    borderRadius: 999,
                    backgroundColor: 'rgba(255, 249, 232, 0.35)',
                    '& .MuiLinearProgress-bar': {
                      backgroundColor: '#f0ddb0',
                    },
                  }}
                />
              </Stack>
            </Stack>
          </Box>

          <AlertWarning />
        </Stack>
      </Paper>

      <Paper variant="outlined" sx={{ borderRadius: 3, p: { xs: 2, sm: 2.5 } }}>
        <Stack spacing={1.5}>
          <Typography variant="h6">{locale.jobListTitle}</Typography>
          {currentJobs.map((job) => {
            const isCurrent = player.job.value === job.value
            const isLocked = player.level < unlockThreshold
            const isDisabled = isLocked || isCurrent
            const isSubmitting = isSubmittingJobValue === job.value
            const jobImageSrc = resolveJobAssetPath(job.code)
            const statusLabel = isCurrent ? locale.currentBadge : isLocked ? locale.jobLocked : locale.jobAvailable

            return (
              <Paper
                key={job.code}
                variant="outlined"
                sx={{
                  borderRadius: 2,
                  p: 2,
                  backgroundColor: isCurrent ? '#f8f3e3' : '#fffdf8',
                  borderColor: isCurrent ? '#bca56d' : '#d1c19a',
                }}
              >
                <Stack
                  direction={{ xs: 'column', sm: 'row' }}
                  spacing={{ xs: 1.5, sm: 2.5 }}
                  justifyContent="space-between"
                  alignItems={{ xs: 'stretch', sm: 'center' }}
                >
                  <Stack
                    direction={{ xs: 'column', sm: 'row' }}
                    spacing={1.5}
                    alignItems={{ xs: 'stretch', sm: 'center' }}
                  >
                    {jobImageSrc ? (
                      <Box
                        sx={{
                          width: { xs: '100%', sm: 132 },
                          minWidth: { sm: 132 },
                          aspectRatio: '1 / 1',
                          borderRadius: 2.5,
                          border: '2px solid',
                          borderColor: isCurrent ? '#bca56d' : '#d1c19a',
                          bgcolor: '#fff9ef',
                          overflow: 'hidden',
                          display: 'grid',
                          placeItems: 'center',
                          p: 1.25,
                          boxShadow: isCurrent
                            ? '0 10px 18px rgba(52, 74, 45, 0.14)'
                            : '0 6px 12px rgba(79, 71, 47, 0.08)',
                          backgroundImage: isCurrent
                            ? 'radial-gradient(circle at 50% 35%, rgba(231, 241, 219, 0.95), rgba(255, 248, 238, 0.88) 58%, rgba(220, 230, 201, 0.9))'
                            : 'radial-gradient(circle at 50% 35%, rgba(246, 240, 214, 0.94), rgba(255, 250, 240, 0.86) 60%, rgba(233, 225, 195, 0.84))',
                        }}
                      >
                        <Box
                          component="img"
                          src={jobImageSrc}
                          alt={job.displayName}
                          loading="lazy"
                          sx={{
                            width: '100%',
                            height: '100%',
                            objectFit: 'contain',
                            display: 'block',
                            filter: 'drop-shadow(0 8px 12px rgba(91, 63, 16, 0.14))',
                            transform: isCurrent ? 'scale(1.05)' : 'scale(1.02)',
                          }}
                        />
                      </Box>
                    ) : null}
                    <Stack spacing={0.5}>
                      <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
                        <Typography variant="subtitle1" fontWeight={700} sx={{ fontSize: '1.2rem' }}>
                          {job.displayName}
                        </Typography>
                        <Chip
                          label={statusLabel}
                          size="small"
                          variant={isLocked ? 'outlined' : 'filled'}
                          sx={{
                            fontWeight: 700,
                            ...(isCurrent
                              ? {
                                  bgcolor: 'rgba(255, 249, 232, 0.92)',
                                  color: '#35513a',
                                  border: '1px solid #cbb783',
                                }
                              : isLocked
                                ? {
                                    bgcolor: 'transparent',
                                    color: '#6c6246',
                                    border: '1px solid #c7ba96',
                                  }
                                : {
                                    bgcolor: '#4a6c51',
                                    color: '#fff8e8',
                                    border: '1px solid #bfa86e',
                                  }),
                          }}
                        />
                      </Stack>
                      <Typography variant="body2" color="text.secondary">
                        {job.description}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        {isCurrent
                          ? locale.currentDetail
                          : isLocked
                            ? locale.jobLockedDetail
                            : locale.jobResetDetail.replace('{{jobName}}', job.displayName)}
                      </Typography>
                    </Stack>
                  </Stack>
                  {isCurrent ? null : (
                    <Button
                      variant="contained"
                      sx={{
                        ...(isDisabled
                          ? {
                              '&&': {
                                backgroundColor: '#efe7cf',
                                borderColor: '#d5c49a',
                                color: '#8a7d5d',
                              },
                              '&&:hover': {
                                backgroundColor: '#efe7cf',
                                borderColor: '#d5c49a',
                                boxShadow: 'none',
                              },
                              '&&.Mui-disabled': {
                                backgroundColor: '#efe7cf',
                                borderColor: '#d5c49a',
                                color: '#8a7d5d',
                              },
                            }
                          : {
                              '&&': {
                                backgroundColor: '#4a6c51',
                                borderColor: '#bfa86e',
                                color: '#fff8e8',
                              },
                              '&&:hover': {
                                backgroundColor: '#3f5f46',
                                borderColor: '#b59d63',
                                boxShadow: 'none',
                              },
                              '&&.Mui-disabled': {
                                backgroundColor: '#7f967f',
                                borderColor: '#c4b07c',
                                color: '#f5efe0',
                              },
                            }),
                        minWidth: { sm: 116 },
                        alignSelf: { sm: 'center' },
                        whiteSpace: 'nowrap',
                      }}
                      disabled={isDisabled || isSubmittingJobValue !== null}
                      fullWidth={false}
                      onClick={() => {
                        onJobChange(job.value)
                      }}
                    >
                      {isSubmitting ? locale.changing : isLocked ? locale.lockedButton : locale.changeButton}
                    </Button>
                  )}
                </Stack>
              </Paper>
            )
          })}
        </Stack>
      </Paper>
    </Stack>
  )
}

function AlertWarning() {
  return (
    <Box
      sx={{
        mt: 1,
        p: 1.5,
        borderRadius: 2,
        bgcolor: 'rgba(255, 229, 143, 0.15)',
        border: '1px solid',
        borderColor: 'rgba(200, 187, 131, 0.45)',
      }}
    >
      <Typography variant="body2" sx={{ color: 'rgba(243, 238, 220, 0.84)' }}>
        {locale.resetNoticeBody}
      </Typography>
    </Box>
  )
}
