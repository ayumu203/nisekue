/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** GA4 の測定 ID（G-XXXXXXXXXX）。本番だけに設定する */
  readonly VITE_GA_MEASUREMENT_ID?: string
}
