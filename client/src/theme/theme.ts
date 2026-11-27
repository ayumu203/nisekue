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
    // ホバーで色を変えない：MUI の部品がホバー時に重ねる色を無くす
    action: {
      hover: 'transparent',
      hoverOpacity: 0,
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
      // ホバーで影を変えない
      defaultProps: { disableElevation: true },
      styleOverrides: {
        root: ({ ownerState, theme }) => {
          const color = ownerState.color ?? 'primary'
          const main =
            color === 'inherit'
              ? theme.palette.grey[300]
              : (theme.palette[color] as { main?: string } | undefined)?.main
          return {
            borderRadius: 999,
            textTransform: 'none',
            fontWeight: 900,
            fontSize: font.md,
            '@media (min-width:900px)': {
              fontSize: font.lg,
            },
            // ホバーで色を変えない：contained の背景を通常時と同じ色に保つ
            '@media (hover: hover)': {
              '&:hover': {
                '--variant-containedBg': main,
                ...(color === 'inherit' ? { backgroundColor: main } : {}),
              },
            },
          }
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
    MuiOutlinedInput: {
      styleOverrides: {
        // ホバーで枠の色を変えない。枠の色を変えた入力欄は sx で --input-outline-color も指定する
        root: {
          '&:hover:not(.Mui-focused):not(.Mui-error):not(.Mui-disabled) .MuiOutlinedInput-notchedOutline': {
            borderColor: 'var(--input-outline-color, rgba(0, 0, 0, 0.23))',
          },
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
