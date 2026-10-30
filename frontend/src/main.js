import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import { router } from './app/router'
import { useAuthStore } from './modules/auth/store'
import { configureHttpAuth } from './shared/api/http'
import './design/tokens.css'
import './design/base.css'

const app = createApp(App)
app.use(createPinia())

const auth = useAuthStore()
configureHttpAuth({
  getAccessToken: () => auth.accessToken,
  refresh: () => auth.refresh(),
  onAuthFailure: () => {
    auth.clearSession()
    const current = router.currentRoute.value
    if (!current.meta.public) {
      router.replace({ name: 'login', query: { redirect: current.fullPath } })
    }
  },
})

app.use(router)
app.mount('#app')
