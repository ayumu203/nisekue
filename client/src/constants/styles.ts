export const menuButtonSx = {
  width: '100%',
  minHeight: 48,
  whiteSpace: 'nowrap',
  justifyContent: 'center',
} as const

export const softGreenButtonSx = {
  '&&': {
    backgroundColor: '#78c27d',
    backgroundImage: 'none',
    borderColor: '#eef8ef',
    color: '#ffffff',
  },
  '&&:hover': {
    backgroundColor: '#69b46f',
    backgroundImage: 'none',
    borderColor: '#e4f3e6',
    boxShadow: 'none',
  },
  '&&.Mui-disabled': {
    backgroundColor: '#b7d9b9',
    backgroundImage: 'none',
    borderColor: '#eef8ef',
    color: '#f8fff8',
  },
} as const

export const mutedGreenButtonSx = {
  '&&': {
    backgroundColor: '#e4f3e6',
    backgroundImage: 'none',
    borderColor: '#8dbe90',
    color: '#2f5b33',
  },
  '&&:hover': {
    backgroundColor: '#d5ebd7',
    backgroundImage: 'none',
    borderColor: '#78b27b',
    boxShadow: 'none',
  },
  '&&.Mui-disabled': {
    backgroundColor: '#eef6ef',
    backgroundImage: 'none',
    borderColor: '#c6ddc8',
    color: '#8aa48c',
  },
} as const

export const softGoldButtonSx = {
  '&&': {
    backgroundColor: '#f2d27a',
    backgroundImage: 'none',
    border: '2px solid #8f6b2f',
    borderColor: '#8f6b2f',
    color: '#6a4b35',
    boxShadow: 'none',
  },
  '&&:hover': {
    backgroundColor: '#e4c062',
    backgroundImage: 'none',
    borderColor: '#7f5d26',
    boxShadow: 'none',
  },
  '&&.Mui-disabled': {
    backgroundColor: '#ead9a7',
    backgroundImage: 'none',
    border: '2px solid #b59a69',
    borderColor: '#b59a69',
    color: '#927b56',
    boxShadow: 'none',
  },
} as const

export const googleButtonSx = {
  '&&': {
    backgroundColor: '#ffffff',
    backgroundImage: 'none',
    borderColor: '#dadce0',
    color: '#3c4043',
    boxShadow: 'none',
  },
  '&&:hover': {
    backgroundColor: '#f8f9fa',
    backgroundImage: 'none',
    borderColor: '#c0c3c7',
    boxShadow: 'none',
  },
  '&&.Mui-disabled': {
    backgroundColor: '#f1f3f4',
    backgroundImage: 'none',
    borderColor: '#e0e0e0',
    color: '#a8aab0',
    boxShadow: 'none',
  },
} as const

export const mutedRedButtonSx = {
  '&&': {
    backgroundColor: '#fff1ef',
    backgroundImage: 'none',
    borderColor: '#d77a70',
    color: '#9f2f24',
  },
  '&&:hover': {
    backgroundColor: '#ffe4e0',
    backgroundImage: 'none',
    borderColor: '#c96256',
    boxShadow: 'none',
  },
  '&&.Mui-disabled': {
    backgroundColor: '#f8ecea',
    backgroundImage: 'none',
    borderColor: '#dfb2ac',
    color: '#bf8b84',
  },
} as const

export const greenBadgeSx = {
  px: 1.5,
  py: 0.75,
  border: '1px solid',
  borderColor: '#eef8ef',
  borderRadius: 999,
  backgroundColor: '#78c27d',
} as const

export const greenBadgeTextSx = {
  color: '#ffffff',
  fontWeight: 700,
} as const

export const greenOutlinedInputSx = {
  '& .MuiOutlinedInput-root': {
    backgroundColor: '#fffdf8',
    '& fieldset': {
      borderColor: '#d9cfb8',
    },
    '&:hover fieldset': {
      borderColor: '#d1c4aa',
    },
    '&.Mui-focused fieldset': {
      borderColor: '#c8b894',
    },
  },
  '& .MuiInputLabel-root.Mui-focused': {
    color: '#4f4638',
  },
} as const

