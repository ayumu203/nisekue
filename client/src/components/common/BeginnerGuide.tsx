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
import { useState } from 'react'
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

function shouldOpenGuideInitially(userId: string | null | undefined, pageKey: string): boolean {
  return Boolean(userId && !hasSeenBeginnerGuide(userId, pageKey))
}

export default function BeginnerGuide({ userId, guide, inverted = false, triggerSx }: BeginnerGuideProps) {
  const [open, setOpen] = useState(() => shouldOpenGuideInitially(userId, guide.pageKey))
  const theme = useTheme()
  const fullScreen = useMediaQuery(theme.breakpoints.down('sm'))
  const guidePalette = {
    home: {
      triggerBackgroundColor: '#78c27d',
      triggerHoverBackgroundColor: '#69b46f',
      triggerBorderColor: '#eef8ef',
      triggerTextColor: '#ffffff',
      triggerBadgeBackgroundColor: 'rgba(255,255,255,0.18)',
      triggerBadgeTextColor: 'rgba(255,255,255,0.8)',
      dialogBackgroundColor: '#f7f7f4',
      dialogBorderColor: '#d7d9d2',
      headerBackgroundColor: '#365b3a',
      headerBorderColor: '#a3cca7',
      headerTextColor: '#f5fbf5',
      headerMutedTextColor: 'rgba(245,251,245,0.78)',
      headerChipBackgroundColor: 'rgba(255,255,255,0.12)',
      headerChipBorderColor: 'rgba(163,204,167,0.72)',
      headerChipIconColor: '#edf8ee',
      closeButtonBackgroundColor: 'rgba(255,255,255,0.05)',
      closeButtonHoverBackgroundColor: 'rgba(255,255,255,0.12)',
      sectionBackgroundColor: '#ffffff',
      sectionBorderColor: '#dde0d8',
      sectionTitleColor: '#213c25',
      sectionBodyColor: '#3c5b41',
      indexBackgroundColor: '#365b3a',
      indexTextColor: '#eef8ef',
      indexBorderColor: '#9dc9a2',
      footerTextColor: '#4f6953',
    },
    quest: {
      triggerBackgroundColor: '#284a74',
      triggerHoverBackgroundColor: '#1f3d61',
      triggerBorderColor: '#b9cdee',
      triggerTextColor: '#f7fbff',
      triggerBadgeBackgroundColor: 'rgba(255,255,255,0.14)',
      triggerBadgeTextColor: 'rgba(226,239,255,0.82)',
      dialogBackgroundColor: '#f7f7f4',
      dialogBorderColor: '#d7d9d2',
      headerBackgroundColor: '#1f3554',
      headerBorderColor: '#9fbbe2',
      headerTextColor: '#f7fbff',
      headerMutedTextColor: 'rgba(231,240,251,0.8)',
      headerChipBackgroundColor: 'rgba(255,255,255,0.1)',
      headerChipBorderColor: 'rgba(159,187,226,0.72)',
      headerChipIconColor: '#dceafe',
      closeButtonBackgroundColor: 'rgba(255,255,255,0.05)',
      closeButtonHoverBackgroundColor: 'rgba(255,255,255,0.12)',
      sectionBackgroundColor: '#ffffff',
      sectionBorderColor: '#dde0e6',
      sectionTitleColor: '#1d3455',
      sectionBodyColor: '#3b5170',
      indexBackgroundColor: '#1f3554',
      indexTextColor: '#eaf3ff',
      indexBorderColor: '#9fbbe2',
      footerTextColor: '#556b86',
    },
    training: {
      triggerBackgroundColor: '#5b3430',
      triggerHoverBackgroundColor: '#4a2a27',
      triggerBorderColor: '#d3ab93',
      triggerTextColor: '#fff6ec',
      triggerBadgeBackgroundColor: 'rgba(255,255,255,0.14)',
      triggerBadgeTextColor: 'rgba(255,231,216,0.84)',
      dialogBackgroundColor: '#f7f7f4',
      dialogBorderColor: '#d7d9d2',
      headerBackgroundColor: '#472824',
      headerBorderColor: '#d0a08d',
      headerTextColor: '#fff6ef',
      headerMutedTextColor: 'rgba(255,236,224,0.8)',
      headerChipBackgroundColor: 'rgba(255,255,255,0.1)',
      headerChipBorderColor: 'rgba(208,160,141,0.74)',
      headerChipIconColor: '#ffe7dc',
      closeButtonBackgroundColor: 'rgba(255,255,255,0.05)',
      closeButtonHoverBackgroundColor: 'rgba(255,255,255,0.12)',
      sectionBackgroundColor: '#ffffff',
      sectionBorderColor: '#e2dddd',
      sectionTitleColor: '#5a312c',
      sectionBodyColor: '#764d47',
      indexBackgroundColor: '#472824',
      indexTextColor: '#fff1ea',
      indexBorderColor: '#d0a08d',
      footerTextColor: '#84665f',
    },
    items: {
      triggerBackgroundColor: '#8a6045',
      triggerHoverBackgroundColor: '#744f38',
      triggerBorderColor: '#e4c48c',
      triggerTextColor: '#fff9ef',
      triggerBadgeBackgroundColor: 'rgba(255,255,255,0.14)',
      triggerBadgeTextColor: 'rgba(255,236,204,0.84)',
      dialogBackgroundColor: '#f7f7f4',
      dialogBorderColor: '#d7d9d2',
      headerBackgroundColor: '#6a4833',
      headerBorderColor: '#ddb57e',
      headerTextColor: '#fff8f0',
      headerMutedTextColor: 'rgba(255,240,220,0.8)',
      headerChipBackgroundColor: 'rgba(255,255,255,0.1)',
      headerChipBorderColor: 'rgba(221,181,126,0.74)',
      headerChipIconColor: '#fff0d4',
      closeButtonBackgroundColor: 'rgba(255,255,255,0.05)',
      closeButtonHoverBackgroundColor: 'rgba(255,255,255,0.12)',
      sectionBackgroundColor: '#ffffff',
      sectionBorderColor: '#e3ded8',
      sectionTitleColor: '#654632',
      sectionBodyColor: '#7c5d48',
      indexBackgroundColor: '#6a4833',
      indexTextColor: '#fff3e3',
      indexBorderColor: '#ddb57e',
      footerTextColor: '#876b55',
    },
    'move-setting': {
      triggerBackgroundColor: '#295d63',
      triggerHoverBackgroundColor: '#224f54',
      triggerBorderColor: '#b8dbde',
      triggerTextColor: '#f3fcfc',
      triggerBadgeBackgroundColor: 'rgba(255,255,255,0.14)',
      triggerBadgeTextColor: 'rgba(224,248,248,0.82)',
      dialogBackgroundColor: '#f7f7f4',
      dialogBorderColor: '#d7d9d2',
      headerBackgroundColor: '#20464b',
      headerBorderColor: '#9dcdd0',
      headerTextColor: '#f3fcfc',
      headerMutedTextColor: 'rgba(226,248,248,0.8)',
      headerChipBackgroundColor: 'rgba(255,255,255,0.1)',
      headerChipBorderColor: 'rgba(157,205,208,0.74)',
      headerChipIconColor: '#d7f3f4',
      closeButtonBackgroundColor: 'rgba(255,255,255,0.05)',
      closeButtonHoverBackgroundColor: 'rgba(255,255,255,0.12)',
      sectionBackgroundColor: '#ffffff',
      sectionBorderColor: '#dde0e0',
      sectionTitleColor: '#21484d',
      sectionBodyColor: '#41656a',
      indexBackgroundColor: '#20464b',
      indexTextColor: '#ebfafb',
      indexBorderColor: '#9dcdd0',
      footerTextColor: '#557377',
    },
  }[guide.pageKey] ?? {
    triggerBackgroundColor: '#131a26',
    triggerHoverBackgroundColor: '#182131',
    triggerBorderColor: '#70613b',
    triggerTextColor: '#f5f7fb',
    triggerBadgeBackgroundColor: 'rgba(141,122,70,0.18)',
    triggerBadgeTextColor: 'rgba(232,221,189,0.78)',
    dialogBackgroundColor: '#f7f7f4',
    dialogBorderColor: '#d7d9d2',
    headerBackgroundColor: '#131a26',
    headerBorderColor: '#70613b',
    headerTextColor: '#f5f7fb',
    headerMutedTextColor: 'rgba(245,247,251,0.76)',
    headerChipBackgroundColor: 'rgba(141,122,70,0.18)',
    headerChipBorderColor: 'rgba(112,97,59,0.72)',
    headerChipIconColor: '#d8cba5',
    closeButtonBackgroundColor: 'rgba(255,255,255,0.04)',
    closeButtonHoverBackgroundColor: 'rgba(255,255,255,0.1)',
    sectionBackgroundColor: '#ffffff',
    sectionBorderColor: '#dde0e0',
    sectionTitleColor: '#152033',
    sectionBodyColor: '#314156',
    indexBackgroundColor: '#131a26',
    indexTextColor: '#e7ddbf',
    indexBorderColor: '#8d7a46',
    footerTextColor: '#556277',
  }

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
          color: guidePalette.triggerTextColor,
          border: '1px solid',
          borderColor: guidePalette.triggerBorderColor,
          backgroundColor: guidePalette.triggerBackgroundColor,
          boxShadow: inverted ? '0 10px 24px rgba(8, 12, 18, 0.18)' : '0 16px 36px rgba(10, 15, 23, 0.16)',
          '&:hover': {
            borderColor: guidePalette.triggerBorderColor,
            backgroundColor: guidePalette.triggerHoverBackgroundColor,
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
              backgroundColor: guidePalette.triggerBadgeBackgroundColor,
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
                color: guidePalette.triggerBadgeTextColor,
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
            backgroundColor: guidePalette.dialogBackgroundColor,
            border: `1px solid ${guidePalette.dialogBorderColor}`,
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
              backgroundColor: guidePalette.headerBackgroundColor,
              color: guidePalette.headerTextColor,
              borderBottom: `1px solid ${guidePalette.headerBorderColor}`,
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
                      color: guidePalette.headerTextColor,
                      borderRadius: 999,
                      backgroundColor: guidePalette.headerChipBackgroundColor,
                      border: `1px solid ${guidePalette.headerChipBorderColor}`,
                      '.MuiChip-icon': {
                        color: guidePalette.headerChipIconColor,
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
                        color: guidePalette.headerMutedTextColor,
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
                    color: guidePalette.headerTextColor,
                    border: `1px solid ${guidePalette.headerChipBorderColor}`,
                    backgroundColor: guidePalette.closeButtonBackgroundColor,
                    '&:hover': {
                      backgroundColor: guidePalette.closeButtonHoverBackgroundColor,
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
                  borderColor: guidePalette.sectionBorderColor,
                  backgroundColor: guidePalette.sectionBackgroundColor,
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
                      backgroundColor: guidePalette.indexBackgroundColor,
                      color: guidePalette.indexTextColor,
                      boxShadow: `inset 0 0 0 1px ${alpha(guidePalette.indexBorderColor, 0.48)}`,
                    }}
                  >
                    <Typography fontSize="0.95rem" fontWeight={900}>
                      {String(index + 1).padStart(2, '0')}
                    </Typography>
                  </Box>
                  <Stack spacing={0.75}>
                    <Typography variant="subtitle1" fontWeight={900} color={guidePalette.sectionTitleColor}>
                      {section.title}
                    </Typography>
                    <Typography
                      variant="body2"
                      sx={{ color: guidePalette.sectionBodyColor, whiteSpace: 'pre-line', lineHeight: 1.8 }}
                    >
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
            <Typography variant="caption" sx={{ color: guidePalette.footerTextColor, px: { sm: 0.5 } }}>
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
                backgroundColor: guidePalette.headerBackgroundColor,
                color: guidePalette.headerTextColor,
                boxShadow: 'none',
                border: `1px solid ${guidePalette.headerBorderColor}`,
                '&:hover': {
                  backgroundColor: guidePalette.triggerHoverBackgroundColor,
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
