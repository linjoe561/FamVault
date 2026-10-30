<script setup>
import { ref, watch } from 'vue'
import AppDialog from './AppDialog.vue'
import BaseButton from './BaseButton.vue'
import FormAlert from './FormAlert.vue'

/**
 * 確認對話框。onConfirm 回傳 Promise；失敗時錯誤訊息顯示在對話框內，不會關閉。
 */
const props = defineProps({
  title: { type: String, required: true },
  message: { type: String, default: '' },
  confirmText: { type: String, default: '確定' },
  danger: { type: Boolean, default: false },
  onConfirm: { type: Function, required: true },
})
const open = defineModel('open', { type: Boolean, default: false })

const busy = ref(false)
const error = ref('')

watch(open, (isOpen) => {
  if (isOpen) error.value = ''
})

async function confirm() {
  busy.value = true
  error.value = ''
  try {
    await props.onConfirm()
    open.value = false
  } catch (e) {
    error.value = e.message
  } finally {
    busy.value = false
  }
}
</script>

<template>
  <AppDialog v-model:open="open" :title="title" :description="message">
    <FormAlert :message="error" />
    <div class="actions">
      <BaseButton variant="ghost" @click="open = false">取消</BaseButton>
      <BaseButton :variant="danger ? 'danger' : 'primary'" :loading="busy" @click="confirm">
        {{ confirmText }}
      </BaseButton>
    </div>
  </AppDialog>
</template>

<style scoped>
.actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--space-2);
  margin-top: var(--space-4);
}
</style>
