<script setup>
import { computed } from 'vue'
import AppIcon from '@/shared/components/AppIcon.vue'
import { useAuthStore } from '@/modules/auth/store'
import { navItems } from '../modules'

const auth = useAuthStore()
const items = computed(() => navItems.filter((item) => !item.adminOnly || auth.isAdmin))
</script>

<template>
  <div class="shell">
    <!-- 桌機：左側選單 -->
    <aside class="sidebar">
      <div class="sidebar__brand">
        <span class="sidebar__logo"><AppIcon name="leaf" /></span>
        家中庫存
      </div>
      <nav aria-label="主選單">
        <RouterLink v-for="item in items" :key="item.label" :to="item.to" class="sidebar__link">
          <AppIcon :name="item.icon" />
          {{ item.label }}
        </RouterLink>
      </nav>
      <p class="sidebar__user">{{ auth.user?.displayName }}</p>
    </aside>

    <main class="content">
      <div class="content__inner">
        <slot />
      </div>
    </main>

    <!-- 手機：底部分頁列 -->
    <nav class="tabbar" aria-label="主選單">
      <RouterLink v-for="item in items" :key="item.label" :to="item.to" class="tabbar__link">
        <AppIcon :name="item.icon" />
        <span>{{ item.label }}</span>
      </RouterLink>
    </nav>
  </div>
</template>

<style scoped>
.shell {
  min-height: 100dvh;
}

.content {
  padding: var(--space-5) var(--space-4) calc(var(--tabbar-height) + var(--space-5) + env(safe-area-inset-bottom));
}

.content__inner {
  max-width: var(--content-max-width);
  margin: 0 auto;
}

.sidebar {
  display: none;
}

.tabbar {
  position: fixed;
  inset: auto 0 0;
  z-index: 10;
  display: flex;
  height: calc(var(--tabbar-height) + env(safe-area-inset-bottom));
  padding-bottom: env(safe-area-inset-bottom);
  background: var(--color-surface);
  border-top: 1px solid var(--color-border);
}

.tabbar__link {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 2px;
  color: var(--color-text-muted);
  font-size: var(--font-size-xs);
  text-decoration: none;
}

.tabbar__link.router-link-exact-active {
  color: var(--color-primary);
  font-weight: 500;
}

@media (min-width: 900px) {
  .shell {
    display: grid;
    grid-template-columns: var(--sidebar-width) 1fr;
  }

  .tabbar {
    display: none;
  }

  .content {
    padding: var(--space-6) var(--space-6) var(--space-7);
  }

  .sidebar {
    position: sticky;
    top: 0;
    height: 100dvh;
    display: flex;
    flex-direction: column;
    gap: var(--space-5);
    padding: var(--space-5) var(--space-4);
    background: var(--color-surface-muted);
    border-right: 1px solid var(--color-border);
  }

  .sidebar__brand {
    display: flex;
    align-items: center;
    gap: var(--space-2);
    padding: 0 var(--space-2);
    font-family: var(--font-heading);
    font-weight: 700;
    font-size: var(--font-size-lg);
    color: var(--color-primary);
  }

  .sidebar__logo {
    display: grid;
    place-items: center;
    width: 36px;
    height: 36px;
    border-radius: var(--radius-sm);
    background: var(--color-primary-soft);
  }

  nav {
    display: flex;
    flex-direction: column;
    gap: var(--space-1);
  }

  .sidebar__link {
    display: flex;
    align-items: center;
    gap: var(--space-3);
    min-height: var(--touch-target);
    padding: 0 var(--space-3);
    border-radius: var(--radius-sm);
    color: var(--color-text);
    text-decoration: none;
  }

  .sidebar__link:hover {
    background: var(--color-surface);
  }

  .sidebar__link.router-link-exact-active {
    background: var(--color-surface);
    color: var(--color-primary);
    font-weight: 500;
    box-shadow: var(--shadow-card);
  }

  .sidebar__user {
    margin-top: auto;
    padding: 0 var(--space-3);
    color: var(--color-text-muted);
    font-size: var(--font-size-sm);
  }
}
</style>
