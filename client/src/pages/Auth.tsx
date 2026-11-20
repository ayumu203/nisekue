import { useState } from 'react'
import type { FormEvent } from 'react'
import { Alert, Box, Button, CircularProgress, Divider, Link, Paper, Stack, TextField, Typography } from '@mui/material'
import { Navigate } from 'react-router-dom'
import { googleButtonSx, softGoldButtonSx } from '@/constants/styles'
import { useAuth } from '@/contexts/useAuth'
import { resolvePublicAssetPath } from '@/lib/assets'
import { supabase } from '@/lib/supabase'
import GoogleColorIcon from '@/components/common/GoogleColorIcon'
import locale from '../../locale/auth/Auth.json'

type SubmitMode = 'google' | 'signIn' | 'signUp' | 'anonymous' | null
type EmailAuthMode = 'signIn' | 'signUp'

// サムネ（Notion「ゲームとしては完成してきたのでプレイヤーを増やしたい」案C v18）と同じ「魔王の玉座の間」の構図。
// 背景は tools/auth-hero/generate_throne_hall.py で生成した自作のドット絵（1200x1200）で、
// 下側 1200x630 がサムネと同じ「ステージ」。キャラ・敵の素材は加工せず、ステージ基準の割合で重ねる。
const heroBackgroundUrl = resolvePublicAssetPath('image/auth/throne-hall.png')
const heroCeilingColor = '#120d2c'

// フォーム脇に出すマスコット。ヒーローで使っているキャラ（ch109・ch173・ch175）は重複を避けるため除外する。
const formMascotPaths = [
  'image/character/ch110b_hero.png',
  'image/character/ch111_hero.png',
  'image/character/ch112b_hero.png',
  'image/character/ch112c_hero.png',
  'image/character/ch118b_hero.png',
  'image/character/ch119_hero.png',
  'image/character/ch119b_hero.png',
  'image/character/ch122_hero.png',
  'image/character/ch122b_hero.png',
  'image/character/ch123_hero.png',
  'image/character/ch129_hero.png',
  'image/character/ch130_hero.png',
  'image/character/ch130b_hero.png',
  'image/character/ch130c_hero.png',
  'image/character/ch132_hero.png',
  'image/character/ch171_hero.png',
  'image/character/ch172_hero.png',
  'image/character/ch174_hero.png',
  'image/character/ch176_hero.png',
  'image/character/ch176b_hero.png',
  'image/character/ch177_hero.png',
  'image/character/ch177b_hero.png',
  'image/character/ch178_hero.png',
  'image/character/ch180_hero.png',
  'image/character/ch183_hero.png',
  'image/character/ch184_hero.png',
  'image/character/ch188_hero.png',
] as const

type StageSprite = {
  src: string
  /** ステージ（1200x630）に対する左端・上端・幅の割合（%） */
  left: number
  top: number
  width: number
  pixelated?: boolean
}

// サムネと同じ配置。敵は左、プレイヤーは右で、画面の外側ほど手前（大きく・下）、玉座に近いほど奥。
// 敵のドット絵（48x48）は整数倍（PC 幅 1200px 時）になる幅にしている。
const stageSprites: StageSprite[] = [
  { src: 'image/battle/Enemy86.png', left: 44, top: 40.6, width: 12, pixelated: true },
  { src: 'image/battle/Enemy100.png', left: 1.3, top: 61.9, width: 20, pixelated: true },
  { src: 'image/battle/Enemy49.png', left: 21.8, top: 64.1, width: 16, pixelated: true },
  { src: 'image/battle/Enemy66.png', left: 38.8, top: 72.4, width: 8, pixelated: true },
  { src: 'image/character/ch175_hero.png', left: 53.3, top: 62.9, width: 12.25 },
  { src: 'image/character/ch109_hero.png', left: 66.7, top: 63.2, width: 12.25 },
  { src: 'image/character/ch173_hero.png', left: 84.2, top: 63.2, width: 15 },
  // 空を飛ぶ敵。垂れ幕の下に置く（外側ほど低く小さく、内側ほど高く大きく。シャンデリアの鎖は避ける）
  { src: 'image/battle/Enemy79.png', left: 1.4, top: 19, width: 8, pixelated: true },
  { src: 'image/battle/Enemy2.png', left: 12.75, top: 15.9, width: 12, pixelated: true },
  { src: 'image/battle/Enemy61.png', left: 75.25, top: 15.9, width: 12, pixelated: true },
  { src: 'image/battle/Enemy14.png', left: 90.6, top: 19, width: 8, pixelated: true },
]

