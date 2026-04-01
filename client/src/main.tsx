import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import { CssBaseline, ThemeProvider, createTheme } from '@mui/material'
import './index.css'
import App from '@/App'
import { AuthProvider } from '@/contexts/AuthProvider'

function getRouterBasename(): string | undefined {
  const normalizedBaseUrl = (import.meta.env.BASE_URL ?? '/').replace(/\/+$/, '')
  return normalizedBaseUrl === '' || normalizedBaseUrl === '/' ? undefined : normalizedBaseUrl
}

function restoreGithubPagesPath(): void {
  if (typeof window === 'undefined') {
    return
  }

  const locationUrl = new URL(window.location.href)
  const redirectedPath = locationUrl.searchParams.get('p')
  if (!redirectedPath) {
    return
  }

  const nextPath = redirectedPath.startsWith('/') ? redirectedPath : `/${redirectedPath}`
  window.history.replaceState(null, '', `${nextPath}${locationUrl.hash}`)
}

restoreGithubPagesPath()

const theme = createTheme({
  palette: {
    primary: {
      main: '#f4a6c1',
      dark: '#ea8db0',
      contrastText: '#ffffff',
    },
    background: {
      default: 'var(--app-bg-color)',
      paper: '#fff7e8',
    },
    text: {
      primary: '#4f4638',
      secondary: '#7a6f5c',
    },
  },
  typography: {
    fontFamily: '"DotGothic16", sans-serif',
  },
  components: {
    MuiButton: {
      styleOverrides: {
        root: {
          borderRadius: 999,
          textTransform: 'none',
          fontWeight: 900,
          fontSize: '0.95rem',
          '@media (min-width:900px)': {
            fontSize: '1.08rem',
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

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <AuthProvider>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <BrowserRouter basename={getRouterBasename()}>
          <App />
        </BrowserRouter>
      </ThemeProvider>
    </AuthProvider>
  </StrictMode>,
)