export const playerHpBarSx = {
  height: 5,
  borderRadius: 999,
  backgroundColor: '#dcefdc',
  '& .MuiLinearProgress-bar': {
    backgroundColor: '#78c27d',
  },
} as const

export const playerMpBarSx = {
  height: 5,
  borderRadius: 999,
  backgroundColor: '#d9ebf6',
  '& .MuiLinearProgress-bar': {
    backgroundColor: '#4f9ed8',
  },
} as const

export const innerSurfaceSx = {
  backgroundColor: '#fff7e8',
  borderColor: '#e7d9b6',
} as const

export const outerPagePaperSx = {
  backgroundColor: '#ffcc00',
  border: '1px solid',
  borderColor: '#d3a93a',
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
  border: '2px solid #ffffff',
  color: '#ffffff',
  backgroundColor: 'rgba(122, 77, 25, 0.9)',
  '&:hover': {
    backgroundColor: 'rgba(110, 68, 21, 0.94)',
  },
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
  fontSize: { xs: '0.78rem', sm: '0.875rem' },
  borderRadius: '10px 10px 0 0',
  border: '2px solid #8dbe90',
  borderBottom: 'none',
  backgroundColor: '#78c27d',
  color: '#ffffff',
  fontWeight: 700,
  textTransform: 'none',
  transition: 'none',
  '& .MuiTouchRipple-root': {
    display: 'none',
  },
  '&.Mui-selected': {
    backgroundColor: '#2f8b3f',
    borderColor: '#2f8b3f',
    color: '#ffffff',
  },
} as const

// PC のホームの色（上部のオレンジの帯・地のクリーム・白い箱）
export const homePanelColors = {
  topBand: '#ffcc00',
  ground: '#fdf3d8',
  band: '#2f7d3a',
  // 帯を「作成｜募集中」のように分けたとき、選んでいる側の色
  bandSelected: '#1d5626',
  divider: '#e3d6b8',
  label: '#6b5a3e',
  text: '#3a2f1e',
} as const

// クエスト画面の色（上部の青緑の帯・地の薄い青緑）。見出しの帯やボタンの緑はホームと同じ
export const questPanelColors = {
  topBand: '#2a7f84',
  ground: '#eaf4f2',
} as const

// 訓練場の色（道場：上部の木の茶色の帯・生成りの地）。見出しの帯とボタンは緑ではなく藍
export const trainingPanelColors = {
  topBand: '#6b4423',
  ground: '#f4efe4',
  band: '#2f4a7a',
  // 帯を「NPC｜PLAYER」のように分けたとき、選んでいる側の色
  bandSelected: '#1d3157',
  // 選んでいる相手カードの地
  selectedSurface: '#e9eef7',
  // 押せないボタンの色
  disabledButton: '#a3b1c9',
  // 白い箱の中の文字・区切り線（茶色やベージュを使わず、藍に合わせたグレー）
  text: '#2b2f38',
  label: '#5a6170',
  divider: '#dfe3ea',
} as const

// 宝の地図の色（白・金・青：上部の金の帯・白に近い地）。見出しの帯とボタンは緑ではなく青
export const treasureMapPanelColors = {
  topBand: '#9a7008',
  ground: '#fbfaf5',
  band: '#1e4f96',
  // 探索中の地図の「探索中」の地と文字
  soft: '#e8eef8',
  softText: '#143a72',
  // 押せないボタンの色
  disabledButton: '#e8e3d8',
  disabledButtonText: '#6f6758',
  disabledButtonBorder: '#cfc8b8',
  // 白い箱の中の文字・区切り線
  text: '#2e2a22',
  label: '#5f5747',
  divider: '#e2dccb',
  // 星の色
  star: '#a8741a',
  starOff: '#d9d2c2',
  // 探索完了のメッセージウィンドウ
  messageWindow: 'rgba(14, 26, 54, 0.9)',
  messageHighlight: '#ffd35a',
  messageLabel: '#b8c4de',
} as const

