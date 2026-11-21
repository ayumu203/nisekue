// 通報用・画像投稿の申請用の Google Form の URL を作る。
// 環境変数には Google Form の「事前入力した URL」を入れ、ID を入れたい箇所に {threadId} / {playerId} と書いておく
// （Google Form がエンコードした %7BthreadId%7D のままでもよい）。未設定ならリンクを出さない。

function fillTemplate(template: string | undefined, name: string, value: string): string | null {
  const trimmed = template?.trim()
  if (!trimmed) {
    return null
  }

  const encoded = encodeURIComponent(value)
  return trimmed.replaceAll(`{${name}}`, encoded).replaceAll(`%7B${name}%7D`, encoded)
}

export function buildThreadReportFormUrl(threadId: string): string | null {
  return fillTemplate(import.meta.env.VITE_THREAD_REPORT_FORM_URL, 'threadId', threadId)
}

export function buildPosterApplyFormUrl(playerId: string): string | null {
  return fillTemplate(import.meta.env.VITE_POSTER_APPLY_FORM_URL, 'playerId', playerId)
}
