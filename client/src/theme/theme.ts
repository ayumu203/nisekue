import { createTheme } from '@mui/material'
import { font, palette } from '@/theme/tokens'

const fontFamily = '"DotGothic16", sans-serif'

export const theme = createTheme({
  palette: {
    primary: {
      main: palette.pink,
      contrastText: palette.white,
    },
    background: {
      default: palette.sky,
      paper: palette.cream,
    },
    text: {
      primary: palette.brown900,
      secondary: palette.brown500,
    },
  },
  typography: {
    fontFamily,
    // variant の大きさも規定値の 9 段階に合わせる
    h1: { fontSize: font.display },
    h2: { fontSize: font.display },
    h3: { fontSize: font.display },
    h4: { fontSize: font.xxxl },
    h5: { fontSize: font.xxl },
    h6: { fontSize: font.xl },
    subtitle1: { fontSize: font.lg },
    subtitle2: { fontSize: font.sm },
    body1: { fontSize: font.md },
    body2: { fontSize: font.sm },
    button: { fontSize: font.sm },
    caption: { fontSize: font.xs },
    overline: { fontSize: font.xxs },
  },
  components: {
    MuiButton: {
      styleOverrides: {
        root: {
          borderRadius: 999,
          textTransform: 'none',
          fontWeight: 900,
          fontSize: font.md,
          '@media (min-width:900px)': {
            fontSize: font.lg,
          },
        },
        contained: {
          borderWidth: 2,
          borderStyle: 'solid',
          borderColor: palette.white,
          boxShadow: 'none',
        },
        containedPrimary: {
          color: palette.white,
        },
        outlined: {
          borderWidth: 2,
          borderStyle: 'solid',
          borderColor: palette.white,
          color: palette.white,
        },
        text: {
          border: 'none',
        },
      },
    },
    MuiCssBaseline: {
      styleOverrides: {
        body: {
          backgroundColor: 'var(--app-bg-color)',
        },
      },
    },
  },
})
