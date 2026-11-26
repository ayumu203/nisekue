import {
  Alert,
  Avatar,
  Box,
  Button,
  CircularProgress,
  Pagination,
  Paper,
  Stack,
  TextField,
  Typography,
  useMediaQuery,
  type Theme,
} from '@mui/material'
import { useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import useSWR from 'swr'
import { createThread, getThreads } from '@/api/thread'
import ThreadBoardSurface from '@/components/common/ThreadBoardSurface'
import HomeNavIconButton from '@/components/common/HomeNavIconButton'
import ThreadPageShell from '@/components/common/ThreadPageShell'
import { useAuth } from '@/contexts/useAuth'
import { resolveCharacterAssetPath } from '@/lib/assets'
import locale from '../../locale/threads/Threads.json'
import { palette, withAlpha } from '@/theme/tokens'

const boardSx = {
  backgroundColor: palette.brown500,
  borderColor: palette.brown900,
  color: palette.cream,
} as const

const accentSx = {
  color: palette.teal700,
} as const

const inputSx = {
  '& .MuiOutlinedInput-root': {
    borderRadius: 2,
    backgroundColor: palette.cream,
    '& fieldset': {
      borderColor: palette.brown500,
    },
    '&:hover fieldset': {
      borderColor: palette.brown500,
    },
    '&.Mui-focused fieldset': {
      borderColor: palette.brown900,
      borderWidth: 2,
    },
  },
  '& .MuiInputLabel-root.Mui-focused': {
    color: palette.brown900,
  },
} as const

const flatPrimaryButtonSx = {
  borderRadius: 2,
  minHeight: 42,
  backgroundColor: palette.brown900,
  color: palette.cream,
  boxShadow: 'none',
  '&:hover': {
    backgroundColor: palette.brown900,
    boxShadow: 'none',
  },
} as const

const flatSecondaryButtonSx = {
  borderRadius: 2,
  minHeight: 36,
  borderColor: palette.gray600,
  color: palette.brown900,
  backgroundColor: 'transparent',
  boxShadow: 'none',
  '&:hover': {
    borderColor: palette.gray600,
    backgroundColor: 'transparent',
    boxShadow: 'none',
  },
} as const

function formatDate(value: string | null): string {
  if (!value) {
    return locale.noReplies
  }

  return new Date(value).toLocaleString('ja-JP')
}

export default function Threads() {
  const { session, isLoading, isAnonymous } = useAuth()
  const navigate = useNavigate()
  const [searchParams, setSearchParams] = useSearchParams()
  const [title, setTitle] = useState('')
  const [body, setBody] = useState('')
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const page = Math.max(1, Number(searchParams.get('page') ?? '1') || 1)

  const swrKey = session?.access_token ? (['threads', page] as const) : null
  const {
    data,
    error,
    isLoading: isThreadsLoading,
    mutate,
  } = useSWR(swrKey, async () => {
    if (!session?.access_token) {
      throw new Error(locale.sessionMissing)
    }

    return getThreads({ page }, session.access_token)
  })

  const isDesktop = useMediaQuery((theme: Theme) => theme.breakpoints.up('sm'), { noSsr: true })

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

  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1

  return (
    <ThreadPageShell>
      {isDesktop ? null : (
        <Stack direction="row" justifyContent="flex-start">
          <HomeNavIconButton ariaLabel={locale.backToHome} />
        </Stack>
      )}

      <ThreadBoardSurface>
        <Paper variant="outlined" sx={{ ...boardSx, borderRadius: 3, p: { xs: 2, sm: 3 } }}>
          <Stack spacing={2}>
            <Typography variant="overline" sx={{ letterSpacing: '0.22em', color: withAlpha(palette.cream, 0.75) }}>
              THREAD BOARD
            </Typography>
            <Stack
              direction={{ xs: 'column', lg: 'row' }}
              spacing={{ xs: 1, lg: 2 }}
              justifyContent="space-between"
              alignItems={{ xs: 'flex-start', lg: 'flex-end' }}
            >
              <Box>
                <Typography variant="h3" fontWeight={900} lineHeight={1} sx={{ color: palette.cream }}>
                  {locale.title}
                </Typography>
              </Box>
              {data ? (
                <Stack
                  direction="row"
                  spacing={2}
                  flexWrap="wrap"
                  useFlexGap
                  sx={{ color: withAlpha(palette.cream, 0.75) }}
                >
                  <Typography variant="body2">{locale.total.replace('{count}', String(data.totalCount))}</Typography>
                  <Typography variant="body2" sx={{ color: palette.cream, fontWeight: 700 }}>
                    {locale.pageStatus.replace('{page}', String(page)).replace('{pages}', String(totalPages))}
                  </Typography>
                </Stack>
              ) : null}
            </Stack>
          </Stack>
        </Paper>

        <Box sx={{ display: 'grid', gap: 2, gridTemplateColumns: { xs: '1fr', lg: '380px minmax(0, 1fr)' } }}>
          <Paper
            variant="outlined"
            sx={{
              borderRadius: 3,
              p: { xs: 2, sm: 2.5 },
              backgroundColor: palette.brown200,
              borderColor: palette.brown500,
            }}
          >
            <Stack spacing={2}>
              <Box>
                <Typography variant="h5" fontWeight={900} sx={{ color: palette.brown900 }}>
                  {locale.form.heading}
                </Typography>
                <Box sx={{ mt: 1, width: 52, height: 4, borderRadius: 999, backgroundColor: palette.teal700 }} />
              </Box>

              {isAnonymous ? <Alert severity="info">{locale.anonymousPostingRestricted}</Alert> : null}
              {submitError ? <Alert severity="warning">{submitError}</Alert> : null}

              <TextField
                label={locale.form.title}
                value={title}
                onChange={(event) => setTitle(event.target.value)}
                inputProps={{ maxLength: 50 }}
                disabled={isAnonymous || isSubmitting}
                sx={inputSx}
              />
              <TextField
                label={locale.form.body}
                value={body}
                onChange={(event) => setBody(event.target.value)}
                multiline
                minRows={8}
                inputProps={{ maxLength: 500 }}
                disabled={isAnonymous || isSubmitting}
                sx={inputSx}
              />
              <Typography variant="caption" sx={{ color: withAlpha(palette.brown900, 0.75) }}>
                {locale.form.helper.replace('{title}', String(title.length)).replace('{body}', String(body.length))}
              </Typography>
              <Button
                variant="contained"
                disabled={isAnonymous || isSubmitting}
                sx={flatPrimaryButtonSx}
                onClick={async () => {
                  if (isAnonymous) {
                    setSubmitError(locale.anonymousPostingRestricted)
                    return
                  }

                  if (!session?.access_token) {
                    setSubmitError(locale.sessionMissing)
                    return
                  }

                  setIsSubmitting(true)
                  setSubmitError(null)
                  try {
                    const created = await createThread({ title, body }, session.access_token)
                    setTitle('')
                    setBody('')
                    await mutate()
                    navigate(`/threads/${created.id}`)
                  } catch (submitError) {
                    setSubmitError(submitError instanceof Error ? submitError.message : locale.createFailed)
                  } finally {
                    setIsSubmitting(false)
                  }
                }}
              >
                {isSubmitting ? locale.form.submitting : locale.form.submit}
              </Button>
            </Stack>
          </Paper>

          <Paper
            variant="outlined"
            sx={{
              borderRadius: 3,
              p: 0,
              overflow: 'hidden',
              backgroundColor: palette.brown200,
              borderColor: palette.brown500,
            }}
          >
            <Stack spacing={0}>
              <Box sx={{ px: { xs: 2, sm: 2.5 }, py: 2, backgroundColor: palette.brown500 }}>
                <Stack direction="row" justifyContent="space-between" alignItems="center">
                  <Typography variant="h5" fontWeight={900} sx={{ color: palette.cream }}>
                    {locale.listTitle}
                  </Typography>
                  <Box sx={{ width: 10, height: 10, borderRadius: '50%', backgroundColor: palette.teal700 }} />
                </Stack>
              </Box>

              {isThreadsLoading ? (
                <Box sx={{ px: { xs: 2, sm: 2.5 }, py: 2.5 }}>
                  <Stack direction="row" spacing={1} alignItems="center">
                    <CircularProgress size={16} />
                    <Typography variant="body2">{locale.loading}</Typography>
                  </Stack>
                </Box>
              ) : error ? (
                <Box sx={{ px: { xs: 2, sm: 2.5 }, py: 2.5 }}>
                  <Alert severity="warning">{error.message}</Alert>
                </Box>
              ) : !data || data.items.length === 0 ? (
                <Box sx={{ px: { xs: 2, sm: 2.5 }, py: 2.5 }}>
                  <Alert severity="info">{locale.empty}</Alert>
                </Box>
              ) : (
                <>
                  <Stack spacing={0}>
                    {data.items.map((thread, index) => (
                      <Box
                        key={thread.id}
                        sx={{
                          px: { xs: 2, sm: 2.5 },
                          py: 2,
                          borderTop: index === 0 ? 'none' : `1px solid ${withAlpha(palette.brown900, 0.25)}`,
                          backgroundColor: index % 2 === 0 ? palette.brown200 : palette.brown200,
                        }}
                      >
                        <Stack spacing={1.5}>
                          <Stack
                            direction={{ xs: 'column', sm: 'row' }}
                            spacing={1.5}
                            justifyContent="space-between"
                            alignItems={{ xs: 'flex-start', sm: 'center' }}
                          >
                            <Stack direction="row" spacing={1.5} alignItems="center" sx={{ minWidth: 0 }}>
                              <Avatar
                                component={Link}
                                to={`/players/${thread.authorPlayerId}/visit`}
                                aria-label={locale.visitAuthorRoom.replace('{name}', thread.authorName)}
                                src={resolveCharacterAssetPath(thread.authorImagePath) ?? undefined}
                                alt={thread.authorName}
                                sx={{
                                  width: 42,
                                  height: 42,
                                  bgcolor: palette.brown200,
                                  color: palette.brown900,
                                  textDecoration: 'none',
                                  transition: 'transform 140ms ease, box-shadow 140ms ease',
                                  '&:hover': {
                                    transform: 'translateY(-1px)',
                                    boxShadow: `0 6px 14px ${withAlpha(palette.brown900, 0.1)}`,
                                  },
                                }}
                              >
                                {thread.authorName.slice(0, 1)}
                              </Avatar>
                              <Box sx={{ minWidth: 0 }}>
                                <Typography
                                  variant="h6"
                                  fontWeight={900}
                                  sx={{ color: palette.brown900, wordBreak: 'break-word' }}
                                >
                                  {thread.title}
                                </Typography>
                                <Typography variant="body2" sx={{ color: withAlpha(palette.brown900, 0.75) }}>
                                  {locale.author.replace('{name}', thread.authorName)}
                                </Typography>
                              </Box>
                            </Stack>

                            <Button
                              component={Link}
                              to={`/threads/${thread.id}`}
                              variant="outlined"
                              sx={flatSecondaryButtonSx}
                            >
                              {locale.open}
                            </Button>
                          </Stack>

                          <Typography
                            variant="body2"
                            sx={{ color: palette.brown900, whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}
                          >
                            {thread.previewBody}
                          </Typography>

                          <Stack direction={{ xs: 'column', md: 'row' }} spacing={1} justifyContent="space-between">
                            <Typography variant="caption" sx={{ color: withAlpha(palette.brown900, 0.75) }}>
                              {locale.createdAt.replace('{value}', formatDate(thread.createdAt))}
                            </Typography>
                            <Typography variant="caption" sx={{ ...accentSx, opacity: 0.88 }}>
                              {locale.replyInfo
                                .replace('{count}', String(thread.replyCount))
                                .replace('{last}', formatDate(thread.lastRepliedAt))}
                            </Typography>
                          </Stack>
                        </Stack>
                      </Box>
                    ))}
                  </Stack>

                  <Box
                    sx={{ px: { xs: 2, sm: 2.5 }, py: 2, borderTop: `1px solid ${withAlpha(palette.brown900, 0.25)}` }}
                  >
                    <Stack alignItems="center">
                      <Pagination
                        page={page}
                        count={totalPages}
                        onChange={(_, nextPage) => setSearchParams({ page: String(nextPage) })}
                        sx={{
                          '& .MuiPaginationItem-root': {
                            color: palette.brown900,
                          },
                          '& .Mui-selected': {
                            backgroundColor: `${palette.brown200} !important`,
                            color: palette.brown900,
                          },
                        }}
                      />
                    </Stack>
                  </Box>
                </>
              )}
            </Stack>
          </Paper>
        </Box>
      </ThreadBoardSurface>
    </ThreadPageShell>
  )
}
