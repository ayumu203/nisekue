import locale from '../../locale/beginner-guide/BeginnerGuide.json'

export type BeginnerGuideSection = {
  title: string
  body: string
}

export type BeginnerGuideDefinition = {
  pageKey: string
  title: string
  description: string
  sections: BeginnerGuideSection[]
}

export const beginnerGuideBadge = locale.badge
export const beginnerGuideOpenLabel = locale.openGuide
export const beginnerGuideCloseLabel = locale.closeGuide
export const beginnerGuideFooterHint = locale.footerHint

export const beginnerGuides = locale.pages as Record<string, BeginnerGuideDefinition>
