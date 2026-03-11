import { useState } from 'react'
import { Alert, Box, Button, CircularProgress, Container, Paper, Stack, Typography } from '@mui/material'
import { Navigate } from 'react-router-dom'
import SignIn from '@/components/auth/SignIn'
import SignUp from '@/components/auth/SignUp'
import { innerSurfaceSx, outerPagePaperSx } from '@/constants/styles'
import { useAuth } from '@/contexts/useAuth'
import commonLocale from '../../locale/auth/Common.json'
import signInLocale from '../../locale/auth/SignIn.json'
import signUpLocale from '../../locale/auth/SignUp.json'

function Auth() {
  const { user, isLoading } = useAuth()
  const [message, setMessage] = useState<string | null>(null)
  const [mode, setMode] = useState<'signIn' | 'signUp'>('signIn')

  if (isLoading) {
    return (
      <Box minHeight="100vh" display="grid" sx={{ placeItems: 'center' }}>
        <Stack direction="row" spacing={1} alignItems="center">
          <CircularProgress size={20} />
          <Typography>{commonLocale.authLoading}</Typography>
        </Stack>
      </Box>
    )
  }

  if (user) {
    return <Navigate to="/" replace />
  }

  return (
    <Container maxWidth="sm" sx={{ py: 8 }}>
      <Paper elevation={2} sx={outerPagePaperSx}>
        <Stack spacing={3}>
          <Box
            sx={{
              display: 'grid',
              gridTemplateColumns: '1fr 1fr',
              gap: 1,
              p: 0.75,
              borderRadius: 999,
              backgroundColor: 'rgba(255, 247, 232, 0.55)',
            }}
          >
            <Button
              variant="contained"
              disableRipple
              onClick={() => {
                setMessage(null)
                setMode('signIn')
              }}
              sx={
                mode === 'signIn'
                  ? {
                      backgroundColor: '#78c27d',
                      borderColor: '#eef8ef',
                      color: '#ffffff',
                      transition: 'none',
                      '&:hover': {
                        backgroundColor: '#78c27d',
                        borderColor: '#eef8ef',
                        boxShadow: 'none',
                      },
                      '&:active': {
                        backgroundColor: '#78c27d',
                        borderColor: '#eef8ef',
                        boxShadow: 'none',
                      },
                    }
                  : {
                      backgroundColor: 'transparent',
                      borderColor: 'transparent',
                      color: '#5a3f2e',
                      boxShadow: 'none',
                      transition: 'none',
                      '&:hover': {
                        backgroundColor: 'transparent',
                        borderColor: 'transparent',
                        boxShadow: 'none',
                      },
                      '&:active': {
                        backgroundColor: 'transparent',
                        borderColor: 'transparent',
                        boxShadow: 'none',
                      },
                    }
              }
            >
              {signInLocale.title}
            </Button>
            <Button
              variant="contained"
              disableRipple
              onClick={() => {
                setMessage(null)
                setMode('signUp')
              }}
              sx={
                mode === 'signUp'
                  ? {
                      backgroundColor: '#78c27d',
                      borderColor: '#eef8ef',
                      color: '#ffffff',
                      transition: 'none',
                      '&:hover': {
                        backgroundColor: '#78c27d',
                        borderColor: '#eef8ef',
                        boxShadow: 'none',
                      },
                      '&:active': {
                        backgroundColor: '#78c27d',
                        borderColor: '#eef8ef',
                        boxShadow: 'none',
                      },
                    }
                  : {
                      backgroundColor: 'transparent',
                      borderColor: 'transparent',
                      color: '#5a3f2e',
                      boxShadow: 'none',
                      transition: 'none',
                      '&:hover': {
                        backgroundColor: 'transparent',
                        borderColor: 'transparent',
                        boxShadow: 'none',
                      },
                      '&:active': {
                        backgroundColor: 'transparent',
                        borderColor: 'transparent',
                        boxShadow: 'none',
                      },
                    }
              }
            >
              {signUpLocale.title}
            </Button>
          </Box>

          {message ? <Alert severity="info">{message}</Alert> : null}

          <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: { xs: 2, sm: 2.5 } }}>
            {mode === 'signIn' ? <SignIn onMessage={setMessage} /> : <SignUp onMessage={setMessage} />}
          </Paper>
        </Stack>
      </Paper>
    </Container>
  )
}

export default Auth
