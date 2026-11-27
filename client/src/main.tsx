import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import { CssBaseline, ThemeProvider } from '@mui/material'
import { SWRConfig } from 'swr'
import './index.css'
import App from '@/App'
import { AuthProvider } from '@/contexts/AuthProvider'
import { theme } from '@/theme/theme'

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

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <SWRConfig value={{ revalidateOnFocus: false }}>
      <AuthProvider>
        <ThemeProvider theme={theme}>
          <CssBaseline />
          <BrowserRouter basename={getRouterBasename()}>
            <App />
          </BrowserRouter>
        </ThemeProvider>
      </AuthProvider>
    </SWRConfig>
  </StrictMode>,
)
