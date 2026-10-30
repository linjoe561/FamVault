<script setup>
import { computed, reactive, ref, watch } from 'vue'
import AppDialog from '@/shared/components/AppDialog.vue'
import BaseButton from '@/shared/components/BaseButton.vue'
import BaseSelect from '@/shared/components/BaseSelect.vue'
import FormAlert from '@/shared/components/FormAlert.vue'
import TextField from '@/shared/components/TextField.vue'

/**
 * 新增／編輯分類、位置或單位的共用對話框。
 * kind 決定額外欄位：分類有「上層分類」，位置有「說明」。
 */
const props = defineProps({
  kind: { type: String, required: true }, // category | location | unit
  label: { type: String, required: true }, // 顯示用：分類／位置／單位
  record: { type: Object, default: null }, // null 代表新增
  parentOptions: { type: Array, default: () => [] }, // 分類用：可選的大分類
  save: { type: Function, required: true }, // (form) => Promise
})
const open = defineModel('open', { type: Boolean, default: false })

const isEdit = computed(() => !!props.record)
const form = reactive({ name: '', parentId: null, description: '' })
const fieldErrors = ref({})
const error = ref('')
const saving = ref(false)

watch(open, (isOpen) => {
  if (!isOpen) return
  Object.assign(form, {
    name: props.record?.name ?? '',
    parentId: props.record?.parentId ?? null,
    description: props.record?.description ?? '',
  })
  fieldErrors.value = {}
  error.value = ''
})

async function submit() {
  saving.value = true
  error.value = ''
  fieldErrors.value = {}
  const payload = { name: form.name }
  if (props.kind === 'category') payload.parentId = form.parentId
  if (props.kind === 'location') payload.description = form.description
  try {
    await props.save(payload)
    open.value = false
  } catch (e) {
    error.value = e.message
    fieldErrors.value = e.fieldErrors ?? {}
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <AppDialog v-model:open="open" :title="`${isEdit ? '編輯' : '新增'}${label}`">
    <form class="form" novalidate @submit.prevent="submit">
      <FormAlert :message="error" />
      <TextField v-model="form.name" label="名稱" :error="fieldErrors.name" />
      <BaseSelect
        v-if="kind === 'category'"
        v-model="form.parentId"
        label="放在哪個大分類底下"
        placeholder="不放（它本身就是大分類）"
        :error="fieldErrors.parentId"
      >
        <option v-for="option in parentOptions" :key="option.id" :value="option.id">{{ option.name }}</option>
      </BaseSelect>
      <TextField
        v-if="kind === 'location'"
        v-model="form.description"
        label="說明"
        hint="可留空，例如：廚房右邊的上櫃"
        :error="fieldErrors.description"
      />
      <div class="actions">
        <BaseButton variant="ghost" @click="open = false">取消</BaseButton>
        <BaseButton type="submit" :loading="saving">{{ isEdit ? '儲存' : '新增' }}</BaseButton>
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

.actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--space-2);
}
</style>
