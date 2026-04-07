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

const boardSx = {
  backgroundColor: '#7b553d',
  borderColor: '#5f3f2b',
  color: '#f5eadc',
} as const

const accentSx = {
  color: '#295d63',
} as const

const inputSx = {
  '& .MuiOutlinedInput-root': {
    borderRadius: 2,
    backgroundColor: '#fff7ea',
    '& fieldset': {
      borderColor: '#94704c',
    },
    '&:hover fieldset': {
      borderColor: '#94704c',
    },
    '&.Mui-focused fieldset': {
      borderColor: '#6f4d34',
      borderWidth: 2,
    },
  },
  '& .MuiInputLabel-root.Mui-focused': {
    color: '#5b4030',
  },
} as const

const flatPrimaryButtonSx = {
  borderRadius: 2,
  minHeight: 42,
  backgroundColor: '#5b4030',
  color: '#fff8ef',
  boxShadow: 'none',
  '&:hover': {
    backgroundColor: '#5b4030',
    boxShadow: 'none',
  },
} as const

const flatSecondaryButtonSx = {
  borderRadius: 2,
  minHeight: 36,
  borderColor: '#8b7a64',
  color: '#4f392c',
  backgroundColor: 'transparent',
  boxShadow: 'none',
  '&:hover': {
    borderColor: '#8b7a64',
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
      <Stack direction="row" justifyContent="flex-start">
        <HomeNavIconButton ariaLabel={locale.backToHome} />
      </Stack>

      <ThreadBoardSurface>
        <Paper variant="outlined" sx={{ ...boardSx, borderRadius: 3, p: { xs: 2, sm: 2.75 } }}>
          <Stack spacing={2}>
            <Typography variant="overline" sx={{ letterSpacing: '0.22em', color: 'rgba(246, 239, 220, 0.72)' }}>
              THREAD BOARD
            </Typography>
            <Stack
              direction={{ xs: 'column', lg: 'row' }}
              spacing={{ xs: 1, lg: 2 }}
              justifyContent="space-between"
              alignItems={{ xs: 'flex-start', lg: 'flex-end' }}
            >
              <Box>
                <Typography variant="h3" fontWeight={900} lineHeight={1} sx={{ color: '#f8efe4' }}>
                  {locale.title}
                </Typography>
              </Box>
              {data ? (
                <Stack
                  direction="row"
                  spacing={2}
                  flexWrap="wrap"
                  useFlexGap
                  sx={{ color: 'rgba(245, 234, 220, 0.72)' }}
                >
                  <Typography variant="body2">{locale.total.replace('{count}', String(data.totalCount))}</Typography>
                  <Typography variant="body2" sx={{ color: '#f0e2d3', fontWeight: 700 }}>
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
            sx={{ borderRadius: 3, p: { xs: 2, sm: 2.5 }, backgroundColor: '#d9c6aa', borderColor: '#94704c' }}
          >
            <Stack spacing={2}>
              <Box>
                <Typography variant="h5" fontWeight={900} sx={{ color: '#4f392c' }}>
                  {locale.form.heading}
                </Typography>
                <Box sx={{ mt: 1, width: 52, height: 4, borderRadius: 999, backgroundColor: '#295d63' }} />
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
              <Typography variant="caption" sx={{ color: 'rgba(79, 57, 44, 0.7)' }}>
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
            sx={{ borderRadius: 3, p: 0, overflow: 'hidden', backgroundColor: '#d9c6aa', borderColor: '#94704c' }}
          >
            <Stack spacing={0}>
              <Box sx={{ px: { xs: 2, sm: 2.5 }, py: 2, backgroundColor: '#8a6045' }}>
                <Stack direction="row" justifyContent="space-between" alignItems="center">
                  <Typography variant="h5" fontWeight={900} sx={{ color: '#fff8ef' }}>
                    {locale.listTitle}
                  </Typography>
                  <Box sx={{ width: 10, height: 10, borderRadius: '50%', backgroundColor: '#295d63' }} />
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
                          borderTop: index === 0 ? 'none' : '1px solid rgba(95, 63, 43, 0.18)',
                          backgroundColor: index % 2 === 0 ? '#d6c4ab' : '#cfbda2',
                        }}
                      >
                        <Stack spacing={1.25}>
                          <Stack
                            direction={{ xs: 'column', sm: 'row' }}
                            spacing={1.5}
                            justifyContent="space-between"
                            alignItems={{ xs: 'flex-start', sm: 'center' }}
                          >
                            <Stack direction="row" spacing={1.25} alignItems="center" sx={{ minWidth: 0 }}>
                              <Avatar
                                component={Link}
                                to={`/players/${thread.authorPlayerId}/visit`}
                                aria-label={locale.visitAuthorRoom.replace('{name}', thread.authorName)}
                                src={resolveCharacterAssetPath(thread.authorImagePath) ?? undefined}
                                alt={thread.authorName}
                                sx={{
                                  width: 42,
                                  height: 42,
                                  bgcolor: '#c7a57b',
                                  color: '#4f392c',
                                  textDecoration: 'none',
                                  transition: 'transform 140ms ease, box-shadow 140ms ease',
                                  '&:hover': {
                                    transform: 'translateY(-1px)',
                                    boxShadow: '0 6px 14px rgba(36, 20, 11, 0.16)',
                                  },
                                }}
                              >
                                {thread.authorName.slice(0, 1)}
                              </Avatar>
                              <Box sx={{ minWidth: 0 }}>
                                <Typography
                                  variant="h6"
                                  fontWeight={900}
                                  sx={{ color: '#4f392c', wordBreak: 'break-word' }}
                                >
                                  {thread.title}
                                </Typography>
                                <Typography variant="body2" sx={{ color: 'rgba(79, 57, 44, 0.72)' }}>
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
                            sx={{ color: '#432f22', whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}
                          >
                            {thread.previewBody}
                          </Typography>

                          <Stack direction={{ xs: 'column', md: 'row' }} spacing={1} justifyContent="space-between">
                            <Typography variant="caption" sx={{ color: 'rgba(79, 57, 44, 0.66)' }}>
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

                  <Box sx={{ px: { xs: 2, sm: 2.5 }, py: 2, borderTop: '1px solid rgba(95, 63, 43, 0.18)' }}>
                    <Stack alignItems="center">
                      <Pagination
                        page={page}
                        count={totalPages}
                        onChange={(_, nextPage) => setSearchParams({ page: String(nextPage) })}
                        sx={{
                          '& .MuiPaginationItem-root': {
                            color: '#4f392c',
                          },
                          '& .Mui-selected': {
                            backgroundColor: '#c7a57b !important',
                            color: '#4f392c',
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
