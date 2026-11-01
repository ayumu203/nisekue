import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react-swc'
import path from 'node:path'

const appBasePath = process.env.VITE_APP_BASE_PATH ?? '/'
// Codespaces など、ブラウザからローカル Supabase (:54321) に直接届かない環境でのみ設定する。
// 設定時は /supabase をローカル Supabase へプロキシする（.devcontainer/start.sh 参照）。
const supabaseProxyTarget = process.env.SUPABASE_PROXY_TARGET

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  base: appBasePath,
  resolve: {
    alias: {
      '@': path.resolve(__dirname, 'src'),
    },
  },
  build: {
    rollupOptions: {
      output: {
        manualChunks: {
          react: ['react', 'react-dom', 'react/jsx-runtime'],
          mui: ['@mui/material', '@emotion/react', '@emotion/styled'],
          supabase: ['@supabase/supabase-js'],
          swr: ['swr'],
          validation: ['zod'],
        },
      },
    },
  },
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5068',
        changeOrigin: true,
        ws: true,
        rewrite: (path) => path.replace(/^\/api/, ''),
      },
      ...(supabaseProxyTarget
        ? {
            '/supabase': {
              target: supabaseProxyTarget,
              changeOrigin: true,
              ws: true,
              rewrite: (path: string) => path.replace(/^\/supabase/, ''),
            },
          }
        : {}),
    },
  },
})
