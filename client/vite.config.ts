import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react-swc'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  base: '/nisekue/',
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5068',
        changeOrigin: true,
      },
    },
  },
})
