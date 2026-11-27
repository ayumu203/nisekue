import { font, palette, withAlpha } from '@/theme/tokens'
export const menuButtonSx = {
  width: '100%',
  minHeight: 48,
  whiteSpace: 'nowrap',
  justifyContent: 'center',
} as const

export const softGreenButtonSx = {
  '&&': {
    backgroundColor: palette.green400,
    backgroundImage: 'none',
    borderColor: palette.white,
    color: palette.white,
  },
  '&&.Mui-disabled': {
    backgroundColor: palette.gray300,
    backgroundImage: 'none',
    borderColor: palette.white,
    color: palette.white,
  },
} as const

export const mutedGreenButtonSx = {
  '&&': {
    backgroundColor: palette.cream,
    backgroundImage: 'none',
    borderColor: palette.green400,
    color: palette.green700,
  },
  '&&.Mui-disabled': {
    backgroundColor: palette.white,
    backgroundImage: 'none',
    borderColor: palette.gray300,
    color: palette.teal400,
  },
} as const

export const softGoldButtonSx = {
  '&&': {
    backgroundColor: palette.gold300,
    backgroundImage: 'none',
    border: `2px solid ${palette.brown500}`,
    borderColor: palette.brown500,
    color: palette.brown900,
    boxShadow: 'none',
  },
  '&&.Mui-disabled': {
    backgroundColor: palette.brown200,
    backgroundImage: 'none',
    border: `2px solid ${palette.brown500}`,
    borderColor: palette.brown500,
    color: palette.brown500,
    boxShadow: 'none',
  },
} as const

export const googleButtonSx = {
  '&&': {
    backgroundColor: palette.white,
    backgroundImage: 'none',
    borderColor: palette.gray300,
    color: palette.brown900,
    boxShadow: 'none',
  },
  '&&.Mui-disabled': {
    backgroundColor: palette.sky,
    backgroundImage: 'none',
    borderColor: palette.gray300,
    color: palette.gray300,
    boxShadow: 'none',
  },
} as const

export const mutedRedButtonSx = {
  '&&': {
    backgroundColor: palette.white,
    backgroundImage: 'none',
    borderColor: palette.red300,
    color: palette.red700,
  },
  '&&.Mui-disabled': {
    backgroundColor: palette.white,
    backgroundImage: 'none',
    borderColor: palette.brown200,
    color: palette.red300,
  },
} as const

export const greenBadgeSx = {
  px: 1.5,
  py: 0.75,
  border: '1px solid',
  borderColor: palette.white,
  borderRadius: 999,
  backgroundColor: palette.green400,
} as const

export const greenBadgeTextSx = {
  color: palette.white,
  fontWeight: 700,
} as const

export const greenOutlinedInputSx = {
  '& .MuiOutlinedInput-root': {
    backgroundColor: palette.white,
    '--input-outline-color': palette.brown200,
    '& fieldset': {
      borderColor: palette.brown200,
    },
    '&.Mui-focused fieldset': {
      borderColor: palette.brown200,
    },
  },
  '& .MuiInputLabel-root.Mui-focused': {
    color: palette.brown900,
  },
} as const

export const playerHpBarSx = {
  height: 5,
  borderRadius: 999,
  backgroundColor: palette.gray300,
  '& .MuiLinearProgress-bar': {
    backgroundColor: palette.green400,
  },
} as const

export const playerMpBarSx = {
  height: 5,
  borderRadius: 999,
  backgroundColor: palette.sky,
  '& .MuiLinearProgress-bar': {
    backgroundColor: palette.blue300,
  },
} as const

export const innerSurfaceSx = {
  backgroundColor: palette.cream,
  borderColor: palette.brown200,
} as const

// 画面いっぱいに広げた外枠（GamePageFrame）の背景と、ヘッダーメニューで今いる画面を塗る色
export const gamePageBackground = palette.yellow
export const gamePageAccent = palette.gold700

export const outerPagePaperSx = {
  backgroundColor: palette.yellow,
  border: '1px solid',
  borderColor: palette.gold300,
  borderRadius: { xs: 3, sm: 4 },
  p: '2%',
} as const