const heroTitleSx = {
  // ステージの幅に合わせて拡縮する（サムネでは幅 1200px に対して約 80px）
  fontSize: 'max(2rem, 6.8cqw)',
  lineHeight: 1.05,
  color: '#fffaf0',
  letterSpacing: '0.04em',
  textShadow: [
    '-3px -3px 0 #1a0f33',
    '3px -3px 0 #1a0f33',
    '-3px 3px 0 #1a0f33',
    '3px 3px 0 #1a0f33',
    '0 6px 0 rgba(10, 6, 24, 0.6)',
  ].join(', '),
} as const

const heroTaglineSx = {
  fontSize: 'max(0.85rem, 2.5cqw)',
  color: '#f2d27a',
  letterSpacing: '0.04em',
  textShadow: ['-2px -2px 0 #1a0f33', '2px -2px 0 #1a0f33', '-2px 2px 0 #1a0f33', '2px 2px 0 #1a0f33'].join(', '),
} as const

// 枠を持たせず、右側（モバイルは下側）のパネル全体をフォームの面にする
const authCardSx = {
  width: '100%',
  maxWidth: { xs: 400, md: 440 },
  border: 'none',
  backgroundColor: 'transparent',
  boxShadow: 'none',
  p: 0,
} as const

const darkInputSx = {
  '& .MuiOutlinedInput-root': {
    backgroundColor: 'rgba(255, 255, 255, 0.05)',
    color: '#f4f0ff',
    '& fieldset': { borderColor: '#5b4b94' },
    '&:hover fieldset': { borderColor: '#8a76c9' },
    '&.Mui-focused fieldset': { borderColor: '#d9a441' },
  },
  '& .MuiInputLabel-root': { color: '#b3a8d6' },
  '& .MuiInputLabel-root.Mui-focused': { color: '#f2d27a' },
} as const

const purpleButtonSx = {
  '&&': {
    backgroundColor: '#7a5bd0',
    backgroundImage: 'none',
    borderColor: '#a58cf0',
    color: '#ffffff',
  },
  '&&:hover': {
    backgroundColor: '#6a4bc0',
    backgroundImage: 'none',
    boxShadow: 'none',
  },
  '&&.Mui-disabled': {
    backgroundColor: '#4a3f73',
    backgroundImage: 'none',
    color: '#b3a8d6',
  },
} as const

const mutedTextColor = '#b3a8d6'
const dividerColor = '#3e3470'

