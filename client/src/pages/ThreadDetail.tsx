import { Alert, Avatar, Box, Button, CircularProgress, Paper, Stack, TextField, Typography } from '@mui/material'
import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import useSWR from 'swr'
import { createThreadReply, deleteThread, getThreadDetail } from '@/api/thread'
import ThreadBoardSurface from '@/components/common/ThreadBoardSurface'
import HomeNavIconButton from '@/components/common/HomeNavIconButton'
import ThreadPageShell from '@/components/common/ThreadPageShell'
import { useAuth } from '@/contexts/useAuth'
import { resolveCharacterAssetPath } from '@/lib/assets'
import locale from '../../locale/threads/ThreadDetail.json'

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

const deleteButtonSx = {
  borderRadius: 2,
  minHeight: 36,
  borderColor: '#b22c1c',
  color: '#b22c1c',
  backgroundColor: 'transparent',
  boxShadow: 'none',
  '&:hover': {
    borderColor: '#b22c1c',
    backgroundColor: '#ffece8',
    boxShadow: 'none',
  },
} as const

function formatDate(value: string | null): string {
  if (!value) {
    return locale.noReplies
  }

  return new Date(value).toLocaleString('ja-JP')
}

export default function ThreadDetail() {
  const { threadId } = useParams<{ threadId: string }>()
  const { session, isLoading } = useAuth()
  const navigate = useNavigate()
  const [replyBody, setReplyBody] = useState('')
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [deleteError, setDeleteError] = useState<string | null>(null)
  const [isDeleting, setIsDeleting] = useState(false)

  const swrKey = session?.access_token && threadId ? (['thread', threadId] as const) : null
  const {
    data,
    error,
    isLoading: isThreadLoading,
    mutate,
  } = useSWR(swrKey, async () => {
    if (!session?.access_token || !threadId) {
      throw new Error(locale.threadMissing)
    }

    return getThreadDetail(threadId, session.access_token)
  })

  const canDelete = Boolean(session?.user?.id && data?.authorPlayerId && session.user.id === data.authorPlayerId)

  const handleDelete = async () => {
    if (!session?.access_token || !threadId) {
      setDeleteError(locale.threadMissing)
      return
    }

    if (!window.confirm(locale.deleteConfirm)) {
      return
    }

    setIsDeleting(true)
    setDeleteError(null)
    try {
      await deleteThread(threadId, session.access_token)
      navigate('/threads')
    } catch (error) {
      setDeleteError(error instanceof Error ? error.message : locale.deleteFailed)
    } finally {
      setIsDeleting(false)
    }
  }

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

  return (
    <ThreadPageShell>
      <Stack direction="row" justifyContent="space-between" alignItems="center">
        <HomeNavIconButton ariaLabel={locale.backToHome} />
        <Stack direction="row" spacing={1}>
          {canDelete ? (
            <Button variant="outlined" disabled={isDeleting} sx={deleteButtonSx} onClick={handleDelete}>
              {isDeleting ? <CircularProgress size={18} color="inherit" /> : locale.deleteButton}
            </Button>
          ) : null}
          <Button component={Link} to="/threads" variant="outlined" sx={{ ...flatSecondaryButtonSx, color: '#295d63' }}>
            {locale.backToList}
          </Button>
        </Stack>
      </Stack>

      <ThreadBoardSurface>
        {deleteError ? (
          <Box sx={{ mb: 2 }}>
            <Alert severity="warning">{deleteError}</Alert>
          </Box>
        ) : null}
        {isThreadLoading ? (
          <Stack direction="row" spacing={1} alignItems="center">
            <CircularProgress size={16} />
            <Typography variant="body2">{locale.loading}</Typography>
          </Stack>
        ) : error ? (
          <Alert severity="warning">{error.message}</Alert>
        ) : !data ? (
          <Alert severity="warning">{locale.threadMissing}</Alert>
        ) : (
          <>
            <Paper variant="outlined" sx={{ ...boardSx, borderRadius: 3, p: { xs: 2, sm: 2.75 } }}>
              <Stack spacing={2}>
                <Typography variant="overline" sx={{ letterSpacing: '0.22em', color: 'rgba(246, 239, 220, 0.76)' }}>
                  THREAD DETAIL
                </Typography>
                <Typography
                  variant="h3"
                  fontWeight={900}
                  lineHeight={1.05}
                  sx={{
                    color: '#f8efe4',
                    wordBreak: 'break-word',
                    fontSize: { xs: '1.25rem', sm: '2.4rem', md: '2.85rem' },
                  }}
                >
                  {data.title}
                </Typography>

                <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} justifyContent="space-between">
                  <Stack direction="row" spacing={1.5} alignItems="center">
                    <Avatar
                      component={Link}
                      to={`/players/${data.authorPlayerId}/visit`}
                      aria-label={locale.visitAuthorRoom.replace('{name}', data.authorName)}
                      src={resolveCharacterAssetPath(data.authorImagePath) ?? undefined}
                      alt={data.authorName}
                      sx={{
                        width: 56,
                        height: 56,
                        bgcolor: '#c7a57b',
                        color: '#4f392c',
                        textDecoration: 'none',
                        transition: 'transform 140ms ease, box-shadow 140ms ease',
                        '&:hover': {
                          transform: 'translateY(-1px)',
                          boxShadow: '0 8px 18px rgba(36, 20, 11, 0.18)',
                        },
                      }}
                    >
                      {data.authorName.slice(0, 1)}
                    </Avatar>
                    <Box>
                      <Typography variant="h6" fontWeight={900} sx={{ color: '#f8efe4' }}>
                        {data.authorName}
                      </Typography>
                      <Typography variant="body2" sx={{ color: 'rgba(64, 43, 28, 0.72)' }}>
                        {locale.createdAt.replace('{value}', formatDate(data.createdAt))}
                      </Typography>
                    </Box>
                  </Stack>

                  <Box sx={{ color: 'rgba(245, 234, 220, 0.9)' }}>
                    <Typography variant="body2" sx={{ color: '#f0e2d3' }}>
                      {locale.replyCount
                        .replace('{count}', String(data.replies.length))
                        .replace('{last}', formatDate(data.lastRepliedAt))}
                    </Typography>
                  </Box>
                </Stack>
              </Stack>
            </Paper>

            <Paper
              variant="outlined"
              sx={{ borderRadius: 3, p: { xs: 2, sm: 2.5 }, backgroundColor: '#d9c6aa', borderColor: '#94704c' }}
            >
              <Typography
                variant="body1"
                sx={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word', color: '#432f22', lineHeight: 1.85 }}
              >
                {data.body}
              </Typography>
            </Paper>

            <Paper
              variant="outlined"
              sx={{ borderRadius: 3, p: { xs: 2, sm: 2.5 }, backgroundColor: '#d9c6aa', borderColor: '#94704c' }}
            >
              <Stack spacing={2}>
                <Box>
                  <Typography variant="h5" fontWeight={900} sx={{ color: '#4f392c' }}>
                    {locale.replyForm.title}
                  </Typography>
                  <Box sx={{ mt: 1, width: 52, height: 4, borderRadius: 999, backgroundColor: '#295d63' }} />
                </Box>

                {submitError ? <Alert severity="warning">{submitError}</Alert> : null}

                <TextField
                  label={locale.replyForm.body}
                  value={replyBody}
                  onChange={(event) => setReplyBody(event.target.value)}
                  multiline
                  minRows={5}
                  inputProps={{ maxLength: 250 }}
                  sx={inputSx}
                />
                <Typography variant="caption" sx={{ color: 'rgba(79, 57, 44, 0.68)' }}>
                  {locale.replyForm.helper.replace('{count}', String(replyBody.length))}
                </Typography>
                <Button
                  variant="contained"
                  disabled={isSubmitting}
                  sx={flatPrimaryButtonSx}
                  onClick={async () => {
                    if (!session?.access_token || !threadId) {
                      setSubmitError(locale.threadMissing)
                      return
                    }

                    setIsSubmitting(true)
                    setSubmitError(null)
                    try {
                      const updated = await createThreadReply(threadId, { body: replyBody }, session.access_token)
                      setReplyBody('')
                      await mutate(updated, { revalidate: false })
                    } catch (submitError) {
                      setSubmitError(submitError instanceof Error ? submitError.message : locale.replyFailed)
                    } finally {
                      setIsSubmitting(false)
                    }
                  }}
                >
                  {isSubmitting ? locale.replyForm.submitting : locale.replyForm.submit}
                </Button>
              </Stack>
            </Paper>

            <Paper
              variant="outlined"
              sx={{ borderRadius: 3, p: 0, overflow: 'hidden', backgroundColor: '#d9c6aa', borderColor: '#94704c' }}
            >
              <Box sx={{ px: { xs: 2, sm: 2.5 }, py: 2, backgroundColor: '#8a6045' }}>
                <Stack direction="row" justifyContent="space-between" alignItems="center">
                  <Typography variant="h5" fontWeight={900} sx={{ color: '#fff8ef' }}>
                    {locale.repliesTitle}
                  </Typography>
                  <Box sx={{ width: 10, height: 10, borderRadius: '50%', backgroundColor: '#295d63' }} />
                </Stack>
              </Box>

              {data.replies.length === 0 ? (
                <Box sx={{ px: { xs: 2, sm: 2.5 }, py: 2.5 }}>
                  <Alert severity="info">{locale.emptyReplies}</Alert>
                </Box>
              ) : (
                <Stack spacing={0}>
                  {data.replies.map((reply, index) => (
                    <Box
                      key={reply.id}
                      sx={{
                        px: { xs: 2, sm: 2.5 },
                        py: 2,
                        borderTop: index === 0 ? 'none' : '1px solid rgba(95, 63, 43, 0.18)',
                        backgroundColor: index % 2 === 0 ? '#d6c4ab' : '#cfbda2',
                      }}
                    >
                      <Stack spacing={1.25}>
                        <Stack direction="row" spacing={1.25} alignItems="center">
                          <Avatar
                            component={Link}
                            to={`/players/${reply.authorPlayerId}/visit`}
                            aria-label={locale.visitAuthorRoom.replace('{name}', reply.authorName)}
                            src={resolveCharacterAssetPath(reply.authorImagePath) ?? undefined}
                            alt={reply.authorName}
                            sx={{
                              width: 40,
                              height: 40,
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
                            {reply.authorName.slice(0, 1)}
                          </Avatar>
                          <Box>
                            <Typography variant="subtitle1" fontWeight={900} sx={{ color: '#4f392c' }}>
                              {reply.authorName}
                            </Typography>
                            <Typography variant="caption" sx={{ ...accentSx, opacity: 0.84 }}>
                              {formatDate(reply.createdAt)}
                            </Typography>
                          </Box>
                        </Stack>

                        <Typography
                          variant="body2"
                          sx={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word', color: '#432f22', lineHeight: 1.75 }}
                        >
                          {reply.body}
                        </Typography>
                      </Stack>
                    </Box>
                  ))}
                </Stack>
              )}
            </Paper>
          </>
        )}
      </ThreadBoardSurface>
    </ThreadPageShell>
  )
}
