<script setup>
import {
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogOverlay,
  DialogPortal,
  DialogRoot,
  DialogTitle,
} from 'reka-ui'
import AppIcon from './AppIcon.vue'

defineProps({
  title: { type: String, required: true },
  description: { type: String, default: '' },
})

const open = defineModel('open', { type: Boolean, default: false })
</script>

<template>
  <DialogRoot v-model:open="open">
    <DialogPortal>
      <DialogOverlay class="dialog-overlay" />
      <DialogContent class="dialog" v-bind="description ? {} : { 'aria-describedby': undefined }">
        <header class="dialog__header">
          <DialogTitle class="dialog__title">{{ title }}</DialogTitle>
          <DialogClose class="dialog__close" aria-label="關閉">
            <AppIcon name="close" />
          </DialogClose>
        </header>
        <DialogDescription v-if="description" class="dialog__description">{{ description }}</DialogDescription>
        <slot />
      </DialogContent>
    </DialogPortal>
  </DialogRoot>
</template>

<style>
.dialog-overlay {
  position: fixed;
  inset: 0;
  background: rgb(59 50 41 / 35%);
  z-index: 50;
}

.dialog {
  position: fixed;
  z-index: 51;
  left: 50%;
  top: 50%;
  transform: translate(-50%, -50%);
  width: min(440px, calc(100vw - 32px));
  max-height: calc(100dvh - 32px);
  overflow-y: auto;
  padding: var(--space-5);
  background: var(--color-surface);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-overlay);
}

/* 手機：從底部滑出的面板，比較好用單手操作 */
@media (max-width: 599px) {
  .dialog {
    top: auto;
    bottom: 0;
    transform: translateX(-50%);
    width: 100vw;
    border-radius: var(--radius-lg) var(--radius-lg) 0 0;
    padding-bottom: calc(var(--space-5) + env(safe-area-inset-bottom));
  }
}

.dialog__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-3);
  margin-bottom: var(--space-4);
}

.dialog__title {
  font-size: var(--font-size-lg);
}

.dialog__description {
  margin: calc(-1 * var(--space-2)) 0 var(--space-4);
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
}

.dialog__close {
  display: grid;
  place-items: center;
  width: var(--touch-target);
  height: var(--touch-target);
  border: none;
  border-radius: 50%;
  background: transparent;
  color: var(--color-text-muted);
  cursor: pointer;
}

.dialog__close:hover {
  background: var(--color-surface-muted);
}
</style>
