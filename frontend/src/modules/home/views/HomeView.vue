<script setup>
import { computed } from 'vue'
import { useAuthStore } from '@/modules/auth/store'

const auth = useAuthStore()

const greeting = computed(() => {
  const hour = new Date().getHours()
  if (hour < 11) return '早安'
  if (hour < 18) return '午安'
  return '晚安'
})

const upcoming = [
  { title: '快到期的東西', text: '依保存期限排好，先到期的先用。' },
  { title: '家裡還有沒有', text: '出門前查一下，不再重複買。' },
  { title: '掃條碼登錄', text: '買回家掃一下就記好了。' },
]
</script>

<template>
  <section class="hero">
    <h1 class="hero__title">{{ greeting }}，{{ auth.user?.displayName }}</h1>
    <p class="hero__text">家裡的庫存功能正在準備中，下面是接下來會出現在這裡的內容。</p>
  </section>

  <ul class="upcoming">
    <li v-for="item in upcoming" :key="item.title" class="upcoming__item card">
      <h2 class="upcoming__title">{{ item.title }}</h2>
      <p class="upcoming__text">{{ item.text }}</p>
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

.upcoming {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
  gap: var(--space-3);
  margin: 0;
  padding: 0;
  list-style: none;
}

.upcoming__item {
  padding: var(--space-4) var(--space-5);
  border-style: dashed;
  box-shadow: none;
}

.upcoming__title {
  font-size: var(--font-size-md);
  margin-bottom: var(--space-1);
}

.upcoming__text {
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
}
</style>
