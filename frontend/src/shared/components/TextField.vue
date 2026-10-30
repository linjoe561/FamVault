<script setup>
import { useId } from 'vue'

// 額外屬性（required、inputmode…）給 input，而不是外層 div
defineOptions({ inheritAttrs: false })

defineProps({
  label: { type: String, required: true },
  type: { type: String, default: 'text' },
  error: { type: String, default: '' },
  hint: { type: String, default: '' },
  autocomplete: { type: String, default: undefined },
})

const model = defineModel({ type: String, default: '' })
const id = useId()
</script>

<template>
  <div class="field" :class="{ 'field--error': error }">
    <label class="field__label" :for="id">{{ label }}</label>
    <input
      :id="id"
      v-model="model"
      class="field__input"
      :type="type"
      :autocomplete="autocomplete"
      :aria-invalid="!!error"
      :aria-describedby="error || hint ? `${id}-msg` : undefined"
      v-bind="$attrs"
    />
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

.field__input {
  min-height: var(--touch-target);
  padding: 0 var(--space-3);
  background: var(--color-surface);
  border: 1px solid var(--color-border-strong);
  border-radius: var(--radius-sm);
  font-size: var(--font-size-md); /* 16px 以上，iOS 才不會自動放大畫面 */
}

.field__input:focus {
  border-color: var(--color-primary);
  outline: none;
  box-shadow: 0 0 0 3px var(--color-primary-soft);
}

.field__message {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
}

.field--error .field__input {
  border-color: var(--color-danger);
}

.field--error .field__message {
  color: var(--color-danger);
}
</style>
