<script setup>
import { computed } from 'vue'
import { describeExpiry } from '@/shared/utils/format'

/**
 * 保存期限標籤。狀態由後端計算（expired / soon / upcoming / ok / none）。
 * 不只靠顏色：每種狀態有不同的文字與記號，色弱的家人也分得出來。
 */
const props = defineProps({
  date: { type: String, default: null },
  status: { type: String, required: true },
})

const marks = { expired: '✕', soon: '!', upcoming: '•', ok: '', none: '' }
const label = computed(() => describeExpiry(props.date))
</script>

<template>
  <span class="expiry" :class="`expiry--${status}`">
    <span v-if="marks[status]" class="expiry__mark" aria-hidden="true">{{ marks[status] }}</span>
    {{ label }}
  </span>
</template>

<style scoped>
.expiry {
  display: inline-flex;
  align-items: center;
  gap: var(--space-1);
  padding: 0 var(--space-2);
  border-radius: var(--radius-pill);
  font-size: var(--font-size-xs);
  line-height: 1.8;
  white-space: nowrap;
}

.expiry__mark {
  font-weight: 700;
}

.expiry--expired {
  background: var(--color-danger-soft);
  color: var(--color-danger);
  font-weight: 500;
}

.expiry--soon {
  background: var(--color-warning-soft);
  color: var(--color-warning);
  font-weight: 500;
}

.expiry--upcoming {
  background: var(--color-notice-soft);
  color: var(--color-notice);
}

.expiry--ok,
.expiry--none {
  background: var(--color-surface-muted);
  color: var(--color-text-muted);
}
</style>
