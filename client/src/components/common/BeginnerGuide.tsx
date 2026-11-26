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
  beginnerGuideCloseLabel,
  beginnerGuideFooterHint,
  beginnerGuideLabel,
  type BeginnerGuideDefinition,
} from '@/lib/beginnerGuides'
import { markBeginnerGuideSeen } from '@/lib/beginnerGuideStorage'
import { font, palette, withAlpha } from '@/theme/tokens'

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
  const guidePalette = {
    home: {
      triggerBackgroundColor: palette.green400,
      triggerHoverBackgroundColor: palette.green400,
      triggerBorderColor: palette.white,
      triggerTextColor: palette.white,
      triggerBadgeBackgroundColor: withAlpha(palette.white, 0.25),
      dialogBackgroundColor: palette.white,
      dialogBorderColor: palette.gray300,
      headerBackgroundColor: palette.green700,
      headerBorderColor: palette.teal400,
      headerTextColor: palette.white,
      headerMutedTextColor: withAlpha(palette.white, 0.75),
      headerChipBackgroundColor: withAlpha(palette.white, 0.1),
      headerChipBorderColor: withAlpha(palette.teal400, 0.75),
      headerChipIconColor: palette.cream,
      closeButtonBackgroundColor: withAlpha(palette.white, 0.1),
      closeButtonHoverBackgroundColor: withAlpha(palette.white, 0.1),
      sectionBackgroundColor: palette.white,
      sectionBorderColor: palette.gray300,
      sectionTitleColor: palette.green700,
      sectionBodyColor: palette.green700,
      indexBackgroundColor: palette.green700,
      indexTextColor: palette.white,
      indexBorderColor: palette.teal400,
      footerTextColor: palette.green700,
    },
    quest: {
      triggerBackgroundColor: palette.teal700,
      triggerHoverBackgroundColor: palette.navy,
      triggerBorderColor: palette.blue300,
      triggerTextColor: palette.white,
      triggerBadgeBackgroundColor: withAlpha(palette.white, 0.1),
      dialogBackgroundColor: palette.white,
      dialogBorderColor: palette.gray300,
      headerBackgroundColor: palette.navy,
      headerBorderColor: palette.blue300,
      headerTextColor: palette.white,
      headerMutedTextColor: withAlpha(palette.sky, 0.75),
      headerChipBackgroundColor: withAlpha(palette.white, 0.1),
      headerChipBorderColor: withAlpha(palette.blue300, 0.75),
      headerChipIconColor: palette.sky,
      closeButtonBackgroundColor: withAlpha(palette.white, 0.1),
      closeButtonHoverBackgroundColor: withAlpha(palette.white, 0.1),
      sectionBackgroundColor: palette.white,
      sectionBorderColor: palette.sky,
      sectionTitleColor: palette.navy,
      sectionBodyColor: palette.teal700,
      indexBackgroundColor: palette.navy,
      indexTextColor: palette.sky,
      indexBorderColor: palette.blue300,
      footerTextColor: palette.teal700,
    },
    training: {
      triggerBackgroundColor: palette.brown900,
      triggerHoverBackgroundColor: palette.brown900,
      triggerBorderColor: palette.brown200,
      triggerTextColor: palette.cream,
      triggerBadgeBackgroundColor: withAlpha(palette.white, 0.1),
      dialogBackgroundColor: palette.white,
      dialogBorderColor: palette.gray300,
      headerBackgroundColor: palette.brown900,
      headerBorderColor: palette.brown200,
      headerTextColor: palette.cream,
      headerMutedTextColor: withAlpha(palette.cream, 0.75),
      headerChipBackgroundColor: withAlpha(palette.white, 0.1),
      headerChipBorderColor: withAlpha(palette.brown200, 0.75),
      headerChipIconColor: palette.cream,
      closeButtonBackgroundColor: withAlpha(palette.white, 0.1),
      closeButtonHoverBackgroundColor: withAlpha(palette.white, 0.1),
      sectionBackgroundColor: palette.white,
      sectionBorderColor: palette.gray300,
      sectionTitleColor: palette.brown900,
      sectionBodyColor: palette.brown900,
      indexBackgroundColor: palette.brown900,
      indexTextColor: palette.cream,
      indexBorderColor: palette.brown200,
      footerTextColor: palette.gray600,
    },
    items: {
      triggerBackgroundColor: palette.brown500,
      triggerHoverBackgroundColor: palette.brown900,
      triggerBorderColor: palette.brown200,
      triggerTextColor: palette.cream,
      triggerBadgeBackgroundColor: withAlpha(palette.white, 0.1),
      dialogBackgroundColor: palette.white,
      dialogBorderColor: palette.gray300,
      headerBackgroundColor: palette.brown900,
      headerBorderColor: palette.gold300,
      headerTextColor: palette.cream,
      headerMutedTextColor: withAlpha(palette.cream, 0.75),
      headerChipBackgroundColor: withAlpha(palette.white, 0.1),
      headerChipBorderColor: withAlpha(palette.gold300, 0.75),
      headerChipIconColor: palette.cream,
      closeButtonBackgroundColor: withAlpha(palette.white, 0.1),
      closeButtonHoverBackgroundColor: withAlpha(palette.white, 0.1),
      sectionBackgroundColor: palette.white,
      sectionBorderColor: palette.gray300,
      sectionTitleColor: palette.brown900,
      sectionBodyColor: palette.brown500,
      indexBackgroundColor: palette.brown900,
      indexTextColor: palette.cream,
      indexBorderColor: palette.gold300,
      footerTextColor: palette.brown500,
    },
    'move-setting': {
      triggerBackgroundColor: palette.teal700,
      triggerHoverBackgroundColor: palette.teal700,
      triggerBorderColor: palette.gray300,
      triggerTextColor: palette.white,
      triggerBadgeBackgroundColor: withAlpha(palette.white, 0.1),
      dialogBackgroundColor: palette.white,
      dialogBorderColor: palette.gray300,
      headerBackgroundColor: palette.teal700,
      headerBorderColor: palette.gray300,
      headerTextColor: palette.white,
      headerMutedTextColor: withAlpha(palette.sky, 0.75),
      headerChipBackgroundColor: withAlpha(palette.white, 0.1),
      headerChipBorderColor: withAlpha(palette.gray300, 0.75),
      headerChipIconColor: palette.sky,
      closeButtonBackgroundColor: withAlpha(palette.white, 0.1),
      closeButtonHoverBackgroundColor: withAlpha(palette.white, 0.1),
      sectionBackgroundColor: palette.white,
      sectionBorderColor: palette.gray300,
      sectionTitleColor: palette.teal700,
      sectionBodyColor: palette.teal700,
      indexBackgroundColor: palette.teal700,
      indexTextColor: palette.sky,
      indexBorderColor: palette.gray300,
      footerTextColor: palette.teal700,
    },
  }[guide.pageKey] ?? {
    triggerBackgroundColor: palette.navy,
    triggerHoverBackgroundColor: palette.navy,
    triggerBorderColor: palette.brown500,
    triggerTextColor: palette.sky,
    triggerBadgeBackgroundColor: withAlpha(palette.brown500, 0.25),
    dialogBackgroundColor: palette.white,
    dialogBorderColor: palette.gray300,
    headerBackgroundColor: palette.navy,
    headerBorderColor: palette.brown500,
    headerTextColor: palette.sky,
    headerMutedTextColor: withAlpha(palette.sky, 0.75),
    headerChipBackgroundColor: withAlpha(palette.brown500, 0.25),
    headerChipBorderColor: withAlpha(palette.brown500, 0.75),
    headerChipIconColor: palette.brown200,
    closeButtonBackgroundColor: withAlpha(palette.white, 0.1),
    closeButtonHoverBackgroundColor: withAlpha(palette.white, 0.1),
    sectionBackgroundColor: palette.white,
    sectionBorderColor: palette.gray300,
    sectionTitleColor: palette.navy,
    sectionBodyColor: palette.teal700,
    indexBackgroundColor: palette.navy,
    indexTextColor: palette.brown200,
    indexBorderColor: palette.brown500,
    footerTextColor: palette.teal700,
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
          px: 2,
          color: guidePalette.triggerTextColor,
          border: '1px solid',
          borderColor: guidePalette.triggerBorderColor,
          backgroundColor: guidePalette.triggerBackgroundColor,
          boxShadow: inverted
            ? `0 10px 24px ${withAlpha(palette.navy, 0.25)}`
            : `0 16px 36px ${withAlpha(palette.navy, 0.1)}`,
          ...triggerSx,
        }}
      >
        <Stack direction="row" spacing={1.5} alignItems="center">
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
          <Typography component="span" fontSize={font.md} fontWeight={800} lineHeight={1.1}>
            {beginnerGuideLabel}
          </Typography>
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
            boxShadow: `0 32px 80px ${withAlpha(palette.navy, 0.5)}`,
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
                <Stack spacing={1.5} sx={{ minWidth: 0 }}>
                  <Chip
                    icon={<AutoStoriesRoundedIcon />}
                    label={beginnerGuideLabel}
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
                    '&&': { backgroundColor: guidePalette.closeButtonBackgroundColor },
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
                  boxShadow: `0 10px 24px ${withAlpha(palette.navy, 0.1)}`,
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
                    <Typography fontSize={font.md} fontWeight={900}>
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
