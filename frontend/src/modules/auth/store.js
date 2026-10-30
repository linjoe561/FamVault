import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { storage } from '@/shared/utils/storage'
import { authApi } from './api'

const REFRESH_TOKEN_KEY = 'invcontrol.refreshToken'

/**
 * 登入狀態。
 * Access Token 只放在記憶體；Refresh Token 放 localStorage，重新整理頁面後用它換回登入狀態。
 */
export const useAuthStore = defineStore('auth', () => {
  const user = ref(null)
  const accessToken = ref(null)

  const isAuthenticated = computed(() => !!user.value && !!accessToken.value)
  const isAdmin = computed(() => user.value?.role === 'Admin')

  let initPromise = null
  let refreshPromise = null

  function applySession(session) {
    accessToken.value = session.accessToken
    user.value = session.user
    storage.set(REFRESH_TOKEN_KEY, session.refreshToken)
  }

  function clearSession() {
    accessToken.value = null
    user.value = null
    storage.remove(REFRESH_TOKEN_KEY)
  }

  async function login(form) {
    applySession(await authApi.login(form))
  }

  async function setup(form) {
    applySession(await authApi.setup(form))
  }

  /** 同時有多個請求遇到 401 時，只換發一次。 */
  function refresh() {
    refreshPromise ??= (async () => {
      const refreshToken = storage.get(REFRESH_TOKEN_KEY)
      if (!refreshToken) {
        clearSession()
        return false
      }
      try {
        applySession(await authApi.refresh(refreshToken))
        return true
      } catch {
        clearSession()
        return false
      } finally {
        refreshPromise = null
      }
    })()
    return refreshPromise
  }

  /** App 啟動時呼叫一次：有 Refresh Token 就嘗試恢復登入狀態。 */
  function init() {
    initPromise ??= storage.get(REFRESH_TOKEN_KEY) ? refresh() : Promise.resolve(false)
    return initPromise
  }

  async function logout() {
    const refreshToken = storage.get(REFRESH_TOKEN_KEY)
    clearSession()
    if (refreshToken) {
      await authApi.logout(refreshToken).catch(() => {})
    }
  }

  function updateUser(patch) {
    user.value = { ...user.value, ...patch }
  }

  return { user, accessToken, isAuthenticated, isAdmin, init, login, setup, refresh, logout, clearSession, updateUser }
})
