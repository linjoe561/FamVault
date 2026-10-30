<script setup>
import { useId } from 'vue'

// 使用原生 select：手機上會打開系統的選擇器，最好操作
defineOptions({ inheritAttrs: false })

defineProps({
  label: { type: String, required: true },
  error: { type: String, default: '' },
  hint: { type: String, default: '' },
  placeholder: { type: String, default: '' }, // 有值時多一個「未選擇」選項（值為 null）
})

const model = defineModel({ type: [Number, String, null], default: null })
const id = useId()
</script>

<template>
  <div class="field" :class="{ 'field--error': error }">
    <label class="field__label" :for="id">{{ label }}</label>
    <select
      :id="id"
      v-model="model"
      class="field__select"
      :aria-invalid="!!error"
      :aria-describedby="error || hint ? `${id}-msg` : undefined"
      v-bind="$attrs"
    >
      <option v-if="placeholder" :value="null">{{ placeholder }}</option>
      <slot />
    </select>
    <p v-if="error || hint" :id="`${id}-msg`" class="field__message">{{ error || hint }}</p>
  </div>
</template>

<style scoped>
.field {
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
}

.field__label {
  font-size: var(--font-size-sm);
  font-weight: 500;
}

.field__select {
  min-height: var(--touch-target);
  padding: 0 var(--space-3);
  background: var(--color-surface);
  border: 1px solid var(--color-border-strong);
  border-radius: var(--radius-sm);
  font-size: var(--font-size-md);
}

.field__select:focus {
  border-color: var(--color-primary);
  outline: none;
  box-shadow: 0 0 0 3px var(--color-primary-soft);
}

.field__message {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
}

.field--error .field__select {
  border-color: var(--color-danger);
}

.field--error .field__message {
  color: var(--color-danger);
}
</style>
