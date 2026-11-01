#!/usr/bin/env node
// masterdata.lock に書かれた版のスナップショットを GitHub Release から取得し、
// server/resources 配下に書き出す。認証は不要（公開リポジトリの Release アセットを使う）。
// 依存パッケージなしで動くので、デプロイのワークフローからもそのまま呼べる。
//
//   pnpm masterdata:pull                                   # リポジトリのルートから
//   node tools/masterdata/src/pull.mjs [--version 3] [--out <dir>] [--file <bundle.json>] [--if-locked]
//
//   --version    lock を無視して指定の版（masterdata-<連番> の連番）を取得する
//   --out        書き出し先のルート（省略時はリポジトリのルート）
//   --file       Release ではなくローカルのスナップショット JSON から書き出す
//   --if-locked  masterdata.lock がなければ何もせず終了する

import { existsSync, readFileSync } from 'node:fs'
import { mkdir, readFile, writeFile } from 'node:fs/promises'
import { dirname, join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'
import { normalizeResourcePath } from './csv.mjs'

const repoRoot = resolve(dirname(fileURLToPath(import.meta.url)), '../../..')
const LOCK_FILE = join(repoRoot, 'masterdata.lock')
const DEFAULT_REPOSITORY = 'ayumu203/nisekue'
const VERSION_PATTERN = /^[1-9]\d*$/

export function releaseTag(version) {
  return `masterdata-${version}`
}

export function assetUrl(repository, version) {
  return `https://github.com/${repository}/releases/download/${releaseTag(version)}/masterdata.json`
}

function parseArgs(argv) {
  const args = { out: repoRoot }
  for (let i = 0; i < argv.length; i++) {
    if (argv[i] === '--version') args.version = argv[++i]
    else if (argv[i] === '--out') args.out = resolve(argv[++i])
    else if (argv[i] === '--file') args.file = resolve(argv[++i])
    else if (argv[i] === '--if-locked') args.ifLocked = true
    else if (argv[i] === '--help' || argv[i] === '-h') args.help = true
    else throw new Error(`不明な引数です: ${argv[i]}`)
  }
  return args
}

function readLockedVersion() {
  if (!existsSync(LOCK_FILE)) return undefined
  return readFileSync(LOCK_FILE, 'utf8').trim()
}

async function loadBundle(args, version) {
  if (args.file) return JSON.parse(await readFile(args.file, 'utf8'))
  const repository = process.env.MASTERDATA_REPOSITORY || DEFAULT_REPOSITORY
  const url = assetUrl(repository, version)
  const res = await fetch(url)
  if (!res.ok) throw new Error(`スナップショットを取得できませんでした (${res.status} ${url})`)
  return res.json()
}

/** スナップショット JSON の中身を検証して書き出す。書き出したパスの一覧を返す。 */
export async function writeBundle(bundle, outRoot) {
  const entries = Object.entries(bundle?.files ?? {})
  if (entries.length === 0) throw new Error('スナップショットに CSV が含まれていません')
  const files = entries.map(([path, content]) => {
    if (typeof content !== 'string') throw new Error(`${path}: 内容が文字列ではありません`)
    return { path: normalizeResourcePath(path, 'スナップショット'), content }
  })
  for (const { path, content } of files) {
    const target = join(outRoot, path)
    await mkdir(dirname(target), { recursive: true })
    await writeFile(target, content, 'utf8')
  }
  return files.map((f) => f.path)
}

async function main() {
  const args = parseArgs(process.argv.slice(2))
  if (args.help) {
    console.log('使い方: node tools/masterdata/src/pull.mjs [--version <連番>] [--out <dir>] [--file <bundle.json>] [--if-locked]')
    return
  }

  const version = args.version ?? readLockedVersion()
  if (!args.file) {
    if (version === undefined) {
      if (args.ifLocked) {
        console.log('masterdata.lock がないため、リポジトリの CSV をそのまま使います')
        return
      }
      throw new Error('masterdata.lock がありません。--version で版を指定してください')
    }
    if (!VERSION_PATTERN.test(version)) throw new Error(`版の形式が不正です（1以上の整数）: ${version}`)
  }

  const paths = await writeBundle(await loadBundle(args, version), args.out)
  console.log(`マスタデータ ${args.file ? args.file : version} を書き出しました (${paths.length} ファイル)`)
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  main().catch((err) => {
    console.error(`エラー: ${err.message}`)
    process.exit(1)
  })
}
