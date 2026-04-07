import { Alert, Box, Button, Paper, Stack, TextField, Typography } from '@mui/material'
import { greenOutlinedInputSx, innerSurfaceSx, softGreenButtonSx } from '@/constants/styles'
import { resolveCharacterAssetPath } from '@/lib/assets'
import type { QuestRunDetailResponse } from '@/schema/quest'

type QuestLastTurnResultsPanelProps = {
  run: QuestRunDetailResponse | null | undefined
  chatMessage: string
  isChatSubmitting: boolean
  canPostChat: boolean
  chatDisabledReason?: string | null
  onChatMessageChange: (value: string) => void
  onSubmitChatMessage: () => void | Promise<void>
  locale: {
    chatPlaceholder: string
    chatSend: string
    chatSending: string
    currentTurnMessagesTitle: string
    lastTurnMessagesTitle: string
    lastTurnResultsTitle: string
    lastTurnResultsEmpty: string
    noImage: string
  }
}

function getLogToneColor(tone: 'Default' | 'Damage' | 'Ailment' | 'Buff' | 'Heal'): string {
  switch (tone) {
    case 'Damage':
      return '#c62828'
    case 'Ailment':
      return '#2e7d32'
    case 'Buff':
      return '#ef6c00'
    case 'Heal':
      return '#0288d1'
    default:
      return '#3b2f1f'
  }
}

function ChatMessageItem({
  displayName,
  imagePath,
  message,
  noImageLabel,
}: {
  displayName: string
  imagePath: string | null | undefined
  message: string
  noImageLabel: string
}) {
  const imageSrc = resolveCharacterAssetPath(imagePath)

  return (
    <Stack
      direction="row"
      spacing={1}
      sx={{
        px: 0.75,
        py: 0.85,
        borderRadius: 2,
        backgroundColor: 'rgba(255, 251, 241, 0.92)',
        border: '1px solid #dbcba7',
      }}
    >
      <Box
        sx={{
          width: 42,
          minWidth: 42,
          height: 42,
          borderRadius: 1.5,
          overflow: 'hidden',
          backgroundColor: '#f5efe2',
          border: '1px solid #d7c7a0',
          display: 'grid',
          placeItems: 'center',
        }}
      >
        {imageSrc ? (
          <Box
            component="img"
            src={imageSrc}
            alt={displayName}
            sx={{
              width: '100%',
              height: '100%',
              objectFit: 'contain',
              objectPosition: 'center bottom',
              display: 'block',
            }}
          />
        ) : (
          <Typography variant="caption" color="text.secondary" textAlign="center" sx={{ px: 0.5, lineHeight: 1.1 }}>
            {noImageLabel}
          </Typography>
        )}
      </Box>

      <Stack spacing={0.3} sx={{ minWidth: 0 }}>
        <Typography variant="caption" sx={{ fontWeight: 700, color: '#5a4421' }}>
          {displayName}
        </Typography>
        <Typography variant="body2" sx={{ color: '#322617', lineHeight: 1.45, wordBreak: 'break-word' }}>
          {message}
        </Typography>
      </Stack>
    </Stack>
  )
}

export default function QuestLastTurnResultsPanel({
  run,
  chatMessage,
  isChatSubmitting,
  canPostChat,
  chatDisabledReason = null,
  onChatMessageChange,
  onSubmitChatMessage,
  locale,
}: QuestLastTurnResultsPanelProps) {
  const maxQuestChatMessageLength = 50
  const isChatDisabled = !canPostChat || isChatSubmitting || Boolean(chatDisabledReason)
  const currentTurnMessages = run?.chatMessages ?? []
  const lastTurnMessages = run?.lastTurnResults?.chatMessages ?? []
  const logs =
    run?.lastTurnResults?.actions.flatMap((action) =>
      action.logEntries.length > 0
        ? action.logEntries
        : action.logs.map((log) => ({
            text: log,
            segments: [{ text: log, tone: 'Default' as const }],
          })),
    ) ?? []
  const hasContent = currentTurnMessages.length > 0 || lastTurnMessages.length > 0 || logs.length > 0

  return (
    <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: { xs: 1.5, sm: 2.5 } }}>
      <Stack spacing={2}>
        {chatDisabledReason ? <Alert severity="info">{chatDisabledReason}</Alert> : null}
        <Stack direction="row" spacing={1} alignItems="flex-start">
          <TextField
            value={chatMessage}
            onChange={(event) => onChatMessageChange(event.target.value.slice(0, maxQuestChatMessageLength))}
            placeholder={locale.chatPlaceholder}
            size="small"
            multiline
            maxRows={3}
            inputProps={{ maxLength: maxQuestChatMessageLength }}
            fullWidth
            disabled={isChatDisabled}
            sx={{
              ...greenOutlinedInputSx,
              '& .MuiInputBase-root': {
                backgroundColor: '#fffdf8',
              },
            }}
          />
          <Button
            variant="contained"
            onClick={() => void onSubmitChatMessage()}
            disabled={isChatDisabled || chatMessage.trim().length === 0}
            sx={{ minWidth: 78, minHeight: 40, ...softGreenButtonSx }}
          >
            {isChatSubmitting ? locale.chatSending : locale.chatSend}
          </Button>
        </Stack>

        {!hasContent ? (
          <Typography variant="body2" color="text.secondary">
            {locale.lastTurnResultsEmpty}
          </Typography>
        ) : (
          <Stack spacing={1}>
            {currentTurnMessages.length > 0 ? (
              <Stack spacing={0.75}>
                <Typography variant="subtitle2" sx={{ fontWeight: 700, color: '#5a4421' }}>
                  {locale.currentTurnMessagesTitle}
                </Typography>
                <Stack spacing={0.6}>
                  {currentTurnMessages.map((message, index) => (
                    <ChatMessageItem
                      key={`${message.senderParticipantId}-${message.sentAt}-${index}`}
                      displayName={message.displayName}
                      imagePath={message.imagePath}
                      message={message.message}
                      noImageLabel={locale.noImage}
                    />
                  ))}
                </Stack>
              </Stack>
            ) : null}

            {lastTurnMessages.length > 0 ? (
              <Stack spacing={0.75}>
                <Typography variant="subtitle2" sx={{ fontWeight: 700, color: '#5a4421' }}>
                  {locale.lastTurnMessagesTitle}
                </Typography>
                <Stack spacing={0.6}>
                  {lastTurnMessages.map((message, index) => (
                    <ChatMessageItem
                      key={`${message.senderParticipantId}-${message.sentAt}-${index}`}
                      displayName={message.displayName}
                      imagePath={message.imagePath}
                      message={message.message}
                      noImageLabel={locale.noImage}
                    />
                  ))}
                </Stack>
              </Stack>
            ) : null}

            {logs.length > 0 ? (
              <Stack spacing={0.5}>
                {logs.map((log, index) => (
                  <Box
                    key={`${log.text}-${index}`}
                    sx={{
                      px: 0.5,
                      py: 0.75,
                      borderBottom: '1px solid #c9c2b7',
                    }}
                  >
                    <Typography variant="body2" sx={{ fontWeight: 600, color: '#3b2f1f', lineHeight: 1.5 }}>
                      {log.segments.map((segment, segmentIndex) => (
                        <Box
                          key={`${segment.text}-${segmentIndex}`}
                          component="span"
                          sx={{ color: getLogToneColor(segment.tone), whiteSpace: 'pre-wrap' }}
                        >
                          {segment.text}
                        </Box>
                      ))}
                    </Typography>
                  </Box>
                ))}
              </Stack>
            ) : null}
          </Stack>
        )}
      </Stack>
    </Paper>
  )
}