export const twoColumnContentGridSx = {
  display: 'grid',
  gridTemplateColumns: {
    xs: '1fr',
    sm: 'minmax(180px, 260px) minmax(0, 1fr)',
    md: 'minmax(280px, 360px) minmax(0, 1fr)',
  },
  gap: { xs: 1.5, sm: 3 },
  alignItems: 'start',
} as const

export const topNavigationIconButtonSx = {
  width: 44,
  height: 44,
  border: `2px solid ${palette.white}`,
  color: palette.white,
  '&&': { backgroundColor: withAlpha(palette.brown500, 0.9) },
} as const

export const gameTabsSx = {
  minHeight: 0,
  '& .MuiTabs-flexContainer': {
    gap: 0.5,
  },
} as const

export const gameTabSx = {
  minHeight: 40,
  minWidth: 0,
  py: 0.5,
  px: { xs: 0.5, sm: 1 },
  fontSize: { xs: font.xs, sm: font.sm },
  borderRadius: '10px 10px 0 0',
  border: `2px solid ${palette.green400}`,
  borderBottom: 'none',
  backgroundColor: palette.green400,
  color: palette.white,
  fontWeight: 700,
  textTransform: 'none',
  transition: 'none',
  '& .MuiTouchRipple-root': {
    display: 'none',
  },
  '&.Mui-selected': {
    backgroundColor: palette.green400,
    borderColor: palette.green400,
    color: palette.white,
  },
} as const

// PC のホームの色（上部のオレンジの帯・地のクリーム・白い箱）
export const homePanelColors = {
  topBand: palette.yellow,
  ground: palette.cream,
  band: palette.green400,
  // 帯を「作成｜募集中」のように分けたとき、選んでいる側の色
  bandSelected: palette.green700,
  divider: palette.brown200,
  label: palette.brown500,
  text: palette.brown900,
} as const

// クエスト画面の色（上部の青緑の帯・地の薄い青緑）。見出しの帯やボタンの緑はホームと同じ
export const questPanelColors = {
  topBand: palette.teal700,
  ground: palette.sky,
} as const

// 訓練場の色（道場：上部の木の茶色の帯・生成りの地）。見出しの帯とボタンは緑ではなく藍
export const trainingPanelColors = {
  topBand: palette.brown900,
  ground: palette.cream,
  band: palette.blue700,
  // 帯を「NPC｜PLAYER」のように分けたとき、選んでいる側の色
  bandSelected: palette.blue900,
  // 選んでいる相手カードの地
  selectedSurface: palette.sky,
  // 押せないボタンの色
  disabledButton: palette.blue300,
  // 白い箱の中の文字・区切り線（茶色やベージュを使わず、藍に合わせたグレー）
  text: palette.blue900,
  label: palette.teal700,
  divider: palette.sky,
} as const

// 宝の地図の色（白・金・青：上部の金の帯・白に近い地）。見出しの帯とボタンは緑ではなく青
export const treasureMapPanelColors = {
  topBand: palette.gold700,
  ground: palette.white,
  band: palette.blue700,
  // 探索中の地図の「探索中」の地と文字
  soft: palette.sky,
  softText: palette.blue900,
  // 押せないボタンの色
  disabledButton: palette.gray300,
  disabledButtonText: palette.gray600,
  disabledButtonBorder: palette.brown200,
  // 白い箱の中の文字・区切り線
  text: palette.brown900,
  label: palette.brown900,
  divider: palette.gray300,
  // 星の色
  star: palette.gold700,
  starOff: palette.gray300,
  // 探索完了のメッセージウィンドウ
  messageWindow: withAlpha(palette.navy, 0.9),
  messageHighlight: palette.gold300,
  messageLabel: palette.blue300,
} as const

