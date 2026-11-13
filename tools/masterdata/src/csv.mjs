// スプレッドシートの値を、サーバーの CSV ローダーが読める形式へ変換する純粋関数群。
// ローダーは各行を単純に `,` で分割するため、カンマや改行を含むセルはエラーにする。

/** Sheets API (UNFORMATTED_VALUE) が返すセル値を CSV 用の文字列にする。 */
export function cellToString(value, where) {
  if (value === null || value === undefined) return ''
  if (typeof value === 'boolean') return value ? 'true' : 'false'
  if (typeof value === 'number') {
    if (!Number.isFinite(value)) throw new Error(`${where}: 数値として扱えない値です (${value})`)
    const text = String(value)
    if (/e/i.test(text)) throw new Error(`${where}: 指数表記になる数値は扱えません (${text})。テキストとして入力してください`)
    return text
  }
  const text = String(value)
  if (text.includes(',')) throw new Error(`${where}: セルにカンマを含めることはできません (${JSON.stringify(text)})`)
  if (/[\r\n]/.test(text)) throw new Error(`${where}: セルに改行を含めることはできません (${JSON.stringify(text)})`)
  return text
}

function columnName(index) {
  let name = ''
  for (let n = index + 1; n > 0; n = Math.floor((n - 1) / 26)) {
    name = String.fromCharCode(65 + ((n - 1) % 26)) + name
  }
  return name
}

function isEmptyCell(value) {
  return value === null || value === undefined || value === ''
}

/**
 * 1タブ分の値（先頭行がヘッダー）を CSV テキストにする。
 * - 列数はヘッダー行の長さで決める（ヘッダー右側の空セルは無視）
 * - 完全に空の行は捨てる
 * - ヘッダーより右にデータがある行はエラー
 * - 改行は LF、末尾に改行を付ける
 */
export function tableToCsv(values, tabName) {
  const rows = values ?? []
  const header = [...(rows[0] ?? [])]
  while (header.length > 0 && isEmptyCell(header[header.length - 1])) header.pop()
  if (header.length === 0) throw new Error(`${tabName}: ヘッダー行がありません`)

  const headerNames = header.map((value, col) => {
    const name = cellToString(value, `${tabName}!${columnName(col)}1`).trim()
    if (name === '') throw new Error(`${tabName}!${columnName(col)}1: ヘッダーが空です`)
    return name
  })
  const duplicated = headerNames.filter((name, i) => headerNames.indexOf(name) !== i)
  if (duplicated.length > 0) throw new Error(`${tabName}: ヘッダーが重複しています (${[...new Set(duplicated)].join(', ')})`)

  const lines = [headerNames.join(',')]
  rows.slice(1).forEach((row, i) => {
    const rowNo = i + 2
    const cells = row ?? []
    if (cells.every(isEmptyCell)) return
    cells.slice(header.length).forEach((value, offset) => {
      if (!isEmptyCell(value)) {
        throw new Error(`${tabName}!${columnName(header.length + offset)}${rowNo}: ヘッダーより右にデータがあります`)
      }
    })
    const line = headerNames.map((_, col) => cellToString(cells[col], `${tabName}!${columnName(col)}${rowNo}`))
    lines.push(line.join(','))
  })

  return lines.join('\n') + '\n'
}

/**
 * `_index` タブの値（ヘッダー行を除く [タブ名, CSVパス] の配列）を検証して返す。
 * 書き込み先は resourcesPrefix 配下の .csv に限定する。
 */
export function parseIndex(values, resourcesPrefix = 'server/resources/') {
  const entries = []
  ;(values ?? []).forEach((row, i) => {
    const rowNo = i + 2
    const tab = String(row?.[0] ?? '').trim()
    const path = String(row?.[1] ?? '').trim()
    if (tab === '' && path === '') return
    if (tab === '' || path === '') throw new Error(`_index!A${rowNo}: タブ名とCSVパスの両方が必要です`)
    const normalized = path.replace(/\\/g, '/')
    if (
      !normalized.startsWith(resourcesPrefix) ||
      !normalized.endsWith('.csv') ||
      normalized.split('/').some((part) => part === '..' || part === '')
    ) {
      throw new Error(`_index!B${rowNo}: CSVパスは ${resourcesPrefix} 配下の .csv である必要があります (${path})`)
    }
    entries.push({ tab, path: normalized })
  })

  const findDuplicates = (key) => entries.map((e) => e[key]).filter((v, i, all) => all.indexOf(v) !== i)
  for (const key of ['tab', 'path']) {
    const duplicated = findDuplicates(key)
    if (duplicated.length > 0) throw new Error(`_index: ${key === 'tab' ? 'タブ名' : 'CSVパス'}が重複しています (${duplicated.join(', ')})`)
  }
  if (entries.length === 0) throw new Error('_index: 書き出し対象がありません')
  return entries
}
