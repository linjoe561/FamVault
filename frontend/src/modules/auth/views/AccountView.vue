<script setup>
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import AppIcon from '@/shared/components/AppIcon.vue'
import BaseButton from '@/shared/components/BaseButton.vue'
import FormAlert from '@/shared/components/FormAlert.vue'
import PageHeader from '@/shared/components/PageHeader.vue'
import TextField from '@/shared/components/TextField.vue'
import { authApi } from '../api'
import { useAuthStore } from '../store'

const auth = useAuthStore()
const router = useRouter()

const form = reactive({ currentPassword: '', newPassword: '', confirmPassword: '' })
const fieldErrors = ref({})
const error = ref('')
const success = ref('')
const submitting = ref(false)

async function changePassword() {
  error.value = ''
  success.value = ''
  fieldErrors.value = {}
  if (form.newPassword !== form.confirmPassword) {
    fieldErrors.value = { confirmPassword: '兩次輸入的密碼不一樣' }
    return
  }

  submitting.value = true
  try {
    await authApi.changePassword({ currentPassword: form.currentPassword, newPassword: form.newPassword })
    Object.assign(form, { currentPassword: '', newPassword: '', confirmPassword: '' })
    success.value = '密碼已更新'
  } catch (e) {
    error.value = e.message
    fieldErrors.value = e.fieldErrors ?? {}
  } finally {
    submitting.value = false
  }
}

async function logout() {
  await auth.logout()
  router.replace({ name: 'login' })
}
</script>

<template>
  <PageHeader title="我的帳號" />

  <section class="card profile">
    <span class="profile__avatar"><AppIcon name="user" :size="28" /></span>
    <div>
      <p class="profile__name">{{ auth.user?.displayName }}</p>
      <p class="profile__meta">
        帳號 {{ auth.user?.userName }} · {{ auth.isAdmin ? '管理者' : '成員' }}
      </p>
    </div>
  </section>

  <nav class="card links" aria-label="更多功能">
    <RouterLink :to="{ name: 'items' }" class="links__item">
      <AppIcon name="box" :size="20" />
      品項
      <AppIcon name="chevron" :size="18" class="links__chevron" />
    </RouterLink>
    <RouterLink :to="{ name: 'purchases' }" class="links__item">
      <AppIcon name="history" :size="20" />
      購買紀錄
      <AppIcon name="chevron" :size="18" class="links__chevron" />
    </RouterLink>
  </nav>

  <nav v-if="auth.isAdmin" class="card links" aria-label="管理">
    <RouterLink :to="{ name: 'members' }" class="links__item">
      <AppIcon name="users" :size="20" />
      家庭成員
      <AppIcon name="chevron" :size="18" class="links__chevron" />
    </RouterLink>
    <RouterLink :to="{ name: 'catalog-settings' }" class="links__item">
      <AppIcon name="settings" :size="20" />
      分類、位置與單位
      <AppIcon name="chevron" :size="18" class="links__chevron" />
    </RouterLink>
  </nav>

  <section class="card panel">
    <h2 class="panel__title">變更密碼</h2>
    <form class="form" novalidate @submit.prevent="changePassword">
      <FormAlert :message="error" />
      <FormAlert :message="success" tone="success" />
      <TextField
        v-model="form.currentPassword"
        label="目前密碼"
        type="password"
        autocomplete="current-password"
        :error="fieldErrors.currentPassword"
      />
      <TextField
        v-model="form.newPassword"
        label="新密碼"
        type="password"
        hint="至少 8 個字元"
        autocomplete="new-password"
        :error="fieldErrors.newPassword"
      />
      <TextField
        v-model="form.confirmPassword"
        label="再輸入一次新密碼"
        type="password"
        autocomplete="new-password"
        :error="fieldErrors.confirmPassword"
      />
      <div>
        <BaseButton type="submit" :loading="submitting">更新密碼</BaseButton>
      </div>
    </form>
  </section>

  <BaseButton variant="secondary" block @click="logout">
    <AppIcon name="logout" :size="18" />
    登出
  </BaseButton>
</template>

<style scoped>
.profile {
  display: flex;
  align-items: center;
  gap: var(--space-4);
  padding: var(--space-4) var(--space-5);
  margin-bottom: var(--space-4);
}

.profile__avatar {
  display: grid;
  place-items: center;
  width: 52px;
  height: 52px;
  border-radius: 50%;
  background: var(--color-accent-soft);
  color: var(--color-accent);
}

.profile__name {
  font-weight: 700;
  font-size: var(--font-size-lg);
}

.profile__meta {
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
}

.links {
  display: flex;
  flex-direction: column;
  margin-bottom: var(--space-4);
}

.links__item {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  min-height: 56px;
  padding: 0 var(--space-4) 0 var(--space-5);
  color: var(--color-text);
  text-decoration: none;
}

.links__item + .links__item {
  border-top: 1px solid var(--color-border);
}

.links__chevron {
  margin-left: auto;
  color: var(--color-border-strong);
}

.panel {
  padding: var(--space-5);
  margin-bottom: var(--space-5);
}

.panel__title {
  font-size: var(--font-size-lg);
  margin-bottom: var(--space-4);
}

.form {
  display: flex;
  flex-direction: column;
  gap: var(--space-4);
  max-width: 420px;
}
</style>
