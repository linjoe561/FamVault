<script setup>
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import AppLayout from '@/app/layouts/AppLayout.vue'

const route = useRoute()
const useAppLayout = computed(() => route.meta.layout !== 'blank')
</script>

<template>
  <AppLayout v-if="useAppLayout">
    <RouterView />
  </AppLayout>
  <RouterView v-else />
</template>
