#!/usr/bin/env node
// テスト用 CSV とスプレッドシートから書き出した CSV の「形」を突き合わせる。
// 値の違いは正常（テスト用 CSV は固定）なので見ない。見るのはファイルの構成とヘッダー（列名と順番）だけ。
//
//   node tools/masterdata/src/structure.mjs <テスト用CSVのディレクトリ> <書き出したCSVのディレクトリ>
//
// 例: node tools/masterdata/src/structure.mjs tests/server.tests/fixtures/resources server/resources

import { readdir, readFile } from 'node:fs/promises'
import { join, relative, resolve, sep } from 'node:path'
import { fileURLToPath } from 'node:url'

async function listCsv(root) {
  const files = []
  async function walk(dir) {
    for (const entry of await readdir(dir, { withFileTypes: true })) {
      const full = join(dir, entry.name)
      if (entry.isDirectory()) await walk(full)
      else if (entry.name.endsWith('.csv')) files.push(relative(root, full).split(sep).join('/'))
    }
  }
  await walk(root)
  return files.sort()
}

async function readHeader(path) {
  const text = await readFile(path, 'utf8')
  return text.split(/\r?\n/, 1)[0].split(',').map((name) => name.trim())
}

/** 2つのディレクトリの CSV を比べ、ずれの説明の配列を返す（空ならずれなし）。 */
export async function compareStructures(fixtureRoot, actualRoot) {
  const problems = []
  const fixtureFiles = await listCsv(fixtureRoot)
  const actualFiles = await listCsv(actualRoot)
  const actualSet = new Set(actualFiles)
  const fixtureSet = new Set(fixtureFiles)

  for (const file of fixtureFiles) {
    if (!actualSet.has(file)) problems.push(`${file}: テスト用 CSV にはあるが、スプレッドシートにない`)
  }
  for (const file of actualFiles) {
    if (!fixtureSet.has(file)) problems.push(`${file}: スプレッドシートにはあるが、テスト用 CSV にない`)
  }
  for (const file of fixtureFiles.filter((f) => actualSet.has(f))) {
    const expected = await readHeader(join(fixtureRoot, file))
    const actual = await readHeader(join(actualRoot, file))
    if (expected.join(',') !== actual.join(',')) {
      const missing = expected.filter((name) => !actual.includes(name))
      const extra = actual.filter((name) => !expected.includes(name))
      const detail = [
        missing.length > 0 ? `スプレッドシートに足りない列: ${missing.join(', ')}` : '',
        extra.length > 0 ? `テスト用 CSV にない列: ${extra.join(', ')}` : '',
        missing.length === 0 && extra.length === 0 ? '列の順番が違う' : '',
      ]
        .filter(Boolean)
        .join(' / ')
      problems.push(`${file}: ヘッダーが一致しない（${detail}）`)
    }
  }
  return problems
}

async function main() {
  const [fixtureRoot, actualRoot] = process.argv.slice(2)
  if (!fixtureRoot || !actualRoot) {
    console.error('使い方: node tools/masterdata/src/structure.mjs <テスト用CSVのディレクトリ> <書き出したCSVのディレクトリ>')
    process.exit(2)
  }
  const problems = await compareStructures(resolve(fixtureRoot), resolve(actualRoot))
  if (problems.length === 0) {
    console.log('テスト用 CSV とスプレッドシートの形（ファイル構成・ヘッダー）は一致しています')
    return
  }
  console.error('テスト用 CSV とスプレッドシートの形がずれています。スプレッドシートの更新を忘れていないか確認してください:')
  for (const problem of problems) console.error(`  - ${problem}`)
  process.exit(1)
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  main().catch((err) => {
    console.error(`エラー: ${err.message}`)
    process.exit(1)
  })
}
