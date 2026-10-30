import axios from 'axios'
import { toApiError } from './errors'

/**
 * 全站共用的 Axios 實例。各模組的 api.js 都透過它呼叫後端。
 * - 自動帶上 Access Token
 * - 收到 401 時先嘗試換發一次，成功就重送原本的請求
 * - 錯誤一律轉成 ApiError（見 errors.js）
 */
export const http = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
})

let auth = {
  getAccessToken: () => null,
  refresh: async () => false,
  onAuthFailure: () => {},
}

/** 由 main.js 注入登入狀態的存取方式，避免 http 層直接依賴 auth 模組。 */
export function configureHttpAuth(handlers) {
  auth = { ...auth, ...handlers }
}

http.interceptors.request.use((config) => {
  const token = auth.getAccessToken()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

http.interceptors.response.use(
  (response) => response,
  async (error) => {
    const original = error.config
    const canRetry = error.response?.status === 401 && original && !original.skipAuthRefresh && !original._retried

    if (canRetry) {
      original._retried = true
      if (await auth.refresh()) {
        return http(original)
      }
      auth.onAuthFailure()
    }

    return Promise.reject(toApiError(error))
  },
)