// アイテム画面の色（道具袋：えんじの革・生成りの布・真鍮）。ほかの画面の白い箱ではなく、袋と札で組む
export const itemsPanelColors = {
  topBand: '#4f1d27',
  ground: '#ede2cf',
  // 袋の革（枠・タブ・主な操作）と、その影
  leather: '#7d3340',
  leatherDark: '#4f1d27',
  // 選んでいないタブ
  tabOff: '#a0646d',
  // 袋の布地・縫い目・マス
  canvas: '#f6ecdc',
  stitch: '#d2b48e',
  slot: '#fffaf0',
  slotLine: '#e2cfae',
  // 選んでいるマスの縁
  brass: '#b8862b',
  brassSoft: '#f6e2b0',
  text: '#2e2420',
  label: '#6a5648',
  danger: '#a05050',
  dangerText: '#8a3535',
  up: '#1f6b3a',
  down: '#a03030',
  // 値札
  priceTag: '#fbe6a6',
  priceTagText: '#4a3208',
  // 合成タブの鍛冶場
  forge: '#2a2220',
  forgeLine: '#4a3e38',
  forgeSlot: '#3a302b',
  forgeSlotLine: '#5a4a42',
  forgeEmber: '#e0782a',
  forgeEmberText: '#ffb27a',
  forgeButton: '#b4501a',
  forgeText: '#f3e6d8',
  forgeLabel: '#d8c8b8',
  forgeMuted: '#8a7a70',
} as const

// 白い箱の中の主な操作（募集開始・START・決定など）。見出しの帯と同じ緑
export const panelPrimaryButtonSx = {
  '&&': {
    backgroundColor: homePanelColors.band,
    backgroundImage: 'none',
    // 枠付きのボタンと高さ・幅をそろえるため、同じ太さの枠を同じ色で付ける
    border: `2px solid ${homePanelColors.band}`,
    color: '#ffffff',
    fontWeight: 800,
    boxShadow: 'none',
    transition: 'none',
  },
  '& .MuiTouchRipple-root': {
    display: 'none',
  },
  // ホバーしても色を変えない
  '&&:hover': {
    backgroundColor: homePanelColors.band,
    boxShadow: 'none',
  },
  '&&.Mui-disabled': {
    backgroundColor: '#a9c9ad',
    color: '#ffffff',
    boxShadow: 'none',
  },
} as const

// 白い箱の中の、主な操作の横に置く操作（参加制限・この配置で決定など）
export const panelOutlinedButtonSx = {
  '&&': {
    backgroundColor: '#ffffff',
    color: homePanelColors.band,
    border: `2px solid ${homePanelColors.band}`,
    fontWeight: 800,
    transition: 'none',
  },
  '& .MuiTouchRipple-root': {
    display: 'none',
  },
  // ホバーしても色を変えない
  '&&:hover': {
    backgroundColor: '#ffffff',
  },
  '&&.Mui-disabled': {
    color: '#9fbfa3',
    borderColor: '#c3dbc6',
  },
} as const

// 白い箱の中の取り消し操作（CANCEL）。ホバーや押したときに見た目を変えない
export const panelCancelButtonSx = {
  '&&': {
    backgroundColor: '#fff1ef',
    backgroundImage: 'none',
    border: '2px solid #d77a70',
    color: '#9f2f24',
    fontWeight: 800,
    boxShadow: 'none',
    transition: 'none',
  },
  '& .MuiTouchRipple-root': {
    display: 'none',
  },
  '&&:hover': {
    backgroundColor: '#fff1ef',
    borderColor: '#d77a70',
    boxShadow: 'none',
  },
  '&&.Mui-disabled': {
    backgroundColor: '#f8ecea',
    borderColor: '#dfb2ac',
    color: '#bf8b84',
  },
} as const

// 転職画面（聖堂の藍）。上部の帯・地・白い箱の見出しの帯・舞台の上の名札
export const jobChangePanelColors = {
  topBand: '#23476d',
  ground: '#f3f0e8',
  band: '#2f5d8a',
  bandDark: '#1f3f60',
  soft: '#e6eef7',
  gold: '#b8862b',
  goldText: '#7a5510',
  goldSoft: '#fbf1dc',
  // 舞台の上の名札と札の金
  plate: 'rgba(28, 52, 82, 0.9)',
  plateGold: '#e8c46a',
  text: '#24303d',
  label: '#4f5b69',
  divider: '#dfe4ea',
  // 未解放の札
  locked: '#f1eee8',
  lockedBorder: '#ddd6c9',
  lockedText: '#6f675b',
  canUnlock: '#fffaf0',
} as const
