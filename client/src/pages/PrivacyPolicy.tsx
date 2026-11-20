import { Box, Container, Link as MuiLink, Paper, Stack, Typography } from '@mui/material'
import HomeNavIconButton from '@/components/common/HomeNavIconButton'
import { outerPagePaperSx } from '@/constants/styles'
import locale from '../../locale/privacy/PrivacyPolicy.json'

type PolicySection = {
  heading: string
  items: string[]
  links?: { label: string; url: string }[]
}

const sections: PolicySection[] = locale.sections

// ログインしていなくても見られるページ（App.tsx でログインを必須にしていない）。
export default function PrivacyPolicy() {
  return (
    <Container maxWidth="md" sx={{ py: { xs: 2, sm: 8 } }}>
      <Paper elevation={2} sx={outerPagePaperSx}>
        <Stack spacing={{ xs: 1.5, sm: 2.5 }}>
          <Stack direction="row" justifyContent="flex-start">
            <HomeNavIconButton ariaLabel={locale.backToHome} />
          </Stack>

          <Paper
            variant="outlined"
            sx={{ borderRadius: 3, p: { xs: 2, sm: 3 }, borderColor: '#dcc793', backgroundColor: '#fff9ef' }}
          >
            <Stack spacing={3}>
              <Stack spacing={1}>
                <Typography variant="overline" sx={{ letterSpacing: '0.18em' }} color="text.secondary">
                  {locale.overline}
                </Typography>
                <Typography component="h1" variant="h4" fontWeight={900} lineHeight={1.1}>
                  {locale.title}
                </Typography>
                <Typography>{locale.intro}</Typography>
              </Stack>

              {sections.map((section) => (
                <Stack key={section.heading} spacing={1}>
                  <Typography component="h2" variant="h6" fontWeight={900}>
                    {section.heading}
                  </Typography>
                  <Box component="ul" sx={{ m: 0, pl: 3 }}>
                    {section.items.map((item) => (
                      <Typography key={item} component="li" variant="body2" sx={{ mb: 0.5 }}>
                        {item}
                      </Typography>
                    ))}
                  </Box>
                  {section.links ? (
                    <Stack spacing={0.5} sx={{ pl: 3 }}>
                      {section.links.map((link) => (
                        <MuiLink
                          key={link.url}
                          href={link.url}
                          target="_blank"
                          rel="noopener noreferrer"
                          underline="hover"
                          variant="body2"
                        >
                          {link.label}
                        </MuiLink>
                      ))}
                    </Stack>
                  ) : null}
                </Stack>
              ))}

              <Typography variant="body2" color="text.secondary" textAlign="right">
                {locale.establishedAt}
              </Typography>
            </Stack>
          </Paper>
        </Stack>
      </Paper>
    </Container>
  )
}
