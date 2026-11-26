import { createTheme } from '@mui/material'
import { font } from '@/theme/tokens'

const fontFamily = '"DotGothic16", sans-serif'

export const theme = createTheme({
  palette: {
    primary: {
      main: '#f4a6c1',
      dark: '#ea8db0',
      contrastText: '#ffffff',
    },
    background: {
      default: '#eaf3f8',
      paper: '#fff7e8',
    },
    text: {
      primary: '#4f4638',
      secondary: '#7a6f5c',
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
          borderColor: '#ffffff',
          boxShadow: 'none',
        },
        containedPrimary: {
          color: '#ffffff',
        },
        outlined: {
          borderWidth: 2,
          borderStyle: 'solid',
          borderColor: '#ffffff',
          color: '#ffffff',
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
