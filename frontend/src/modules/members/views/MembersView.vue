<script setup>
import { onMounted, ref } from 'vue'
import AppIcon from '@/shared/components/AppIcon.vue'
import BaseButton from '@/shared/components/BaseButton.vue'
import FormAlert from '@/shared/components/FormAlert.vue'
import PageHeader from '@/shared/components/PageHeader.vue'
import { useAuthStore } from '@/modules/auth/store'
import MemberDialog from '../components/MemberDialog.vue'
import ResetPasswordDialog from '../components/ResetPasswordDialog.vue'
import { membersApi, roleLabels } from '../api'

const auth = useAuthStore()
const members = ref([])
const loading = ref(true)
const error = ref('')
const notice = ref('')

const editing = ref(null)
const editOpen = ref(false)
const resetting = ref(null)
const resetOpen = ref(false)

async function load() {
  loading.value = true
  error.value = ''
  try {
    members.value = await membersApi.list()
  } catch (e) {
    error.value = e.message
  } finally {
    loading.value = false
  }
}

function openCreate() {
  editing.value = null
  editOpen.value = true
}

function openEdit(member) {
  editing.value = member
  editOpen.value = true
}

function openReset(member) {
  resetting.value = member
  resetOpen.value = true
}

function onSaved(saved) {
  const index = members.value.findIndex((m) => m.id === saved.id)
  if (index >= 0) {
    members.value[index] = saved
  } else {
    members.value.push(saved)
  }
  if (saved.id === auth.user?.id) {
    auth.updateUser({ displayName: saved.displayName, role: saved.role })
  }
  notice.value = `已儲存「${saved.displayName}」`
}

onMounted(load)
</script>

<template>
  <PageHeader title="家庭成員" subtitle="每位家人用自己的帳號登入，系統會記錄是誰用掉了什麼">
    <template #actions>
      <BaseButton @click="openCreate">
        <AppIcon name="plus" :size="18" />
        新增
      </BaseButton>
    </template>
  </PageHeader>

  <FormAlert :message="error" />
  <FormAlert :message="notice" tone="success" />

  <p v-if="loading" class="muted">載入中…</p>

  <ul v-else class="members">
    <li v-for="member in members" :key="member.id" class="member card" :class="{ 'member--inactive': !member.isActive }">
      <span class="member__avatar">{{ member.displayName.slice(0, 1) }}</span>
      <div class="member__info">
        <p class="member__name">
          {{ member.displayName }}
          <span v-if="member.id === auth.user?.id" class="tag">你</span>
        </p>
        <p class="member__meta">
          {{ member.userName }} · {{ roleLabels[member.role] }}
          <span v-if="!member.isActive"> · 已停用</span>
        </p>
      </div>
      <div class="member__actions">
        <BaseButton variant="ghost" :aria-label="`編輯 ${member.displayName}`" @click="openEdit(member)">
          <AppIcon name="edit" :size="18" />
        </BaseButton>
        <BaseButton variant="ghost" :aria-label="`重設 ${member.displayName} 的密碼`" @click="openReset(member)">
          <AppIcon name="key" :size="18" />
        </BaseButton>
      </div>
    </li>
  </ul>

  <MemberDialog v-model:open="editOpen" :member="editing" @saved="onSaved" />
  <ResetPasswordDialog
    v-model:open="resetOpen"
    :member="resetting"
    @done="(m) => (notice = `已重設「${m.displayName}」的密碼`)"
  />
</template>

<style scoped>
.muted {
  color: var(--color-text-muted);
}

.members {
  display: grid;
  gap: var(--space-3);
  margin: var(--space-3) 0 0;
  padding: 0;
  list-style: none;
}

.member {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  padding: var(--space-3) var(--space-3) var(--space-3) var(--space-4);
}

.member--inactive {
  opacity: 0.6;
}

.member__avatar {
  display: grid;
  place-items: center;
  flex-shrink: 0;
  width: 44px;
  height: 44px;
  border-radius: 50%;
  background: var(--color-accent-soft);
  color: var(--color-accent);
  font-family: var(--font-heading);
  font-weight: 700;
}

.member__info {
  flex: 1;
  min-width: 0;
}

.member__name {
  font-weight: 500;
}

.member__meta {
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.member__actions {
  display: flex;
}

.tag {
  margin-left: var(--space-1);
  padding: 0 var(--space-2);
  border-radius: var(--radius-pill);
  background: var(--color-primary-soft);
  color: var(--color-primary-hover);
  font-size: var(--font-size-xs);
}
</style>
