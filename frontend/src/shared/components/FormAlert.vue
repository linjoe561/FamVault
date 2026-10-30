<script setup>
defineProps({
  message: { type: String, default: '' },
  tone: { type: String, default: 'error' }, // error | success
})
</script>

<template>
  <p v-if="message" class="alert" :class="`alert--${tone}`" :role="tone === 'error' ? 'alert' : 'status'">
    {{ message }}
  </p>
</template>

<style scoped>
.alert {
  padding: var(--space-3) var(--space-4);
  border-radius: var(--radius-sm);
  font-size: var(--font-size-sm);
}

.alert--error {
  background: var(--color-danger-soft);
  color: var(--color-danger);
}

.alert--success {
  background: var(--color-success-soft);
  color: var(--color-success);
}
</style>