// アイテム画面の色（道具袋：えんじの革・生成りの布・真鍮）。ほかの画面の白い箱ではなく、袋と札で組む
export const itemsPanelColors = {
  topBand: palette.brown900,
  ground: palette.cream,
  // 袋の革（枠・タブ・主な操作）と、その影
  leather: palette.brown900,
  leatherDark: palette.brown900,
  // 選んでいないタブ
  tabOff: palette.red300,
  // 袋の布地・縫い目・マス
  canvas: palette.cream,
  stitch: palette.brown200,
  slot: palette.cream,
  slotLine: palette.brown200,
  // 選んでいるマスの縁
  brass: palette.gold700,
  brassSoft: palette.brown200,
  text: palette.brown900,
  label: palette.brown900,
  danger: palette.red300,
  dangerText: palette.red300,
  up: palette.green700,
  down: palette.red700,
  // 値札
  priceTag: palette.gold300,
  priceTagText: palette.brown900,
  // 合成タブの鍛冶場
  forge: palette.black,
  forgeLine: palette.brown900,
  forgeSlot: palette.brown900,
  forgeSlotLine: palette.brown900,
  forgeEmber: palette.red700,
  forgeEmberText: palette.gold300,
  forgeButton: palette.red700,
  forgeText: palette.cream,
  forgeLabel: palette.brown200,
  forgeMuted: palette.gray600,
} as const

// 白い箱の中の主な操作（募集開始・START・決定など）。見出しの帯と同じ緑
export const panelPrimaryButtonSx = {
  '&&': {
    backgroundColor: homePanelColors.band,
    backgroundImage: 'none',
    // 枠付きのボタンと高さ・幅をそろえるため、同じ太さの枠を同じ色で付ける
    border: `2px solid ${homePanelColors.band}`,
    color: palette.white,
    fontWeight: 800,
    boxShadow: 'none',
    transition: 'none',
  },
  '& .MuiTouchRipple-root': {
    display: 'none',
  },
  // ホバーしても色を変えない
  '&&.Mui-disabled': {
    backgroundColor: palette.teal400,
    color: palette.white,
    boxShadow: 'none',
  },
} as const

// 白い箱の中の、主な操作の横に置く操作（参加制限・この配置で決定など）
export const panelOutlinedButtonSx = {
  '&&': {
    backgroundColor: palette.white,
    color: homePanelColors.band,
    border: `2px solid ${homePanelColors.band}`,
    fontWeight: 800,
    transition: 'none',
  },
  '& .MuiTouchRipple-root': {
    display: 'none',
  },
  // ホバーしても色を変えない
  '&&.Mui-disabled': {
    color: palette.teal400,
    borderColor: palette.gray300,
  },
} as const

// 白い箱の中の取り消し操作（CANCEL）。ホバーや押したときに見た目を変えない
export const panelCancelButtonSx = {
  '&&': {
    backgroundColor: palette.white,
    backgroundImage: 'none',
    border: `2px solid ${palette.red300}`,
    color: palette.red700,
    fontWeight: 800,
    boxShadow: 'none',
    transition: 'none',
  },
  '& .MuiTouchRipple-root': {
    display: 'none',
  },
  '&&.Mui-disabled': {
    backgroundColor: palette.white,
    borderColor: palette.brown200,
    color: palette.red300,
  },
} as const

// スキル画面（魔導書）。表紙はこげ茶の革、ページは焦げ茶に生成りの文字
export const skillPanelColors = {
  topBand: palette.brown900,
  ground: palette.cream,
  // 表紙の革と、その縁
  cover: palette.brown900,
  coverDark: palette.brown900,
  gold: palette.gold300,
  // ページ・選んでいる行・行の区切り
  paper: palette.brown900,
  rowSelected: palette.brown900,
  line: palette.brown900,
  ink: palette.cream,
  sub: palette.brown200,
  grip: palette.brown500,
  // 消費MP の結晶と数字
  gem: palette.blue300,
  mpText: palette.sky,
  // 1 番目の技の「消えてしまいます」の札
  warning: palette.red700,
} as const

// 転職画面（聖堂の藍）。上部の帯・地・白い箱の見出しの帯・舞台の上の名札
export const jobChangePanelColors = {
  topBand: palette.blue900,
  ground: palette.cream,
  band: palette.blue700,
  bandDark: palette.blue900,
  soft: palette.sky,
  gold: palette.gold700,
  goldText: palette.gold700,
  goldSoft: palette.cream,
  // 舞台の上の名札と札の金
  plate: withAlpha(palette.blue900, 0.9),
  plateGold: palette.gold300,
  text: palette.blue900,
  label: palette.teal700,
  divider: palette.sky,
  // 未解放の札
  locked: palette.cream,
  lockedBorder: palette.gray300,
  lockedText: palette.gray600,
  canUnlock: palette.cream,
} as const
