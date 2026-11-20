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
  /** 宙に浮いているキャラ（足元の床に影を出す） */
  floating?: boolean
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
  // 後衛の魔法使いは魔法陣を出しながら宙に浮いている（地上のキャラより少し上に置く）
  { src: 'image/character/ch173_hero.png', left: 84.2, top: 56, width: 15, floating: true },
  // 空を飛ぶ敵。左右のアーチ窓の前に 1 体ずつ置き、高さを左右のシャンデリアにそろえる。
  // 魔王より手前・地上のキャラより奥にいるイメージなので、地上の敵より小さい 2 倍にしている
  { src: 'image/battle/Enemy2.png', left: 24.75, top: 18.4, width: 8, pixelated: true },
  { src: 'image/battle/Enemy62.png', left: 67.25, top: 18.4, width: 8, pixelated: true },
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
  maxWidth: { xs: 400, md: 460 },
  border: 'none',
  backgroundColor: 'transparent',
  boxShadow: 'none',
  p: 0,
} as const

const autofillOverrideSx = {
  WebkitBoxShadow: '0 0 0 100px #231c45 inset',
  boxShadow: '0 0 0 100px #231c45 inset',
  WebkitTextFillColor: '#f4f0ff',
  caretColor: '#f4f0ff',
} as const

const darkInputSx = {
  '& .MuiOutlinedInput-root': {
    backgroundColor: 'rgba(255, 255, 255, 0.05)',
    color: '#f4f0ff',
    // 紫の枠は背景から浮いて見えるため、白を薄く乗せた控えめな枠にする
    '& fieldset': { borderColor: 'rgba(255, 255, 255, 0.18)' },
    '&:hover fieldset': { borderColor: 'rgba(255, 255, 255, 0.35)' },
    '&.Mui-focused fieldset': { borderColor: '#d9a441' },
    // ブラウザの自動入力で入力欄が黄色くなり、暗い背景から浮くのを防ぐ。
    // 未対応の疑似クラスがあるとルールごと無視されるため、:-webkit-autofill と :autofill は別々に書く
    '& input:-webkit-autofill': autofillOverrideSx,
    '& input:autofill': autofillOverrideSx,
  },
} as const

// ラベルは枠の上に重ねず、入力欄の上に置く（自動入力で欄の色が変わってもラベルとかぶらない）
const fieldLabelSx = {
  display: 'block',
  mb: 0.75,
  fontSize: '0.9rem',
  color: '#e6dfff',
} as const

const purpleButtonSx = {
  '&&': {
    backgroundColor: '#4f2fa8',
    backgroundImage: 'none',
    color: '#ffffff',
  },
  '&&:hover': {
    backgroundColor: '#41248f',
    backgroundImage: 'none',
    boxShadow: 'none',
  },
  '&&.Mui-disabled': {
    backgroundColor: '#3a2f63',
    backgroundImage: 'none',
    color: '#b3a8d6',
  },
} as const

// テーマの contained ボタンには 2px の縁があるが、このページの暗い背景では縁が浮いて見えるため外す。
// 角は入力欄とそろえて四角くする（テーマの既定は丸）
const noBorderButtonSx = {
  '&&&': { border: 'none', borderRadius: '8px' },
} as const

