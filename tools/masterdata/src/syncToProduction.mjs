#!/usr/bin/env node
// ステージング用スプレッドシートの内容で、本番用スプレッドシートを上書きする。
//
//   node tools/masterdata/src/syncToProduction.mjs --from <ステージング用ID> --to <本番用ID> [--dry-run]
//
// - 対象はステージング用の `_index` タブと、そこに載っているタブだけ。それ以外のタブ（メモなど）は触らない
// - 本番用に無いタブは追加する。本番用にだけあるタブは消さない（`_index` から外れるので使われなくなる）
// - 数式は数式のまま、テキストはテキストのまま写す
// - 差分を標準出力と GITHUB_STEP_SUMMARY に出す
//
// 環境変数
//   GOOGLE_SERVICE_ACCOUNT_JSON  サービスアカウントキー（JSON 文字列）。本番用スプレッドシートの編集者であること

import { appendFile } from 'node:fs/promises'
import { parseIndex } from './csv.mjs'
import { addSheets, batchGetValues, createSheetsClient, listSheetTitles, overwriteTabs, quoteTab } from './sheets.mjs'
import { diffTable, formatDiffSummary, hasDiff, toUserEnteredTable } from './syncToProductionValues.mjs'

const INDEX_TAB = '_index'

function parseArgs(argv) {
  const args = { dryRun: false }
  for (let i = 0; i < argv.length; i++) {
    if (argv[i] === '--from') args.from = argv[++i]?.trim()
    else if (argv[i] === '--to') args.to = argv[++i]?.trim()
    else if (argv[i] === '--dry-run') args.dryRun = true
    else throw new Error(`不明な引数です: ${argv[i]}`)
  }
  if (!args.from || !args.to) throw new Error('--from と --to を指定してください')
  if (args.from === args.to) throw new Error('--from と --to が同じスプレッドシートです')
  return args
}

async function writeSummary(markdown) {
  const file = process.env.GITHUB_STEP_SUMMARY
  if (file) await appendFile(file, markdown, 'utf8')
}

async function main() {
  const args = parseArgs(process.argv.slice(2))
  const client = await createSheetsClient({ write: !args.dryRun })

  const [indexValues] = await batchGetValues(client, args.from, [`${quoteTab(INDEX_TAB)}!A2:B`])
  // _index の書式チェックを兼ねる（壊れていたら本番に触らずに止める）
  const index = parseIndex(indexValues, 'server/resources/')
  const tabs = [INDEX_TAB, ...index.map((entry) => entry.tab)]
  const ranges = tabs.map(quoteTab)

  const prodTitles = new Set(await listSheetTitles(client, args.to))
  const missing = tabs.filter((tab) => !prodTitles.has(tab))

  const [stagingValues, stagingFormulas, prodValues] = await Promise.all([
    batchGetValues(client, args.from, ranges),
    batchGetValues(client, args.from, ranges, 'FORMULA'),
    batchGetValues(
      client,
      args.to,
      tabs.filter((tab) => prodTitles.has(tab)).map(quoteTab),
    ),
  ])
  const prodByTab = new Map(tabs.filter((tab) => prodTitles.has(tab)).map((tab, i) => [tab, prodValues[i]]))

  const results = tabs.map((tab, i) => ({ tab, diff: diffTable(prodByTab.get(tab) ?? [], stagingValues[i]) }))
  const summary =
    `## マスタデータの本番への同期${args.dryRun ? '（確認のみ）' : ''}\n\n` +
    (missing.length > 0 ? `本番用に無いため追加するタブ: ${missing.join(', ')}\n\n` : '') +
    formatDiffSummary(results)
  console.log(summary)
  await writeSummary(summary)

  if (!results.some(({ diff }) => hasDiff(diff)) && missing.length === 0) {
    console.log('差分が無いので書き込みません。')
    return
  }
  if (args.dryRun) return

  await addSheets(client, args.to, missing)
  await overwriteTabs(
    client,
    args.to,
    tabs.map((tab, i) => ({ tab, values: toUserEnteredTable(stagingFormulas[i]) })),
  )
  console.log(`本番用スプレッドシートに ${tabs.length} タブを書き込みました。`)
}

main().catch((err) => {
  const detail = err?.response?.data?.error?.message
  console.error(`エラー: ${err.message}${detail ? ` (${detail})` : ''}`)
  process.exit(1)
})
