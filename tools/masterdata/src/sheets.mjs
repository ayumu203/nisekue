import { GoogleAuth } from 'google-auth-library'

const READONLY_SCOPES = ['https://www.googleapis.com/auth/spreadsheets.readonly']
const READWRITE_SCOPES = ['https://www.googleapis.com/auth/spreadsheets']
const API = 'https://sheets.googleapis.com/v4/spreadsheets'

/**
 * Sheets API クライアントを作る。
 * - GOOGLE_SERVICE_ACCOUNT_JSON にサービスアカウントキーの JSON 文字列があればそれを使う（GitHub Actions 用）
 * - なければ Application Default Credentials（GOOGLE_APPLICATION_CREDENTIALS や gcloud のログイン）を使う
 * - write: true のときは書き込みの権限を要求する（本番用スプレッドシートへの同期用）
 */
export async function createSheetsClient({ write = false } = {}) {
  const json = process.env.GOOGLE_SERVICE_ACCOUNT_JSON
  const auth = new GoogleAuth({
    scopes: write ? READWRITE_SCOPES : READONLY_SCOPES,
    ...(json ? { credentials: JSON.parse(json) } : {}),
  })
  return auth.getClient()
}

export function quoteTab(tab) {
  return `'${tab.replace(/'/g, "''")}'`
}

/** 複数レンジの値をまとめて取得する（既定は UNFORMATTED_VALUE）。戻り値はレンジ順の 2 次元配列。 */
export async function batchGetValues(client, spreadsheetId, ranges, valueRenderOption = 'UNFORMATTED_VALUE') {
  const params = new URLSearchParams({ valueRenderOption, majorDimension: 'ROWS' })
  for (const range of ranges) params.append('ranges', range)
  const res = await client.request({ url: `${API}/${encodeURIComponent(spreadsheetId)}/values:batchGet?${params}` })
  return (res.data.valueRanges ?? []).map((vr) => vr.values ?? [])
}

export async function getIndexValues(client, spreadsheetId) {
  const [values] = await batchGetValues(client, spreadsheetId, [`${quoteTab('_index')}!A2:B`])
  return values
}

export async function getTabValues(client, spreadsheetId, tabs) {
  return batchGetValues(
    client,
    spreadsheetId,
    tabs.map((tab) => quoteTab(tab)),
  )
}

/** タブ名の一覧を返す。 */
export async function listSheetTitles(client, spreadsheetId) {
  const res = await client.request({
    url: `${API}/${encodeURIComponent(spreadsheetId)}?fields=sheets.properties.title`,
  })
  return (res.data.sheets ?? []).map((sheet) => sheet.properties.title)
}

/** 足りないタブを末尾に追加する。 */
export async function addSheets(client, spreadsheetId, titles) {
  if (titles.length === 0) return
  await client.request({
    url: `${API}/${encodeURIComponent(spreadsheetId)}:batchUpdate`,
    method: 'POST',
    data: { requests: titles.map((title) => ({ addSheet: { properties: { title } } })) },
  })
}

/** 各タブの内容を消してから、値を USER_ENTERED で書き込む。 */
export async function overwriteTabs(client, spreadsheetId, tabValues) {
  const id = encodeURIComponent(spreadsheetId)
  await client.request({
    url: `${API}/${id}/values:batchClear`,
    method: 'POST',
    data: { ranges: tabValues.map(({ tab }) => quoteTab(tab)) },
  })
  await client.request({
    url: `${API}/${id}/values:batchUpdate`,
    method: 'POST',
    data: {
      valueInputOption: 'USER_ENTERED',
      data: tabValues.map(({ tab, values }) => ({ range: `${quoteTab(tab)}!A1`, majorDimension: 'ROWS', values })),
    },
  })
}
