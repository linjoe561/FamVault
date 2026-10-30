<script setup>
import { computed } from 'vue'
import AppIcon from '@/shared/components/AppIcon.vue'
import { useAuthStore } from '@/modules/auth/store'

const auth = useAuthStore()

const greeting = computed(() => {
  const hour = new Date().getHours()
  if (hour < 11) return '早安'
  if (hour < 18) return '午安'
  return '晚安'
})

// 第五階段會換成到期提醒、需要補貨等儀表板資訊
const shortcuts = [
  { to: { name: 'scan' }, icon: 'scan', title: '掃條碼', text: '拿起東西掃一下，馬上登錄或記錄用掉' },
  { to: { name: 'stock' }, icon: 'jar', title: '家裡還有沒有', text: '查數量、放哪裡、什麼時候到期' },
  { to: { name: 'stock', query: { status: 'expiring' } }, icon: 'history', title: '快過期的東西', text: '已過期和 7 天內到期的，先到期的先用' },
  { to: { name: 'purchase-new' }, icon: 'cart', title: '買東西回來了', text: '登錄一下，庫存自動增加' },
]
</script>

<template>
  <section class="hero">
    <h1 class="hero__title">{{ greeting }}，{{ auth.user?.displayName }}</h1>
    <p class="hero__text">今天要做什麼呢？</p>
  </section>

  <ul class="shortcuts">
    <li v-for="item in shortcuts" :key="item.title">
      <RouterLink :to="item.to" class="shortcut card">
        <span class="shortcut__icon"><AppIcon :name="item.icon" /></span>
        <span>
          <span class="shortcut__title">{{ item.title }}</span>
          <span class="shortcut__text">{{ item.text }}</span>
        </span>
      </RouterLink>
    </li>
  </ul>
</template>

<style scoped>
.hero {
  margin-bottom: var(--space-5);
}

.hero__title {
  font-size: var(--font-size-xl);
}

.hero__text {
  margin-top: var(--space-2);
  color: var(--color-text-muted);
}

.shortcuts {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
  gap: var(--space-3);
  margin: 0;
  padding: 0;
  list-style: none;
}

.shortcut {
  display: flex;
  align-items: center;
  gap: var(--space-4);
  height: 100%;
  padding: var(--space-4) var(--space-5);
  color: inherit;
  text-decoration: none;
}

.shortcut:hover {
  border-color: var(--color-border-strong);
}

.shortcut__icon {
  display: grid;
  place-items: center;
  flex-shrink: 0;
  width: 44px;
  height: 44px;
  border-radius: 50%;
  background: var(--color-primary-soft);
  color: var(--color-primary);
}

.shortcut__title {
  display: block;
  font-weight: 500;
}

.shortcut__text {
  display: block;
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
}
</style>
