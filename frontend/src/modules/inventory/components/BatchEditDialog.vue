<script setup>
import { ref, watch } from 'vue'
import AppDialog from '@/shared/components/AppDialog.vue'
import BaseButton from '@/shared/components/BaseButton.vue'
import BaseSelect from '@/shared/components/BaseSelect.vue'
import FormAlert from '@/shared/components/FormAlert.vue'
import TextField from '@/shared/components/TextField.vue'
import { formatQuantity } from '@/shared/utils/format'
import { useCatalogStore } from '@/modules/catalog/store'
import { stockApi } from '../api'

/** 盤點更正數量、移到別的位置、更正到期日。 */
const props = defineProps({
  batch: { type: Object, default: null },
  unitName: { type: String, required: true },
})
const emit = defineEmits(['done'])
const open = defineModel('open', { type: Boolean, default: false })

const catalog = useCatalogStore()
const form = ref({ quantity: '', locationId: null, expiryDate: '', note: '' })
const fieldErrors = ref({})
const error = ref('')
const saving = ref(false)

watch(open, (isOpen) => {
  if (!isOpen || !props.batch) return
  form.value = {
    quantity: formatQuantity(props.batch.quantity),
    locationId: props.batch.locationId,
    expiryDate: props.batch.expiryDate ?? '',
    note: '',
  }
  fieldErrors.value = {}
  error.value = ''
  catalog.load().catch((e) => (error.value = e.message))
})

async function submit() {
  saving.value = true
  error.value = ''
  fieldErrors.value = {}
  try {
    const detail = await stockApi.updateBatch(props.batch.id, {
      quantity: Number(form.value.quantity),
      locationId: form.value.locationId,
      expiryDate: form.value.expiryDate || null,
      note: form.value.note,
    })
    emit('done', detail)
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
  <AppDialog v-model:open="open" title="修改這一批" description="數量有變動時會留下「調整」紀錄">
    <form class="form" novalidate @submit.prevent="submit">
      <FormAlert :message="error" />
      <TextField
        v-model="form.quantity"
        :label="`實際剩下的數量（${unitName}）`"
        inputmode="decimal"
        :error="fieldErrors.quantity"
      />
      <BaseSelect v-model="form.locationId" label="存放位置" placeholder="不指定" :error="fieldErrors.locationId">
        <option v-for="location in catalog.locations" :key="location.id" :value="location.id">{{ location.name }}</option>
      </BaseSelect>
      <TextField v-model="form.expiryDate" label="到期日" type="date" hint="沒有期限可留空" :error="fieldErrors.expiryDate" />
      <TextField v-model="form.note" label="備註" hint="可留空，例如：盤點更正" :error="fieldErrors.note" />
      <div class="actions">
        <BaseButton variant="ghost" @click="open = false">取消</BaseButton>
        <BaseButton type="submit" :loading="saving">儲存</BaseButton>
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
