import js from '@eslint/js'
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import tseslint from 'typescript-eslint'
import { defineConfig, globalIgnores } from 'eslint/config'

// 規定値（src/theme/tokens.ts）以外の文字サイズ・間隔を検出する
const spacingKeys =
  '^(p|pt|pr|pb|pl|px|py|m|mt|mr|mb|ml|mx|my|padding|paddingTop|paddingRight|paddingBottom|paddingLeft|paddingX|paddingY|margin|marginTop|marginRight|marginBottom|marginLeft|marginX|marginY|gap|rowGap|columnGap)$'
const spacingScale = '0|0\\.25|0\\.5|0\\.75|1|1\\.5|2|2\\.5|3|4|6'
const spacingList = spacingScale.replaceAll('\\', '').replaceAll('|', '・')

// 値として書かれたリテラル（そのまま／三項演算子／画面幅ごとの指定／マイナス）だけを見る。計算式の中の数値は対象外
const valueLiterals = (owner) =>
  [
    `${owner} > Literal`,
    `${owner} > UnaryExpression > Literal`,
    `${owner} > ConditionalExpression > Literal`,
    `${owner} > ConditionalExpression > UnaryExpression > Literal`,
    `${owner} > ObjectExpression > Property > Literal`,
    `${owner} > ObjectExpression > Property > UnaryExpression > Literal`,
    `${owner} > ConditionalExpression > ObjectExpression > Property > Literal`,
    `${owner} > ConditionalExpression > ObjectExpression > Property > UnaryExpression > Literal`,
    `${owner} > JSXExpressionContainer > Literal`,
    `${owner} > JSXExpressionContainer > ConditionalExpression > Literal`,
    `${owner} > JSXExpressionContainer > ObjectExpression > Property > Literal`,
  ].join(', ')

const fontOwner = "Property[key.name='fontSize']"
const spacingOwner = `:matches(Property[key.name=/${spacingKeys}/], JSXAttribute[name.name=/^(spacing|rowSpacing|columnSpacing)$/])`

const designTokenRules = [
  {
    selector: `:matches(${valueLiterals(fontOwner)})[raw=/^[0-9.]+$/]`,
    message: '文字サイズは font（@/theme/tokens）から選んでください',
  },
  {
    selector: `:matches(${valueLiterals(fontOwner)}, JSXAttribute[name.name='fontSize'] > Literal)[value=/^[0-9.]+(px|rem)$/]`,
    message: '文字サイズは font（@/theme/tokens）から選んでください',
  },
  {
    selector: `:matches(${valueLiterals(spacingOwner)})[raw=/^[0-9.]+$/][raw!=/^(${spacingScale})$/]`,
    message: `間隔は ${spacingList} から選んでください（MUI の spacing の倍率）`,
  },
  {
    selector: `:matches(${valueLiterals(spacingOwner)})[value=/^-?[0-9.]+(px|rem)$/]`,
    message: '間隔は px・rem で書かず、MUI の spacing の倍率（0.25〜6）で書いてください',
  },
]

export default defineConfig([
  globalIgnores(['dist']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      js.configs.recommended,
      tseslint.configs.recommended,
      reactHooks.configs.flat.recommended,
      reactRefresh.configs.vite,
    ],
    languageOptions: {
      ecmaVersion: 2020,
      globals: globals.browser,
    },
  },
  {
    files: ['src/**/*.{ts,tsx}'],
    // 規定値の定義と、アイコンなどの絵は対象外
    ignores: ['src/theme/**', 'src/components/items/ItemIllustrations.tsx'],
    rules: {
      'no-restricted-syntax': ['error', ...designTokenRules],
    },
  },
])