function Auth() {
  const { user, isLoading } = useAuth()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [emailAuthMode, setEmailAuthMode] = useState<EmailAuthMode>('signIn')
  const [submitMode, setSubmitMode] = useState<SubmitMode>(null)
  const [error, setError] = useState<string | null>(null)
  const [mascotSrc] = useState(() =>
    resolvePublicAssetPath(formMascotPaths[Math.floor(Math.random() * formMascotPaths.length)]),
  )
  const [message, setMessage] = useState<string | null>(null)

  if (isLoading) {
    return (
      <Box minHeight="100vh" display="grid" sx={{ placeItems: 'center' }}>
        <Stack direction="row" spacing={1} alignItems="center">
          <CircularProgress size={20} />
          <Typography>{locale.authLoading}</Typography>
        </Stack>
      </Box>
    )
  }

  if (user) {
    return <Navigate to="/" replace />
  }

  const resolveError = (msg: string): string => {
    const normalized = msg.trim().toLowerCase()
    return locale.errorMessages[normalized as keyof typeof locale.errorMessages] ?? locale.defaultError
  }

  const handleGoogleSignIn = async () => {
    setSubmitMode('google')
    setError(null)
    setMessage(null)

    const redirectTo = new URL(import.meta.env.BASE_URL, window.location.origin).toString()
    const { error: oauthError } = await supabase.auth.signInWithOAuth({
      provider: 'google',
      options: { redirectTo },
    })

    if (oauthError) {
      setError(resolveError(oauthError.message))
      setSubmitMode(null)
    }
  }

  const toggleEmailAuthMode = () => {
    setEmailAuthMode((prev) => (prev === 'signIn' ? 'signUp' : 'signIn'))
    setError(null)
    setMessage(null)
  }

  const handleEmailAuth = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError(null)
    setMessage(null)

    if (emailAuthMode === 'signIn') {
      setSubmitMode('signIn')
      const { error: signInError } = await supabase.auth.signInWithPassword({ email, password })

      if (signInError) {
        setError(resolveError(signInError.message))
        setSubmitMode(null)
        return
      }

      setSubmitMode(null)
      return
    }

    if (password.length < 6) {
      setError(locale.errorMessages['password should be at least 6 characters'])
      setSubmitMode(null)
      return
    }

    setSubmitMode('signUp')

    const emailRedirectTo = new URL(import.meta.env.BASE_URL, window.location.origin).toString()
    const { data, error: signUpError } = await supabase.auth.signUp({
      email,
      password,
      options: { emailRedirectTo },
    })

    if (signUpError) {
      setError(resolveError(signUpError.message))
      setSubmitMode(null)
      return
    }

    if (!data.session?.access_token) {
      setMessage(locale.toastNeedsEmailConfirm)
    }

    setSubmitMode(null)
  }

  const handleAnonymousSignIn = async () => {
    setSubmitMode('anonymous')
    setError(null)
    setMessage(null)

    const { error: anonymousError } = await supabase.auth.signInAnonymously()

    if (anonymousError) {
      setError(resolveError(anonymousError.message))
      setSubmitMode(null)
    }
  }

  const isAnySubmitting = submitMode !== null

  return (
    <Box
      sx={{
        minHeight: '100dvh',
        display: 'grid',
        // PC はフォームを固定幅にして、残りをすべてゲームの絵に使う（絵が広いほどキャラも大きくなる）
        gridTemplateColumns: { xs: '1fr', md: 'minmax(0, 1fr) 460px', lg: 'minmax(0, 1fr) 520px' },
        gridTemplateRows: { xs: 'auto 1fr', md: '1fr' },
        backgroundColor: heroCeilingColor,
      }}
    >
      <Box
        sx={{
          overflow: 'hidden',
          // モバイルはステージ（1200x630）より少し縦長にして天井をのぞかせる
          aspectRatio: { xs: '1200 / 760', md: 'auto' },
          // PC はフォームが画面より長くてもキャラが見切れないよう、画面の高さに固定する
          position: { xs: 'relative', md: 'sticky' },
          top: { md: 0 },
          alignSelf: { md: 'start' },
          height: { md: '100dvh' },
          backgroundColor: heroCeilingColor,
          backgroundImage: `url(${heroBackgroundUrl})`,
          backgroundSize: '100% auto',
          backgroundPosition: 'center bottom',
          backgroundRepeat: 'no-repeat',
          imageRendering: 'pixelated',
        }}
      >
        <Box
          sx={{
            position: 'absolute',
            left: 0,
            right: 0,
            bottom: 0,
            aspectRatio: '1200 / 630',
            containerType: 'inline-size',
          }}
        >
          <Stack
            alignItems="center"
            spacing={{ xs: 0.5, md: 1 }}
            sx={{
              position: 'absolute',
              left: 0,
              right: 0,
              top: '15.2%',
              transform: 'translateY(-50%)',
              textAlign: 'center',
            }}
          >
            <Typography component="h1" sx={heroTitleSx}>
              {locale.heroTitle}
            </Typography>
            <Typography sx={heroTaglineSx}>{locale.heroTagline}</Typography>
          </Stack>

          {stageSprites.map((sprite) => (
            <Box
              key={sprite.src}
              component="img"
              src={resolvePublicAssetPath(sprite.src)}
              alt=""
              sx={{
                position: 'absolute',
                left: `${sprite.left}%`,
                top: `${sprite.top}%`,
                width: `${sprite.width}%`,
                height: 'auto',
                imageRendering: sprite.pixelated ? 'pixelated' : 'auto',
                pointerEvents: 'none',
              }}
            />
          ))}
        </Box>
      </Box>

      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          px: { xs: 2, sm: 4 },
          py: { xs: 4, md: 6 },
          background: 'linear-gradient(180deg, #1b1538 0%, #0d0a20 100%)',
        }}
      >
        <Paper elevation={0} sx={authCardSx}>
          <Stack spacing={{ xs: 3, md: 3.5 }}>
            <Stack alignItems="center" spacing={1.25}>
              <Typography
                component="h2"
                textAlign="center"
                sx={{ fontSize: { xs: '1.7rem', md: '2rem' }, color: '#f2d27a', letterSpacing: '0.06em' }}
              >
                {locale.cardTitle}
              </Typography>
              {/* 垂れ幕と同じ金と紫の飾り罫で、見出しとフォーム本体を区切る */}
              <Box
                aria-hidden
                sx={{
                  width: 88,
                  height: 4,
                  borderRadius: 999,
                  background: 'linear-gradient(90deg, #7a5bd0 0%, #d9a441 50%, #7a5bd0 100%)',
                }}
              />
            </Stack>

            {message ? <Alert severity="info">{message}</Alert> : null}

            <Box
              component="img"
              src={mascotSrc}
              alt=""
              sx={{ height: { xs: 104, md: 128 }, alignSelf: 'center', mb: -1, pointerEvents: 'none' }}
            />

            <Button
              type="button"
              variant="contained"
              size="large"
              disabled={isAnySubmitting}
              onClick={handleAnonymousSignIn}
              sx={{ ...softGoldButtonSx, py: { xs: 1.4, md: 1.8 }, fontSize: { md: '1.05rem' } }}
            >
              {submitMode === 'anonymous' ? locale.anonymousSubmitting : locale.anonymousSubmit}
            </Button>

            <Divider sx={{ '&::before, &::after': { borderColor: dividerColor } }}>
              <Typography variant="caption" sx={{ color: mutedTextColor }}>
                {locale.orDivider}
              </Typography>
            </Divider>

            <Button
              variant="contained"
              disabled={isAnySubmitting}
              onClick={handleGoogleSignIn}
              sx={{ ...googleButtonSx, py: { xs: 1, md: 1.3 } }}
              startIcon={<GoogleColorIcon />}
            >
              {submitMode === 'google' ? locale.googleSignInSubmitting : locale.googleSignIn}
            </Button>

            <Divider sx={{ borderColor: dividerColor }} />

            <Box component="form" onSubmit={handleEmailAuth} noValidate>
              <Stack spacing={{ xs: 1.5, md: 2 }}>
                <TextField
                  id="auth-email"
                  label={locale.emailLabel}
                  type="email"
                  autoComplete="email"
                  value={email}
                  onChange={(event) => setEmail(event.target.value)}
                  required
                  fullWidth
                  size="medium"
                  sx={darkInputSx}
                />
                <TextField
                  id="auth-password"
                  label={locale.passwordLabel}
                  type="password"
                  autoComplete={emailAuthMode === 'signIn' ? 'current-password' : 'new-password'}
                  value={password}
                  onChange={(event) => setPassword(event.target.value)}
                  required
                  fullWidth
                  sx={darkInputSx}
                />
                <Button
                  type="submit"
                  variant="contained"
                  disabled={isAnySubmitting}
                  fullWidth
                  sx={{ ...purpleButtonSx, py: { xs: 1, md: 1.3 } }}
                >
                  {submitMode === emailAuthMode
                    ? emailAuthMode === 'signIn'
                      ? locale.signInSubmitting
                      : locale.signUpSubmitting
                    : emailAuthMode === 'signIn'
                      ? locale.signIn
                      : locale.signUp}
                </Button>
                {error ? <Alert severity="error">{error}</Alert> : null}
                <Stack direction="row" spacing={0.75} justifyContent="center" alignItems="center" sx={{ pt: 0.5 }}>
                  <Typography variant="body2" sx={{ color: mutedTextColor }}>
                    {emailAuthMode === 'signIn' ? locale.signUpPrompt : locale.signInPrompt}
                  </Typography>
                  <Link
                    component="button"
                    type="button"
                    variant="body2"
                    onClick={toggleEmailAuthMode}
                    disabled={isAnySubmitting}
                    sx={{ color: '#cdb8ff', fontWeight: 700, textDecorationColor: 'rgba(205, 184, 255, 0.5)' }}
                  >
                    {emailAuthMode === 'signIn' ? locale.signUp : locale.signIn}
                  </Link>
                </Stack>
              </Stack>
            </Box>
          </Stack>
        </Paper>
      </Box>
    </Box>
  )
}

export default Auth
