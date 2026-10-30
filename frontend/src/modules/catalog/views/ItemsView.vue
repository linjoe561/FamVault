<script setup>
import { onMounted, reactive, ref, watch } from 'vue'
import AppIcon from '@/shared/components/AppIcon.vue'
import BaseButton from '@/shared/components/BaseButton.vue'
import BaseSelect from '@/shared/components/BaseSelect.vue'
import FormAlert from '@/shared/components/FormAlert.vue'
import PageHeader from '@/shared/components/PageHeader.vue'
import ScanDialog from '@/shared/components/ScanDialog.vue'
import { useAuthStore } from '@/modules/auth/store'
import { itemsApi } from '../api'
import { useCatalogStore } from '../store'

const PAGE_SIZE = 30

const auth = useAuthStore()
const catalog = useCatalogStore()

const filters = reactive({ search: '', categoryId: null, includeArchived: false })
const scanOpen = ref(false)
const items = ref([])
const total = ref(0)
const page = ref(1)
const loading = ref(false)
const error = ref('')

async function load({ append = false } = {}) {
  loading.value = true
  error.value = ''
  try {
    const result = await itemsApi.list({
      search: filters.search.trim() || undefined,
      categoryId: filters.categoryId ?? undefined,
      includeArchived: filters.includeArchived || undefined,
      page: page.value,
      pageSize: PAGE_SIZE,
    })
    items.value = append ? [...items.value, ...result.items] : result.items
    total.value = result.total
  } catch (e) {
    error.value = e.message
  } finally {
    loading.value = false
  }
}

function reload() {
  page.value = 1
  load()
}

function loadMore() {
  page.value += 1
  load({ append: true })
}

// 打字時稍等一下再查詢，避免每個字都送出請求
let searchTimer
watch(
  () => filters.search,
  () => {
    clearTimeout(searchTimer)
    searchTimer = setTimeout(reload, 300)
  },
)
watch(() => [filters.categoryId, filters.includeArchived], reload)

function describe(item) {
  return [item.categoryName, item.locationName, `以「${item.unitName}」計`].filter(Boolean).join(' · ')
}

onMounted(() => {
  catalog.load().catch((e) => (error.value = e.message))
  load()
})
</script>

<template>
  <PageHeader title="品項" subtitle="家裡會買的東西，先在這裡建立一次">
    <template #actions>
      <BaseButton @click="$router.push({ name: 'item-new' })">
        <AppIcon name="plus" :size="18" />
        新增
      </BaseButton>
    </template>
  </PageHeader>

  <section class="filters">
    <label class="search">
      <AppIcon name="search" :size="20" />
      <span class="visually-hidden">搜尋品項</span>
      <input v-model="filters.search" type="search" placeholder="搜尋名稱或條碼" enterkeyhint="search" />
      <button type="button" class="search__scan" aria-label="用鏡頭掃條碼搜尋" @click="scanOpen = true">
        <AppIcon name="scan" :size="20" />
      </button>
    </label>
    <ScanDialog v-model:open="scanOpen" @detected="(code) => (filters.search = code)" />
    <div class="filters__row">
      <div class="filters__category">
        <BaseSelect v-model="filters.categoryId" label="分類" placeholder="全部分類">
          <template v-for="parent in catalog.categoryTree" :key="parent.id">
            <option :value="parent.id">{{ parent.name }}</option>
            <option v-for="child in parent.children" :key="child.id" :value="child.id">　└ {{ child.name }}</option>
          </template>
        </BaseSelect>
      </div>
      <label class="toggle">
        <input v-model="filters.includeArchived" type="checkbox" />
        <span>顯示已停用</span>
      </label>
    </div>
    <RouterLink v-if="auth.isAdmin" :to="{ name: 'catalog-settings' }" class="settings-link">
      <AppIcon name="settings" :size="18" />
      管理分類、位置與單位
    </RouterLink>
  </section>

  <FormAlert :message="error" />

  <p v-if="!loading && items.length === 0" class="empty">
    {{ filters.search || filters.categoryId ? '找不到符合的品項' : '還沒有任何品項，按「新增」建立第一個吧' }}
  </p>

  <ul class="items">
    <li v-for="item in items" :key="item.id">
      <RouterLink :to="{ name: 'item-edit', params: { id: item.id } }" class="item card" :class="{ 'item--archived': item.isArchived }">
        <div class="item__body">
          <p class="item__name">
            {{ item.name }}
            <span v-if="item.isArchived" class="tag tag--muted">已停用</span>
          </p>
          <p class="item__meta">{{ describe(item) }}</p>
        </div>
        <span v-if="item.barcodes.length" class="item__barcode" :title="item.barcodes.join('、')">
          <AppIcon name="barcode" :size="18" />
          {{ item.barcodes.length }}
        </span>
        <AppIcon name="chevron" :size="18" class="item__chevron" />
      </RouterLink>
    </li>
  </ul>

  <p v-if="loading" class="empty">載入中…</p>
  <div v-else-if="items.length < total" class="more">
    <BaseButton variant="secondary" @click="loadMore">再載入 {{ Math.min(PAGE_SIZE, total - items.length) }} 筆</BaseButton>
  </div>
  <p v-if="total > 0" class="count">共 {{ total }} 個品項</p>
