import { fileURLToPath, URL } from 'node:url'
import { defineConfig, loadEnv } from 'vite'
import vue from '@vitejs/plugin-vue'
import basicSsl from '@vitejs/plugin-basic-ssl'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')

  return {
    // 手機用鏡頭掃條碼時瀏覽器要求 HTTPS：`npm run dev:https` 會啟用自簽憑證。
    plugins: [vue(), ...(env.VITE_DEV_HTTPS === 'true' ? [basicSsl()] : [])],
    resolve: {
      alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) },
    },
    server: {
      host: true,
      port: 5173,
      // 開發時前端只對自己發請求，再由 Vite 轉給後端，不需要處理 CORS。
      proxy: {
        '/api': { target: env.VITE_API_PROXY_TARGET || 'http://localhost:5080', changeOrigin: true },
      },
    },
  }
})
