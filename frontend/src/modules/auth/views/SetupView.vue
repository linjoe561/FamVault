<script setup>
import { onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import BaseButton from '@/shared/components/BaseButton.vue'
import FormAlert from '@/shared/components/FormAlert.vue'
import TextField from '@/shared/components/TextField.vue'
import AuthCard from '../components/AuthCard.vue'
import { authApi } from '../api'
import { useAuthStore } from '../store'

const auth = useAuthStore()
const router = useRouter()

const form = reactive({ displayName: '', userName: '', password: '', confirmPassword: '' })
const fieldErrors = ref({})
const error = ref('')
const submitting = ref(false)

onMounted(async () => {
  try {
    const { needsSetup } = await authApi.getSetupStatus()
    if (!needsSetup) {
      router.replace({ name: 'login' })
    }
  } catch (e) {
    error.value = e.message
  }
})

async function submit() {
  error.value = ''
  fieldErrors.value = {}
  if (form.password !== form.confirmPassword) {
    fieldErrors.value = { confirmPassword: '兩次輸入的密碼不一樣' }
    return
  }

  submitting.value = true
  try {
    await auth.setup({ displayName: form.displayName, userName: form.userName, password: form.password })
    router.replace({ name: 'home' })
  } catch (e) {
    error.value = e.message
    fieldErrors.value = e.fieldErrors ?? {}
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <AuthCard title="第一次使用" subtitle="先建立管理者帳號。之後可以在「家庭成員」新增其他家人。">
    <form class="form" novalidate @submit.prevent="submit">
      <FormAlert :message="error" />
      <TextField
        v-model="form.displayName"
        label="顯示名稱"
        hint="家人看到的名字，例如：媽媽"
        :error="fieldErrors.displayName"
      />
      <TextField
        v-model="form.userName"
        label="帳號"
        hint="登入用，英文字母、數字與 . _ -"
        autocomplete="username"
        autocapitalize="none"
        :error="fieldErrors.userName"
      />
      <TextField
        v-model="form.password"
        label="密碼"
        type="password"
        hint="至少 8 個字元"
        autocomplete="new-password"
        :error="fieldErrors.password"
      />
      <TextField
        v-model="form.confirmPassword"
        label="再輸入一次密碼"
        type="password"
        autocomplete="new-password"
        :error="fieldErrors.confirmPassword"
      />
      <BaseButton type="submit" block :loading="submitting">建立並開始使用</BaseButton>
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
