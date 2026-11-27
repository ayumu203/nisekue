import { Box, Link as MuiLink, Paper, Stack, Typography, useMediaQuery, type Theme } from '@mui/material'
import GamePageFrame from '@/components/common/GamePageFrame'
import HomeNavIconButton from '@/components/common/HomeNavIconButton'
import { gamePageBackground } from '@/constants/styles'
import { useAuth } from '@/contexts/useAuth'
import locale from '../../locale/thanks/Thanks.json'
import { palette, withAlpha } from '@/theme/tokens'

const creditEntries = [
  {
    name: 'モンスター & マテリアルズ II様',
    category: 'モンスター素材',
    url: 'https://darts-x.sakura.ne.jp/m/',
  },
  {
    name: 'Mokemo様',
    category: 'モンスター素材',
    url: 'https://mokemo-factory.booth.pm',
  },
  {
    name: 'MNO様',
    category: 'キャラクター素材',
    url: 'https://www.tumblr.com/sanoya0u0',
  },
  {
    name: '三日月アルペジオ様',
    category: 'キャラクター素材',
    url: 'https://roughsketch.en-grey.com/',
  },
  {
    name: 'らぬきの立ち絵保管庫',
    category: 'キャラクター素材',
    url: 'https://ranuking.ko-me.com/',
  },
  {
    name: 'DOT ILLUST',
    category: 'モンスター素材',
    url: 'https://dot-illust.net/',
  },
  {
    name: '【Rド】様',
    category: 'モンスター素材',
    url: 'http://rpgdot3319.g1.xrea.com/',
  },
] as const

export default function Thanks() {
  const { session } = useAuth()
  const isDesktop = useMediaQuery((theme: Theme) => theme.breakpoints.up('sm'), { noSsr: true })
  // ログインしていないときも開ける画面なので、ヘッダーメニューが出ないときは今までどおりホームボタンを出す
  const showHeader = isDesktop && session != null

  return (
    <GamePageFrame currentKey="specialThanks" currentColor={palette.green700} background={gamePageBackground}>
      <Stack spacing={{ xs: 1.5, sm: 2.5 }}>
        {showHeader ? null : (
          <Stack direction="row" justifyContent="flex-start">
            <HomeNavIconButton ariaLabel={locale.backToHome} />
          </Stack>
        )}

        <Paper
          variant="outlined"
          sx={{
            borderRadius: 3,
            p: { xs: 2, sm: 2.5 },
            borderColor: palette.brown200,
            backgroundColor: palette.green700,
          }}
        >
          <Stack spacing={2}>
            <Stack spacing={1.5}>
              <Typography variant="overline" sx={{ letterSpacing: '0.18em', color: withAlpha(palette.cream, 0.75) }}>
                CREDITS
              </Typography>
              <Typography variant="h4" fontWeight={900} color={palette.cream} lineHeight={1.1}>
                スペシャルサンクス
              </Typography>
              <Typography color={withAlpha(palette.cream, 0.9)}>
                画像素材の権利関係はすべて製作者様へ帰属します。製作者様の許可なく再配布・改変は行わないでください。
              </Typography>
            </Stack>

            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: { xs: '1fr', md: 'repeat(2, minmax(0, 1fr))' },
                gap: 1.5,
              }}
            >
              {creditEntries.map((entry) => (
                <Paper
                  key={entry.url}
                  variant="outlined"
                  sx={{
                    borderRadius: 3,
                    p: { xs: 2, sm: 2.5 },
                    borderColor: palette.brown200,
                    backgroundColor: palette.cream,
                  }}
                >
                  <Stack spacing={1.5}>
                    <Stack direction="row" justifyContent="space-between" alignItems="flex-start" spacing={1}>
                      <Stack spacing={0.5}>
                        <Typography variant="h6" fontWeight={900} lineHeight={1.2}>
                          {entry.name}
                        </Typography>
                        <Typography variant="body2" color="text.secondary">
                          {entry.category}
                        </Typography>
                      </Stack>
                    </Stack>

                    <Paper
                      variant="outlined"
                      sx={{
                        borderRadius: 2,
                        px: 1.5,
                        py: 1.5,
                        borderColor: palette.brown200,
                        backgroundColor: palette.white,
                      }}
                    >
                      <Stack spacing={0.5}>
                        <Typography variant="caption" color="text.secondary">
                          URL
                        </Typography>
                        <MuiLink href={entry.url} target="_blank" rel="noopener noreferrer" underline="always">
                          {entry.url}
                        </MuiLink>
                      </Stack>
                    </Paper>
                  </Stack>
                </Paper>
              ))}
            </Box>
          </Stack>
        </Paper>
      </Stack>
    </GamePageFrame>
  )
}
