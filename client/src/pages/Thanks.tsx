import { Box, Container, Link as MuiLink, Paper, Stack, Typography } from '@mui/material'
import HomeNavIconButton from '@/components/common/HomeNavIconButton'
import { outerPagePaperSx } from '@/constants/styles'
import locale from '../../locale/thanks/Thanks.json'

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
] as const

export default function Thanks() {
  return (
    <Container maxWidth="lg" sx={{ py: { xs: 2, sm: 8 } }}>
      <Paper elevation={2} sx={outerPagePaperSx}>
        <Stack spacing={{ xs: 1.5, sm: 2.5 }}>
          <Stack direction="row" justifyContent="flex-start">
            <HomeNavIconButton ariaLabel={locale.backToHome} />
          </Stack>

          <Paper
            variant="outlined"
            sx={{
              borderRadius: 3,
              p: { xs: 2, sm: 2.5 },
              borderColor: '#b8ab7a',
              backgroundColor: '#44644a',
            }}
          >
            <Stack spacing={2}>
              <Stack spacing={1.25}>
                <Typography variant="overline" sx={{ letterSpacing: '0.18em', color: 'rgba(242, 235, 207, 0.8)' }}>
                  CREDITS
                </Typography>
                <Typography variant="h4" fontWeight={900} color="#fff8ea" lineHeight={1.1}>
                  スペシャルサンクス
                </Typography>
                <Typography color="rgba(242, 235, 207, 0.92)">
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
                      borderColor: '#dcc793',
                      backgroundColor: '#fff9ef',
                    }}
                  >
                    <Stack spacing={1.25}>
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
                          py: 1.25,
                          borderColor: '#e7d9b6',
                          backgroundColor: '#fffdf8',
                        }}
                      >
                        <Stack spacing={0.5}>
                          <Typography variant="caption" color="text.secondary">
                            URL
                          </Typography>
                          <MuiLink href={entry.url} target="_blank" rel="noopener noreferrer" underline="hover">
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
      </Paper>
    </Container>
  )
}