const dividerColor = 'rgba(205, 184, 255, 0.4)'

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
            // 画面の上辺と玉座の上端（ステージの上から 192/630）の中間。
            // 玉座の上端はヒーローの下端から 438/1200 × 横幅 の位置にある
            top: 'calc((100% - 36.5cqw) / 2)',
            zIndex: 1,
            transform: 'translateY(-50%)',
            textAlign: 'center',
          }}
        >
          <Typography component="h1" sx={heroTitleSx}>
            {locale.heroTitle}
          </Typography>
          <Typography sx={heroTaglineSx}>{locale.heroTagline}</Typography>
        </Stack>
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
          {stageSprites
            .filter((sprite) => sprite.floating)
            .map((sprite) => (
              <Box
                key={`${sprite.src}-shadow`}
                aria-hidden
                sx={{
                  position: 'absolute',
                  // 地上のキャラと同じ高さ（ステージの下端付近）の床に影を落とす
                  left: `${sprite.left + sprite.width * 0.25}%`,
                  top: '95.5%',
                  width: `${sprite.width * 0.5}%`,
                  height: '3%',
                  borderRadius: '50%',
                  background: 'radial-gradient(ellipse at center, rgba(8, 6, 20, 0.9) 0%, rgba(8, 6, 20, 0) 70%)',
                  opacity: 0.5,
                  pointerEvents: 'none',
                }}
              />
            ))}
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
          {/* PC は画面の高さに合わせて余白・マスコット・ボタンを大きくし、パネル全体を使う */}
          <Stack sx={{ gap: { xs: 3, md: 'clamp(20px, 3.6vh, 48px)' } }}>
            <Stack alignItems="center" spacing={1.25}>
              <Typography
                component="h2"
                textAlign="center"
                sx={{
                  fontSize: { xs: '1.7rem', md: 'clamp(2rem, 3.8vh, 2.8rem)' },
                  color: '#f2d27a',
                  letterSpacing: '0.06em',
                }}
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
              sx={{
                height: { xs: 104, md: 'clamp(128px, 19vh, 240px)' },
                alignSelf: 'center',
                mb: -1,
                pointerEvents: 'none',
              }}
            />

            <Button
              type="button"
              variant="contained"
              size="large"
              disabled={isAnySubmitting}
              onClick={handleAnonymousSignIn}
              sx={{
                ...softGoldButtonSx,
                ...noBorderButtonSx,
                py: { xs: 1.4, md: 'clamp(14px, 2.2vh, 24px)' },
                fontSize: { xs: '1.2rem', md: 'clamp(1.35rem, 2.6vh, 1.6rem)' },
              }}
            >
              {submitMode === 'anonymous' ? locale.anonymousSubmitting : locale.anonymousSubmit}
            </Button>

            <Divider sx={{ borderColor: dividerColor }} />

            <Button
              variant="contained"
              disabled={isAnySubmitting}
              onClick={handleGoogleSignIn}
              sx={{
                ...googleButtonSx,
                ...noBorderButtonSx,
                py: { xs: 1, md: 'clamp(10px, 1.8vh, 20px)' },
                fontSize: { xs: '1.1rem', md: 'clamp(1.25rem, 2.3vh, 1.45rem)' },
                // MUI はアイコンを 20px 固定にするため、文字の大きさに合わせて拡縮させる
                '& .MuiButton-startIcon > *:nth-of-type(1)': { fontSize: '1.2em' },
                '& .MuiButton-startIcon': { mr: '0.5em' },
              }}
              startIcon={<GoogleColorIcon />}
            >
              {submitMode === 'google' ? locale.googleSignInSubmitting : locale.googleSignIn}
            </Button>

            <Divider sx={{ borderColor: dividerColor }} />

            <Box component="form" onSubmit={handleEmailAuth} noValidate>
              <Stack sx={{ gap: { xs: 1.5, md: 'clamp(12px, 2vh, 24px)' } }}>
                <Box>
                  <Typography component="label" htmlFor="auth-email" sx={fieldLabelSx}>
                    {locale.emailLabel}
                  </Typography>
                  <TextField
                    id="auth-email"
                    type="email"
                    autoComplete="email"
                    value={email}
                    onChange={(event) => setEmail(event.target.value)}
                    required
                    fullWidth
                    size="medium"
                    sx={darkInputSx}
                  />
                </Box>
                <Box>
                  <Typography component="label" htmlFor="auth-password" sx={fieldLabelSx}>
                    {locale.passwordLabel}
                  </Typography>
                  <TextField
                    id="auth-password"
                    type="password"
                    autoComplete={emailAuthMode === 'signIn' ? 'current-password' : 'new-password'}
                    value={password}
                    onChange={(event) => setPassword(event.target.value)}
                    required
                    fullWidth
                    sx={darkInputSx}
                  />
                </Box>
                <Button
                  type="submit"
                  variant="contained"
                  disabled={isAnySubmitting}
                  fullWidth
                  sx={{
                    ...purpleButtonSx,
                    ...noBorderButtonSx,
                    py: { xs: 1, md: 'clamp(10px, 1.8vh, 20px)' },
                    fontSize: { xs: '1.1rem', md: 'clamp(1.25rem, 2.3vh, 1.45rem)' },
                  }}
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
                  <Typography variant="body2" sx={{ color: '#ffffff' }}>
                    {emailAuthMode === 'signIn' ? locale.signUpPrompt : locale.signInPrompt}
                  </Typography>
                  <Link
                    component="button"
                    type="button"
                    variant="body2"
                    onClick={toggleEmailAuthMode}
                    disabled={isAnySubmitting}
                    sx={{ color: '#ffffff', fontWeight: 700, textDecorationColor: 'rgba(255, 255, 255, 0.6)' }}
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
