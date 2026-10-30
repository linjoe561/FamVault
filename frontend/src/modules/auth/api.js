import { http } from '@/shared/api/http'

// 這些請求本身就是在處理登入狀態，401 時不要再觸發自動換發
const noRefresh = { skipAuthRefresh: true }

export const authApi = {
  getSetupStatus: () => http.get('/auth/setup-status', noRefresh).then((r) => r.data),
  setup: (form) => http.post('/auth/setup', form, noRefresh).then((r) => r.data),
  login: (form) => http.post('/auth/login', form, noRefresh).then((r) => r.data),
  refresh: (refreshToken) => http.post('/auth/refresh', { refreshToken }, noRefresh).then((r) => r.data),
  logout: (refreshToken) => http.post('/auth/logout', { refreshToken }, noRefresh),
  me: () => http.get('/auth/me').then((r) => r.data),
  changePassword: (form) => http.put('/auth/me/password', form),
}
