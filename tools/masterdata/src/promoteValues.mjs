// ステージング用スプレッドシートの内容を本番用へ昇格するときの純粋関数群。

/**
 * FORMULA で取得したセル値を、USER_ENTERED で書き込んでも同じ内容になる値に変換する。
 * - 数式（= で始まる文字列）はそのまま書いて数式として残す
 * - それ以外の文字列は先頭に ' を付け、`0.30` や `true` などが数値・真偽値に変換されないようにする
 * - 数値・真偽値・空はそのまま
 */
export function toUserEnteredValue(value) {
  if (value === null || value === undefined) return ''
  if (typeof value !== 'string') return value
  if (value === '') return ''
  if (value.startsWith('=')) return value
  return `'${value}`
}

/** 1 タブ分の 2 次元配列を書き込み用に変換する。 */
export function toUserEnteredTable(values) {
  return (values ?? []).map((row) => (row ?? []).map(toUserEnteredValue))
}

function normalizeRow(row) {
  const cells = (row ?? []).map((value) => (value === null || value === undefined ? '' : value))
  while (cells.length > 0 && cells[cells.length - 1] === '') cells.pop()
  return JSON.stringify(cells)
}

/**
 * 2 つのタブの値（UNFORMATTED_VALUE）を行単位で比べる。
 * 行番号が同じ行どうしを比較し、変わった行・増えた行・減った行の数と、変わった行番号（最大 limit 件）を返す。
 */
export function diffTable(before, after, limit = 20) {
  const a = (before ?? []).map(normalizeRow)
  const b = (after ?? []).map(normalizeRow)
  const trim = (rows) => {
    const copy = [...rows]
    while (copy.length > 0 && copy[copy.length - 1] === '[]') copy.pop()
    return copy
  }
  const left = trim(a)
  const right = trim(b)
  const changedRows = []
  let changed = 0
  for (let i = 0; i < Math.min(left.length, right.length); i++) {
    if (left[i] !== right[i]) {
      changed++
      if (changedRows.length < limit) changedRows.push(i + 1)
    }
  }
  return {
    changed,
    added: Math.max(0, right.length - left.length),
    removed: Math.max(0, left.length - right.length),
    changedRows,
  }
}

export function hasDiff(diff) {
  return diff.changed + diff.added + diff.removed > 0
}

/** GitHub Actions のジョブサマリー用の Markdown を作る。 */
export function formatDiffSummary(results) {
  const lines = ['| タブ | 変更 | 追加 | 削除 | 変更した行（先頭 20 件） |', '| --- | ---: | ---: | ---: | --- |']
  for (const { tab, diff } of results) {
    if (!hasDiff(diff)) continue
    lines.push(`| ${tab} | ${diff.changed} | ${diff.added} | ${diff.removed} | ${diff.changedRows.join(', ')} |`)
  }
  if (lines.length === 2) return '差分はありません。\n'
  return `${lines.join('\n')}\n`
}
