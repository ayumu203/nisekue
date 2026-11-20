// Google アナリティクス 4（GA4）の読み込み。
// 測定 ID は本番（Cloudflare Pages の環境変数）にだけ設定する。未設定のビルド（ステージング・ローカル）では何もしない。
// CSP がインラインスクリプトを禁止しているため、公式スニペットの初期化処理をここで行い、gtag.js を動的に読み込む。
// ページビューは GA4 の拡張計測機能（ブラウザの履歴の変更でページビューを送る）に任せ、ここでは送らない。
const measurementId = import.meta.env.VITE_GA_MEASUREMENT_ID?.trim()

declare global {
  interface Window {
    dataLayer: unknown[]
    gtag: (...args: unknown[]) => void
  }
}

let isInitialized = false

export function initAnalytics(): void {
  if (!measurementId || isInitialized || typeof window === 'undefined') {
    return
  }
  isInitialized = true

  window.dataLayer = window.dataLayer || []
  // gtag.js は配列ではなく arguments オブジェクトが積まれることを前提にしているため、公式スニペットどおりに書く。
  window.gtag = function gtag() {
    // eslint-disable-next-line prefer-rest-params
    window.dataLayer.push(arguments)
  }
  window.gtag('js', new Date())
  window.gtag('config', measurementId)

  const script = document.createElement('script')
  script.async = true
  script.src = `https://www.googletagmanager.com/gtag/js?id=${encodeURIComponent(measurementId)}`
  document.head.appendChild(script)
}
