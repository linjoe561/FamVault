<script setup>
import { ref, watch } from 'vue'
import AppDialog from '@/shared/components/AppDialog.vue'
import BaseButton from '@/shared/components/BaseButton.vue'
import FormAlert from '@/shared/components/FormAlert.vue'
import TextField from '@/shared/components/TextField.vue'
import { membersApi } from '../api'

const props = defineProps({
  member: { type: Object, default: null },
})
const emit = defineEmits(['done'])
const open = defineModel('open', { type: Boolean, default: false })

const newPassword = ref('')
const fieldErrors = ref({})
const error = ref('')
const submitting = ref(false)

watch(open, (isOpen) => {
  if (isOpen) {
    newPassword.value = ''
    fieldErrors.value = {}
    error.value = ''
  }
})

async function submit() {
  submitting.value = true
  error.value = ''
  fieldErrors.value = {}
  try {
    await membersApi.resetPassword(props.member.id, newPassword.value)
    emit('done', props.member)
    open.value = false
  } catch (e) {
    error.value = e.message
    fieldErrors.value = e.fieldErrors ?? {}
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <AppDialog
    v-model:open="open"
    title="重設密碼"
    :description="`為「${member?.displayName ?? ''}」設定新密碼，對方在其他裝置上的登入會被登出。`"
  >
    <form class="form" novalidate @submit.prevent="submit">
      <FormAlert :message="error" />
      <TextField
        v-model="newPassword"
        label="新密碼"
        type="password"
        hint="至少 8 個字元"
        autocomplete="new-password"
        :error="fieldErrors.newPassword"
      />
      <div class="form__actions">
        <BaseButton variant="ghost" @click="open = false">取消</BaseButton>
        <BaseButton type="submit" :loading="submitting">重設</BaseButton>
      </div>
    </form>
  </AppDialog>
</template>

<style scoped>
.form {
  display: flex;
  flex-direction: column;
  gap: var(--space-4);
}

.form__actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--space-2);
}
</style>
