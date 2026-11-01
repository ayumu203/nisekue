import { GoogleAuth } from 'google-auth-library'

const SCOPES = ['https://www.googleapis.com/auth/spreadsheets.readonly']
const API = 'https://sheets.googleapis.com/v4/spreadsheets'

/**
 * Sheets API クライアントを作る。
 * - GOOGLE_SERVICE_ACCOUNT_JSON にサービスアカウントキーの JSON 文字列があればそれを使う（GitHub Actions 用）
 * - なければ Application Default Credentials（GOOGLE_APPLICATION_CREDENTIALS や gcloud のログイン）を使う
 */
export async function createSheetsClient() {
  const json = process.env.GOOGLE_SERVICE_ACCOUNT_JSON
  const auth = new GoogleAuth({
    scopes: SCOPES,
    ...(json ? { credentials: JSON.parse(json) } : {}),
  })
  return auth.getClient()
}

function quoteTab(tab) {
  return `'${tab.replace(/'/g, "''")}'`
}

/** 複数レンジの値を UNFORMATTED_VALUE でまとめて取得する。戻り値はレンジ順の 2 次元配列。 */
export async function batchGetValues(client, spreadsheetId, ranges) {
  const params = new URLSearchParams({ valueRenderOption: 'UNFORMATTED_VALUE', majorDimension: 'ROWS' })
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
