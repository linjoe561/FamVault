<script setup>
import AppIcon from '@/shared/components/AppIcon.vue'

defineProps({
  title: { type: String, required: true },
  subtitle: { type: String, default: '' },
})
</script>

<template>
  <main class="auth">
    <section class="auth__card card">
      <div class="auth__brand">
        <span class="auth__logo"><AppIcon name="leaf" :size="26" /></span>
        <span class="auth__brand-name">FamVault</span>
      </div>
      <h1 class="auth__title">{{ title }}</h1>
      <p v-if="subtitle" class="auth__subtitle">{{ subtitle }}</p>
      <slot />
    </section>
  </main>
</template>

<style scoped>
.auth {
  min-height: 100dvh;
  display: grid;
  place-items: center;
  padding: var(--space-5) var(--space-4);
}

.auth__card {
  width: min(400px, 100%);
  padding: var(--space-6) var(--space-5);
}

.auth__brand {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  margin-bottom: var(--space-5);
  color: var(--color-primary);
}

.auth__logo {
  display: grid;
  place-items: center;
  width: 40px;
  height: 40px;
  border-radius: var(--radius-sm);
  background: var(--color-primary-soft);
}

.auth__brand-name {
  font-family: var(--font-heading);
  font-weight: 700;
  font-size: var(--font-size-lg);
}

.auth__title {
  font-size: var(--font-size-xl);
}

.auth__subtitle {
  margin-top: var(--space-2);
  margin-bottom: var(--space-5);
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
}
</style>
