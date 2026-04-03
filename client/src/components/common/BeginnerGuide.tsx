import AutoStoriesRoundedIcon from '@mui/icons-material/AutoStoriesRounded'
import CloseRoundedIcon from '@mui/icons-material/CloseRounded'
import HelpOutlineIcon from '@mui/icons-material/HelpOutline'
import {
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  Paper,
  Stack,
  Typography,
  useMediaQuery,
} from '@mui/material'
import { alpha, type SxProps, type Theme, useTheme } from '@mui/material/styles'
import { useEffect, useState } from 'react'
import {
  beginnerGuideBadge,
  beginnerGuideCloseLabel,
  beginnerGuideFooterHint,
  beginnerGuideOpenLabel,
  type BeginnerGuideDefinition,
} from '@/lib/beginnerGuides'
import { hasSeenBeginnerGuide, markBeginnerGuideSeen } from '@/lib/beginnerGuideStorage'

type BeginnerGuideProps = {
  userId?: string | null
  guide: BeginnerGuideDefinition
  inverted?: boolean
  triggerSx?: SxProps<Theme>
}

export default function BeginnerGuide({ userId, guide, inverted = false, triggerSx }: BeginnerGuideProps) {
  const [open, setOpen] = useState(false)
  const theme = useTheme()
  const fullScreen = useMediaQuery(theme.breakpoints.down('sm'))
  const shellColor = '#131a26'
  const shellBorderColor = '#70613b'
  const shellTextColor = '#f5f7fb'
  const shellMutedTextColor = 'rgba(245, 247, 251, 0.76)'
  const surfaceColor = '#f4f5f1'
  const surfaceBorderColor = '#d5d9e1'
  const strongTextColor = '#152033'
  const bodyTextColor = '#314156'
  const accentColor = '#8d7a46'
  const triggerPalette =
    {
      home: {
        backgroundColor: '#78c27d',
        hoverBackgroundColor: '#69b46f',
        borderColor: '#eef8ef',
        textColor: '#ffffff',
        badgeBackgroundColor: 'rgba(255,255,255,0.18)',
        badgeTextColor: 'rgba(255,255,255,0.8)',
      },
      quest: {
        backgroundColor: '#284a74',
        hoverBackgroundColor: '#1f3d61',
        borderColor: '#b9cdee',
        textColor: '#f7fbff',
        badgeBackgroundColor: 'rgba(255,255,255,0.14)',
        badgeTextColor: 'rgba(226,239,255,0.82)',
      },
      training: {
        backgroundColor: '#5b3430',
        hoverBackgroundColor: '#4a2a27',
        borderColor: '#d3ab93',
        textColor: '#fff6ec',
        badgeBackgroundColor: 'rgba(255,255,255,0.14)',
        badgeTextColor: 'rgba(255,231,216,0.84)',
      },
      items: {
        backgroundColor: '#8a6045',
        hoverBackgroundColor: '#744f38',
        borderColor: '#e4c48c',
        textColor: '#fff9ef',
        badgeBackgroundColor: 'rgba(255,255,255,0.14)',
        badgeTextColor: 'rgba(255,236,204,0.84)',
      },
      'move-setting': {
        backgroundColor: '#295d63',
        hoverBackgroundColor: '#224f54',
        borderColor: '#b8dbde',
        textColor: '#f3fcfc',
        badgeBackgroundColor: 'rgba(255,255,255,0.14)',
        badgeTextColor: 'rgba(224,248,248,0.82)',
      },
    }[guide.pageKey] ?? {
      backgroundColor: shellColor,
      hoverBackgroundColor: '#182131',
      borderColor: shellBorderColor,
      textColor: shellTextColor,
      badgeBackgroundColor: 'rgba(141,122,70,0.18)',
      badgeTextColor: 'rgba(232,221,189,0.78)',
    }

  useEffect(() => {
    if (!userId || hasSeenBeginnerGuide(userId, guide.pageKey)) {
      return
    }

    setOpen(true)
  }, [guide.pageKey, userId])

  function handleClose(): void {
    if (userId) {
      markBeginnerGuideSeen(userId, guide.pageKey)
    }

    setOpen(false)
  }

  return (
    <>
      <Button
        variant="text"
        size="small"
        onClick={() => setOpen(true)}
        sx={{
          alignSelf: 'flex-start',
          borderRadius: 999,
          minHeight: 46,
          px: 1.75,
          color: triggerPalette.textColor,
          border: '1px solid',
          borderColor: triggerPalette.borderColor,
          backgroundColor: triggerPalette.backgroundColor,
          boxShadow: inverted ? '0 10px 24px rgba(8, 12, 18, 0.18)' : '0 16px 36px rgba(10, 15, 23, 0.16)',
          '&:hover': {
            borderColor: triggerPalette.borderColor,
            backgroundColor: triggerPalette.hoverBackgroundColor,
            boxShadow: inverted ? '0 14px 28px rgba(8, 12, 18, 0.24)' : '0 18px 40px rgba(10, 15, 23, 0.2)',
          },
          ...triggerSx,
        }}
      >
        <Stack direction="row" spacing={1.25} alignItems="center">
          <Box
            sx={{
              width: 28,
              height: 28,
              display: 'grid',
              placeItems: 'center',
              borderRadius: '50%',
              backgroundColor: triggerPalette.badgeBackgroundColor,
            }}
          >
            <HelpOutlineIcon fontSize="small" />
          </Box>
          <Stack spacing={0.15} alignItems="flex-start">
            <Typography
              variant="caption"
              sx={{
                lineHeight: 1,
                letterSpacing: '0.12em',
                textTransform: 'uppercase',
                color: triggerPalette.badgeTextColor,
              }}
            >
              {beginnerGuideBadge}
            </Typography>
            <Typography component="span" fontSize="0.95rem" fontWeight={800} lineHeight={1.1}>
              {beginnerGuideOpenLabel}
            </Typography>
          </Stack>
        </Stack>
      </Button>

      <Dialog
        open={open}
        onClose={handleClose}
        fullWidth
        maxWidth="md"
        fullScreen={fullScreen}
        PaperProps={{
          sx: {
            overflow: 'hidden',
            borderRadius: fullScreen ? 0 : 4,
            backgroundColor: surfaceColor,
            border: `1px solid ${alpha(shellBorderColor, 0.45)}`,
            boxShadow: '0 32px 80px rgba(7, 10, 15, 0.4)',
          },
        }}
      >
        <DialogTitle sx={{ p: 0 }}>
          <Box
            sx={{
              px: { xs: 2, sm: 3 },
              pt: { xs: 2, sm: 2.5 },
              pb: { xs: 2.5, sm: 3 },
              backgroundColor: shellColor,
              color: shellTextColor,
              borderBottom: `1px solid ${alpha(shellBorderColor, 0.56)}`,
            }}
          >
            <Stack spacing={2}>
              <Stack direction="row" justifyContent="space-between" alignItems="flex-start" spacing={2}>
                <Stack spacing={1.25} sx={{ minWidth: 0 }}>
                  <Chip
                    icon={<AutoStoriesRoundedIcon />}
                    label={beginnerGuideBadge}
                    sx={{
                      alignSelf: 'flex-start',
                      color: shellTextColor,
                      borderRadius: 999,
                      backgroundColor: 'rgba(141,122,70,0.18)',
                      border: `1px solid ${alpha(shellBorderColor, 0.72)}`,
                      '.MuiChip-icon': {
                        color: '#d8cba5',
                      },
                    }}
                  />
                  <Stack spacing={0.75}>
                    <Typography
                      variant={fullScreen ? 'h5' : 'h4'}
                      fontWeight={900}
                      lineHeight={1.1}
                      sx={{ textWrap: 'balance' }}
                    >
                      {guide.title.trim()}
                    </Typography>
                    <Typography
                      variant="body1"
                      sx={{
                        maxWidth: 560,
                        color: shellMutedTextColor,
                        whiteSpace: 'pre-line',
                      }}
                    >
                      {guide.description}
                    </Typography>
                  </Stack>
                </Stack>
                <IconButton
                  aria-label={beginnerGuideCloseLabel}
                  onClick={handleClose}
                  sx={{
                    color: shellTextColor,
                    border: `1px solid ${alpha(shellBorderColor, 0.72)}`,
                    backgroundColor: 'rgba(255,255,255,0.04)',
                    '&:hover': {
                      backgroundColor: 'rgba(255,255,255,0.1)',
                    },
                  }}
                >
                  <CloseRoundedIcon />
                </IconButton>
              </Stack>
            </Stack>
          </Box>
        </DialogTitle>
        <DialogContent sx={{ px: { xs: 2, sm: 3 }, py: { xs: 2, sm: 3 } }}>
          <Stack spacing={1.5}>
            {guide.sections.map((section, index) => (
              <Paper
                key={section.title}
                variant="outlined"
                sx={{
                  p: { xs: 1.5, sm: 2 },
                  borderRadius: 3,
                  borderColor: surfaceBorderColor,
                  backgroundColor: '#fbfcf8',
                  boxShadow: '0 10px 24px rgba(17, 24, 39, 0.05)',
                }}
              >
                <Stack direction="row" spacing={1.5} alignItems="flex-start">
                  <Box
                    sx={{
                      width: 42,
                      height: 42,
                      flexShrink: 0,
                      display: 'grid',
                      placeItems: 'center',
                      borderRadius: 2.5,
                      backgroundColor: shellColor,
                      color: '#e7ddbf',
                      boxShadow: `inset 0 0 0 1px ${alpha(accentColor, 0.48)}`,
                    }}
                  >
                    <Typography fontSize="0.95rem" fontWeight={900}>
                      {String(index + 1).padStart(2, '0')}
                    </Typography>
                  </Box>
                  <Stack spacing={0.75}>
                    <Typography variant="subtitle1" fontWeight={900} color={strongTextColor}>
                      {section.title}
                    </Typography>
                    <Typography variant="body2" sx={{ color: bodyTextColor, whiteSpace: 'pre-line', lineHeight: 1.8 }}>
                      {section.body}
                    </Typography>
                  </Stack>
                </Stack>
              </Paper>
            ))}
          </Stack>
        </DialogContent>
        <DialogActions sx={{ px: { xs: 2, sm: 3 }, pb: { xs: 2, sm: 3 }, pt: 0 }}>
          <Box
            sx={{
              width: '100%',
              display: 'flex',
              justifyContent: { xs: 'stretch', sm: 'space-between' },
              alignItems: { xs: 'stretch', sm: 'center' },
              flexDirection: { xs: 'column', sm: 'row' },
              gap: 1.5,
            }}
          >
            <Typography variant="caption" sx={{ color: '#556277', px: { sm: 0.5 } }}>
              {beginnerGuideFooterHint}
            </Typography>
            <Button
              onClick={handleClose}
              variant="contained"
              sx={{
                minWidth: 140,
                borderRadius: 999,
                px: 2.5,
                py: 1,
                fontWeight: 800,
                backgroundColor: shellColor,
                color: shellTextColor,
                boxShadow: 'none',
                border: `1px solid ${shellBorderColor}`,
                '&:hover': {
                  backgroundColor: '#182131',
                  boxShadow: 'none',
                },
              }}
            >
              {beginnerGuideCloseLabel}
            </Button>
          </Box>
        </DialogActions>
      </Dialog>
    </>
  )
}
