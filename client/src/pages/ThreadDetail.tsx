import {
  Alert,
  Avatar,
  Box,
  Button,
  CircularProgress,
  Paper,
  Stack,
  TextField,
  Typography,
  useMediaQuery,
  type Theme,
} from '@mui/material'
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
import { font, palette, withAlpha } from '@/theme/tokens'

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
    '--input-outline-color': palette.brown500,
    '& fieldset': {
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
} as const

const flatSecondaryButtonSx = {
  borderRadius: 2,
  minHeight: 36,
  borderColor: palette.gray600,
  color: palette.brown900,
  backgroundColor: 'transparent',
  boxShadow: 'none',
} as const

const deleteButtonSx = {
  borderRadius: 2,
  minHeight: 36,
  borderColor: palette.red700,
  color: palette.red700,
  backgroundColor: 'transparent',
  boxShadow: 'none',
} as const

function formatDate(value: string | null): string {
  if (!value) {
    return locale.noReplies
  }

  return new Date(value).toLocaleString('ja-JP')
}

export default function ThreadDetail() {
  const { threadId } = useParams<{ threadId: string }>()
  const { session, isLoading, isAnonymous } = useAuth()
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

  return (
    <ThreadPageShell>
      <Stack direction="row" justifyContent={isDesktop ? 'flex-end' : 'space-between'} alignItems="center">
        {isDesktop ? null : <HomeNavIconButton ariaLabel={locale.backToHome} />}
        <Stack direction="row" spacing={1}>
          {canDelete ? (
            <Button variant="outlined" disabled={isDeleting} sx={deleteButtonSx} onClick={handleDelete}>
              {isDeleting ? <CircularProgress size={18} color="inherit" /> : locale.deleteButton}
            </Button>
          ) : null}
          <Button
            component={Link}
            to="/threads"
            variant="outlined"
            sx={{ ...flatSecondaryButtonSx, color: palette.teal700 }}
          >
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
            <Paper variant="outlined" sx={{ ...boardSx, borderRadius: 3, p: { xs: 2, sm: 3 } }}>
              <Stack spacing={2}>
                <Typography variant="overline" sx={{ letterSpacing: '0.22em', color: withAlpha(palette.cream, 0.75) }}>
                  THREAD DETAIL
                </Typography>
                <Typography
                  variant="h3"
                  fontWeight={900}
                  lineHeight={1.05}
                  sx={{
                    color: palette.cream,
                    wordBreak: 'break-word',
                    fontSize: { xs: font.xl, sm: font.display, md: font.display },
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
                        bgcolor: palette.brown200,
                        color: palette.brown900,
                        textDecoration: 'none',
                        transition: 'transform 140ms ease, box-shadow 140ms ease',
                      }}
                    >
                      {data.authorName.slice(0, 1)}
                    </Avatar>
                    <Box>
                      <Typography variant="h6" fontWeight={900} sx={{ color: palette.cream }}>
                        {data.authorName}
                      </Typography>
                      <Typography variant="body2" sx={{ color: withAlpha(palette.brown900, 0.75) }}>
                        {locale.createdAt.replace('{value}', formatDate(data.createdAt))}
                      </Typography>
                    </Box>
                  </Stack>

                  <Box sx={{ color: withAlpha(palette.cream, 0.9) }}>
                    <Typography variant="body2" sx={{ color: palette.cream }}>
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
              sx={{
                borderRadius: 3,
                p: { xs: 2, sm: 2.5 },
                backgroundColor: palette.brown200,
                borderColor: palette.brown500,
              }}
            >
              <Typography
                variant="body1"
                sx={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word', color: palette.brown900, lineHeight: 1.85 }}
              >
                {data.body}
              </Typography>
            </Paper>

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
                    {locale.replyForm.title}
                  </Typography>
                  <Box sx={{ mt: 1, width: 52, height: 4, borderRadius: 999, backgroundColor: palette.teal700 }} />
                </Box>

                {isAnonymous ? <Alert severity="info">{locale.anonymousPostingRestricted}</Alert> : null}
                {submitError ? <Alert severity="warning">{submitError}</Alert> : null}

                <TextField
                  label={locale.replyForm.body}
                  value={replyBody}
                  onChange={(event) => setReplyBody(event.target.value)}
                  multiline
                  minRows={5}
                  inputProps={{ maxLength: 250 }}
                  disabled={isAnonymous || isSubmitting}
                  sx={inputSx}
                />
                <Typography variant="caption" sx={{ color: withAlpha(palette.brown900, 0.75) }}>
                  {locale.replyForm.helper.replace('{count}', String(replyBody.length))}
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
              sx={{
                borderRadius: 3,
                p: 0,
                overflow: 'hidden',
                backgroundColor: palette.brown200,
                borderColor: palette.brown500,
              }}
            >
              <Box sx={{ px: { xs: 2, sm: 2.5 }, py: 2, backgroundColor: palette.brown500 }}>
                <Stack direction="row" justifyContent="space-between" alignItems="center">
                  <Typography variant="h5" fontWeight={900} sx={{ color: palette.cream }}>
                    {locale.repliesTitle}
                  </Typography>
                  <Box sx={{ width: 10, height: 10, borderRadius: '50%', backgroundColor: palette.teal700 }} />
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
                        borderTop: index === 0 ? 'none' : `1px solid ${withAlpha(palette.brown900, 0.25)}`,
                        backgroundColor: index % 2 === 0 ? palette.brown200 : palette.brown200,
                      }}
                    >
                      <Stack spacing={1.5}>
                        <Stack direction="row" spacing={1.5} alignItems="center">
                          <Avatar
                            component={Link}
                            to={`/players/${reply.authorPlayerId}/visit`}
                            aria-label={locale.visitAuthorRoom.replace('{name}', reply.authorName)}
                            src={resolveCharacterAssetPath(reply.authorImagePath) ?? undefined}
                            alt={reply.authorName}
                            sx={{
                              width: 40,
                              height: 40,
                              bgcolor: palette.brown200,
                              color: palette.brown900,
                              textDecoration: 'none',
                              transition: 'transform 140ms ease, box-shadow 140ms ease',
                            }}
                          >
                            {reply.authorName.slice(0, 1)}
                          </Avatar>
                          <Box>
                            <Typography variant="subtitle1" fontWeight={900} sx={{ color: palette.brown900 }}>
                              {reply.authorName}
                            </Typography>
                            <Typography variant="caption" sx={{ ...accentSx, opacity: 0.84 }}>
                              {formatDate(reply.createdAt)}
                            </Typography>
                          </Box>
                        </Stack>

                        <Typography
                          variant="body2"
                          sx={{
                            whiteSpace: 'pre-wrap',
                            wordBreak: 'break-word',
                            color: palette.brown900,
                            lineHeight: 1.75,
                          }}
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
