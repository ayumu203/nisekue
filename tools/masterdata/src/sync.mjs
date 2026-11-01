#!/usr/bin/env node
// スプレッドシートのマスタデータを server/resources 配下の CSV に書き出す。
//
//   pnpm masterdata:sync                     # リポジトリのルートから
//   node tools/masterdata/src/sync.mjs --out <dir>
//
// 環境変数
//   MASTERDATA_SPREADSHEET_ID     対象スプレッドシート（省略時は DEFAULT_SPREADSHEET_ID）
//   GOOGLE_SERVICE_ACCOUNT_JSON   サービスアカウントキー（JSON 文字列）。省略時は ADC を使う

import { mkdir, readdir, writeFile } from 'node:fs/promises'
import { dirname, join, relative, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'
import { parseIndex, tableToCsv } from './csv.mjs'
import { createSheetsClient, getIndexValues, getTabValues } from './sheets.mjs'

const DEFAULT_SPREADSHEET_ID = '1u8eKVM50g39nCXUWDl7yAgNVkZ3UxpnR9H4VwkMmbJY'
const RESOURCES_PREFIX = 'server/resources/'
const repoRoot = resolve(dirname(fileURLToPath(import.meta.url)), '../../..')

function parseArgs(argv) {
  const args = { out: repoRoot }
  for (let i = 0; i < argv.length; i++) {
    if (argv[i] === '--out') args.out = resolve(argv[++i])
    else if (argv[i] === '--help' || argv[i] === '-h') args.help = true
    else throw new Error(`不明な引数です: ${argv[i]}`)
  }
  return args
}

async function listCsvFiles(dir) {
  const found = []
  async function walk(current) {
    let entries
    try {
      entries = await readdir(current, { withFileTypes: true })
    } catch {
      return
    }
    for (const entry of entries) {
      const full = join(current, entry.name)
      if (entry.isDirectory()) await walk(full)
      else if (entry.name.endsWith('.csv')) found.push(full)
    }
  }
  await walk(dir)
  return found
}

async function main() {
  const args = parseArgs(process.argv.slice(2))
  if (args.help) {
    console.log('使い方: node tools/masterdata/src/sync.mjs [--out <出力先のルート>]')
    return
  }

  const spreadsheetId = process.env.MASTERDATA_SPREADSHEET_ID || DEFAULT_SPREADSHEET_ID
  const client = await createSheetsClient()

  const index = parseIndex(await getIndexValues(client, spreadsheetId), RESOURCES_PREFIX)
  const tables = await getTabValues(
    client,
    spreadsheetId,
    index.map((entry) => entry.tab),
  )

  // 全タブを変換してから書き込む（途中でエラーになったら何も書き換えない）
  const outputs = index.map((entry, i) => ({ ...entry, csv: tableToCsv(tables[i], entry.tab) }))

  for (const { path, csv } of outputs) {
    const target = join(args.out, path)
    await mkdir(dirname(target), { recursive: true })
    await writeFile(target, csv, 'utf8')
    console.log(`書き出し: ${path} (${csv.split('\n').length - 2} 行)`)
  }

  const written = new Set(outputs.map((o) => resolve(args.out, o.path)))
  const stale = (await listCsvFiles(join(args.out, RESOURCES_PREFIX))).filter((file) => !written.has(resolve(file)))
  for (const file of stale) {
    console.warn(`警告: _index に載っていない CSV があります: ${relative(args.out, file)}`)
  }
}

main().catch((err) => {
  const detail = err?.response?.data?.error?.message
  console.error(`エラー: ${err.message}${detail ? ` (${detail})` : ''}`)
  process.exit(1)
})