</template>

<style scoped>
.filters {
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
  margin-bottom: var(--space-4);
}

.search {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  min-height: var(--touch-target);
  padding: 0 var(--space-3);
  background: var(--color-surface);
  border: 1px solid var(--color-border-strong);
  border-radius: var(--radius-pill);
  color: var(--color-text-muted);
}

.search:focus-within {
  border-color: var(--color-primary);
  box-shadow: 0 0 0 3px var(--color-primary-soft);
}

.search input {
  flex: 1;
  min-width: 0;
  border: none;
  background: transparent;
  font-size: var(--font-size-md);
  color: var(--color-text);
  outline: none;
}

.search__scan {
  display: grid;
  place-items: center;
  width: 40px;
  height: 40px;
  margin-right: calc(-1 * var(--space-2));
  border: none;
  border-radius: 50%;
  background: transparent;
  color: var(--color-primary);
  cursor: pointer;
}

.search__scan:hover {
  background: var(--color-primary-soft);
}

.filters__row {
  display: flex;
  align-items: flex-end;
  gap: var(--space-4);
}

.filters__category {
  flex: 1;
  max-width: 280px;
}

.toggle {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  min-height: var(--touch-target);
  font-size: var(--font-size-sm);
  white-space: nowrap;
}

.toggle input {
  width: 20px;
  height: 20px;
  accent-color: var(--color-primary);
}

.settings-link {
  display: inline-flex;
  align-items: center;
  gap: var(--space-1);
  align-self: flex-start;
  font-size: var(--font-size-sm);
  text-decoration: none;
}

.items {
  display: grid;
  gap: var(--space-2);
  margin: 0;
  padding: 0;
  list-style: none;
}

.item {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  min-height: 64px;
  padding: var(--space-3) var(--space-3) var(--space-3) var(--space-4);
  color: inherit;
  text-decoration: none;
}

.item:hover {
  border-color: var(--color-border-strong);
}

.item--archived {
  opacity: 0.6;
}

.item__body {
  flex: 1;
  min-width: 0;
}

.item__name {
  font-weight: 500;
}

.item__meta {
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.item__barcode {
  display: inline-flex;
  align-items: center;
  gap: 2px;
  color: var(--color-text-muted);
  font-size: var(--font-size-xs);
}

.item__chevron {
  color: var(--color-border-strong);
  flex-shrink: 0;
}

.tag {
  margin-left: var(--space-1);
  padding: 0 var(--space-2);
  border-radius: var(--radius-pill);
  font-size: var(--font-size-xs);
  font-weight: 400;
}

.tag--muted {
  background: var(--color-surface-muted);
  color: var(--color-text-muted);
}

.empty,
.count {
  margin-top: var(--space-4);
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
  text-align: center;
}

.more {
  display: flex;
  justify-content: center;
  margin-top: var(--space-4);
}
</style>
