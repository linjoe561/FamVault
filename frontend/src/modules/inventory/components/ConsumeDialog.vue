<script setup>
import { computed, ref, watch } from 'vue'
import AppDialog from '@/shared/components/AppDialog.vue'
import BaseButton from '@/shared/components/BaseButton.vue'
import FormAlert from '@/shared/components/FormAlert.vue'
import TextField from '@/shared/components/TextField.vue'
import { formatQuantity } from '@/shared/utils/format'
import { stockApi } from '../api'

/**
 * 用掉或丟棄。沒有指定 batch 時，後端會從最早到期的那批開始扣。
 */
const props = defineProps({
  item: { type: Object, required: true }, // { itemId, name, unitName, quantity }
  batch: { type: Object, default: null }, // 指定某一批時傳入
  initialKind: { type: String, default: 'consume' },
})
const emit = defineEmits(['done'])
const open = defineModel('open', { type: Boolean, default: false })

const kind = ref('consume')
const quantity = ref('1')
const note = ref('')
const fieldErrors = ref({})
const error = ref('')
const saving = ref(false)

const available = computed(() => (props.batch ? props.batch.quantity : props.item.quantity))
const description = computed(() =>
  props.batch
    ? `這一批還有 ${formatQuantity(available.value)} ${props.item.unitName}`
    : `家裡共有 ${formatQuantity(available.value)} ${props.item.unitName}，會從最早到期的開始扣`,
)

watch(open, (isOpen) => {
  if (!isOpen) return
  kind.value = props.initialKind
  quantity.value = String(Math.min(1, Number(available.value)) || formatQuantity(available.value))
  note.value = ''
  fieldErrors.value = {}
  error.value = ''
})

function setAll() {
  quantity.value = formatQuantity(available.value)
}

async function submit() {
  saving.value = true
  error.value = ''
  fieldErrors.value = {}
  try {
    const detail = await stockApi.consume({
      itemId: props.item.itemId,
      quantity: Number(quantity.value),
      kind: kind.value,
      batchId: props.batch?.id ?? null,
      note: note.value,
    })
    emit('done', detail, { kind: kind.value, quantity: Number(quantity.value) })
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
  <AppDialog v-model:open="open" :title="item.name" :description="description">
    <form class="form" novalidate @submit.prevent="submit">
      <FormAlert :message="error" />

      <fieldset class="choice">
        <legend class="visually-hidden">類型</legend>
        <label class="choice__option">
          <input v-model="kind" type="radio" value="consume" />
          <span>用掉了</span>
        </label>
        <label class="choice__option choice__option--discard">
          <input v-model="kind" type="radio" value="discard" />
          <span>丟掉了（過期、壞掉）</span>
        </label>
      </fieldset>

      <div class="quantity">
        <TextField
          v-model="quantity"
          :label="`數量（${item.unitName}）`"
          inputmode="decimal"
          :error="fieldErrors.quantity"
          class="quantity__field"
        />
        <BaseButton variant="secondary" class="quantity__all" @click="setAll">全部</BaseButton>
      </div>

      <TextField v-model="note" label="備註" hint="可留空，例如：發霉了" :error="fieldErrors.note" />

      <div class="actions">
        <BaseButton variant="ghost" @click="open = false">取消</BaseButton>
        <BaseButton type="submit" :variant="kind === 'discard' ? 'danger' : 'primary'" :loading="saving">
          {{ kind === 'discard' ? '記錄丟棄' : '記錄用掉' }}
        </BaseButton>
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

.choice {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--space-2);
  margin: 0;
  padding: 0;
  border: none;
}

.choice__option {
  position: relative;
}

.choice__option input {
  position: absolute;
  opacity: 0;
}

.choice__option span {
  display: grid;
  place-items: center;
  min-height: var(--touch-target);
  padding: var(--space-2);
  border: 1px solid var(--color-border-strong);
  border-radius: var(--radius-sm);
  text-align: center;
  font-size: var(--font-size-sm);
  cursor: pointer;
}

.choice__option input:checked + span {
  border-color: var(--color-primary);
  background: var(--color-primary-soft);
  color: var(--color-primary-hover);
  font-weight: 500;
}

.choice__option--discard input:checked + span {
  border-color: var(--color-danger);
  background: var(--color-danger-soft);
  color: var(--color-danger);
}

.choice__option input:focus-visible + span {
  outline: 2px solid var(--color-accent);
  outline-offset: 2px;
}

.quantity {
  display: flex;
  align-items: flex-start;
  gap: var(--space-2);
}

.quantity__field {
  flex: 1;
}

.quantity__all {
  margin-top: 26px;
}

.actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--space-2);
}
</style>
