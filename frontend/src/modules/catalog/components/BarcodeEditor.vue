<script setup>
import { ref, useId } from 'vue'
import AppIcon from '@/shared/components/AppIcon.vue'
import BaseButton from '@/shared/components/BaseButton.vue'

/**
 * 條碼清單編輯。目前用手動輸入；第四階段會在這裡加上「用鏡頭掃描」按鈕。
 */
defineProps({
  error: { type: String, default: '' },
})
const codes = defineModel({ type: Array, default: () => [] })

const id = useId()
const draft = ref('')
const localError = ref('')

function add() {
  const code = draft.value.trim().toUpperCase()
  localError.value = ''
  if (!code) return
  if (!/^[0-9A-Z-]+$/.test(code)) {
    localError.value = '條碼只能包含英文字母、數字與 -'
    return
  }
  if (codes.value.includes(code)) {
    localError.value = '這組條碼已經在清單中'
    return
  }
  codes.value = [...codes.value, code]
  draft.value = ''
}

function remove(code) {
  codes.value = codes.value.filter((c) => c !== code)
}
</script>

<template>
  <div class="barcodes">
    <label class="barcodes__label" :for="id">條碼</label>
    <ul v-if="codes.length" class="barcodes__list">
      <li v-for="code in codes" :key="code" class="chip">
        <AppIcon name="barcode" :size="16" />
        <span class="chip__code">{{ code }}</span>
        <button type="button" class="chip__remove" :aria-label="`移除條碼 ${code}`" @click="remove(code)">
          <AppIcon name="close" :size="16" />
        </button>
      </li>
    </ul>
    <div class="barcodes__add">
      <input
        :id="id"
        v-model="draft"
        class="barcodes__input"
        inputmode="numeric"
        autocomplete="off"
        placeholder="輸入商品上的條碼數字"
        @keydown.enter.prevent="add"
      />
      <BaseButton variant="secondary" @click="add">加入</BaseButton>
    </div>
    <p class="barcodes__message" :class="{ 'barcodes__message--error': localError || error }">
      {{ localError || error || '同一個品項可以有多組條碼（例如不同包裝）' }}
    </p>
  </div>
</template>

<style scoped>
.barcodes {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
}

.barcodes__label {
  font-size: var(--font-size-sm);
  font-weight: 500;
}

.barcodes__list {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-2);
  margin: 0;
  padding: 0;
  list-style: none;
}

.chip {
  display: inline-flex;
  align-items: center;
  gap: var(--space-1);
  padding-left: var(--space-3);
  background: var(--color-surface-muted);
  border-radius: var(--radius-pill);
  color: var(--color-text-muted);
}

.chip__code {
  color: var(--color-text);
  font-variant-numeric: tabular-nums;
  letter-spacing: 0.02em;
}

.chip__remove {
  display: grid;
  place-items: center;
  width: 36px;
  height: 36px;
  border: none;
  border-radius: 50%;
  background: transparent;
  color: var(--color-text-muted);
  cursor: pointer;
}

.chip__remove:hover {
  color: var(--color-danger);
}

.barcodes__add {
  display: flex;
  gap: var(--space-2);
}

.barcodes__input {
  flex: 1;
  min-width: 0;
  min-height: var(--touch-target);
  padding: 0 var(--space-3);
  background: var(--color-surface);
  border: 1px solid var(--color-border-strong);
  border-radius: var(--radius-sm);
  font-size: var(--font-size-md);
}

.barcodes__input:focus {
  border-color: var(--color-primary);
  outline: none;
  box-shadow: 0 0 0 3px var(--color-primary-soft);
}

.barcodes__message {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
}

.barcodes__message--error {
  color: var(--color-danger);
}
</style>
