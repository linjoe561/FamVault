<script setup>
import { onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import BaseButton from '@/shared/components/BaseButton.vue'
import FormAlert from '@/shared/components/FormAlert.vue'
import TextField from '@/shared/components/TextField.vue'
import AuthCard from '../components/AuthCard.vue'
import { authApi } from '../api'
import { useAuthStore } from '../store'

const auth = useAuthStore()
const route = useRoute()
const router = useRouter()

const form = reactive({ userName: '', password: '' })
const fieldErrors = ref({})
const error = ref('')
const submitting = ref(false)

onMounted(async () => {
  try {
    const { needsSetup } = await authApi.getSetupStatus()
    if (needsSetup) {
      router.replace({ name: 'setup' })
    }
  } catch (e) {
    error.value = e.message
  }
})

async function submit() {
  submitting.value = true
  error.value = ''
  fieldErrors.value = {}
  try {
    await auth.login(form)
    const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : '/'
    router.replace(redirect.startsWith('/') ? redirect : '/')
  } catch (e) {
    error.value = e.message
    fieldErrors.value = e.fieldErrors ?? {}
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <AuthCard title="歡迎回家" subtitle="登入後就能查看家裡的庫存與保存期限">
    <form class="form" novalidate @submit.prevent="submit">
      <FormAlert :message="error" />
      <TextField
        v-model="form.userName"
        label="帳號"
        autocomplete="username"
        autocapitalize="none"
        :error="fieldErrors.userName"
      />
      <TextField
        v-model="form.password"
        label="密碼"
        type="password"
        autocomplete="current-password"
        :error="fieldErrors.password"
      />
      <BaseButton type="submit" block :loading="submitting">登入</BaseButton>
    </form>
  </AuthCard>
</template>

<style scoped>
.form {
  display: flex;
  flex-direction: column;
  gap: var(--space-4);
}
</style>
