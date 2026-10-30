<script setup>
import { onMounted, reactive, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import AppIcon from '@/shared/components/AppIcon.vue'
import BaseButton from '@/shared/components/BaseButton.vue'
import BaseSelect from '@/shared/components/BaseSelect.vue'
import ExpiryBadge from '@/shared/components/ExpiryBadge.vue'
import FormAlert from '@/shared/components/FormAlert.vue'
import PageHeader from '@/shared/components/PageHeader.vue'
import { formatQuantity } from '@/shared/utils/format'
import { useCatalogStore } from '@/modules/catalog/store'
import { stockApi } from '../api'

const PAGE_SIZE = 40

const statuses = [
  { value: 'all', label: '全部' },
  { value: 'inStock', label: '有庫存' },
  { value: 'outOfStock', label: '沒有了' },
  { value: 'expiring', label: '快過期' },
  { value: 'low', label: '該補貨' },
]

const catalog = useCatalogStore()
const route = useRoute()
// 從首頁捷徑過來時會帶 ?status=expiring
const initialStatus = statuses.some((s) => s.value === route.query.status) ? route.query.status : 'all'
const filters = reactive({ search: '', status: initialStatus, categoryId: null })
const rows = ref([])
const total = ref(0)
const page = ref(1)
const loading = ref(false)
const error = ref('')

async function load({ append = false } = {}) {
  loading.value = true
  error.value = ''
  try {
    const result = await stockApi.overview({
      search: filters.search.trim() || undefined,
      status: filters.status,
      categoryId: filters.categoryId ?? undefined,
      page: page.value,
      pageSize: PAGE_SIZE,
    })
    rows.value = append ? [...rows.value, ...result.items] : result.items
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

let searchTimer
watch(
  () => filters.search,
  () => {
    clearTimeout(searchTimer)
    searchTimer = setTimeout(reload, 300)
  },
)
watch(() => [filters.status, filters.categoryId], reload)

const emptyMessages = {
  all: '還沒有任何品項，先到「品項」建立吧',
  inStock: '家裡目前沒有任何庫存',
  outOfStock: '每樣東西家裡都還有',
  expiring: '沒有快過期的東西 👍',
  low: '目前沒有需要補貨的東西',
}

onMounted(() => {
  catalog.load().catch((e) => (error.value = e.message))
  load()
})
</script>

<template>
  <PageHeader title="家裡還有什麼" subtitle="查查看家裡還有沒有、放在哪裡、什麼時候到期">
    <template #actions>
      <BaseButton @click="$router.push({ name: 'purchase-new' })">
        <AppIcon name="plus" :size="18" />
        購買登錄
      </BaseButton>
    </template>
  </PageHeader>

  <section class="filters">
    <label class="search">
      <AppIcon name="search" :size="20" />
      <span class="visually-hidden">搜尋</span>
      <input v-model="filters.search" type="search" placeholder="家裡還有沒有…（名稱或條碼）" enterkeyhint="search" />
    </label>

    <div class="chips" role="radiogroup" aria-label="狀態">
      <button
        v-for="option in statuses"
        :key="option.value"
        type="button"
        role="radio"
        class="chip"
        :class="{ 'chip--active': filters.status === option.value }"
        :aria-checked="filters.status === option.value"
        @click="filters.status = option.value"
      >
        {{ option.label }}
      </button>
    </div>

    <div class="filters__category">
      <BaseSelect v-model="filters.categoryId" label="分類" placeholder="全部分類">
        <template v-for="parent in catalog.categoryTree" :key="parent.id">
          <option :value="parent.id">{{ parent.name }}</option>
          <option v-for="child in parent.children" :key="child.id" :value="child.id">　└ {{ child.name }}</option>
        </template>
      </BaseSelect>
    </div>
  </section>

  <FormAlert :message="error" />

  <p v-if="!loading && rows.length === 0" class="empty">
    {{ filters.search ? `找不到「${filters.search}」，可能還沒建立這個品項` : emptyMessages[filters.status] }}
  </p>

  <ul class="rows">
    <li v-for="row in rows" :key="row.itemId">
      <RouterLink
        :to="{ name: 'stock-item', params: { itemId: row.itemId } }"
        class="row card"
        :class="{ 'row--empty': row.quantity === 0 }"
      >
        <div class="row__body">
          <p class="row__name">
            {{ row.name }}
            <span v-if="row.isLow" class="tag tag--low">該補貨</span>
          </p>
          <p class="row__meta">
            <ExpiryBadge v-if="row.quantity > 0 && row.nearestExpiry" :date="row.nearestExpiry" :status="row.expiryStatus" />
            <span v-if="row.locationNames.length">{{ row.locationNames.join('、') }}</span>
            <span v-if="row.quantity === 0">家裡沒有了</span>
          </p>
        </div>
        <p class="row__quantity">
          <strong>{{ formatQuantity(row.quantity) }}</strong>
          <span>{{ row.unitName }}</span>
        </p>
      </RouterLink>
    </li>
  </ul>

  <p v-if="loading" class="empty">載入中…</p>
  <div v-else-if="rows.length < total" class="more">
    <BaseButton variant="secondary" @click="page++; load({ append: true })">再載入更多</BaseButton>
  </div>
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

.chips {
  display: flex;
  gap: var(--space-2);
  overflow-x: auto;
  padding-bottom: 2px;
  scrollbar-width: none;
}

.chip {
  flex-shrink: 0;
  min-height: 36px;
  padding: 0 var(--space-4);
  border: 1px solid var(--color-border-strong);
  border-radius: var(--radius-pill);
  background: var(--color-surface);
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
  cursor: pointer;
}

.chip--active {
  border-color: var(--color-primary);
  background: var(--color-primary-soft);
  color: var(--color-primary-hover);
  font-weight: 500;
}

.filters__category {
  max-width: 280px;
}

.rows {
  display: grid;
  gap: var(--space-2);
  margin: 0;
  padding: 0;
  list-style: none;
}

.row {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  min-height: 68px;
  padding: var(--space-3) var(--space-4);
  color: inherit;
  text-decoration: none;
}

.row:hover {
  border-color: var(--color-border-strong);
}

.row--empty {
  background: var(--color-bg);
  box-shadow: none;
}

.row__body {
  flex: 1;
  min-width: 0;
}

.row__name {
  font-weight: 500;
}

.row__meta {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--space-1) var(--space-2);
  margin-top: 2px;
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
}

.row__quantity {
  display: flex;
  align-items: baseline;
  gap: var(--space-1);
  flex-shrink: 0;
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
}

.row__quantity strong {
  color: var(--color-text);
  font-family: var(--font-heading);
  font-size: var(--font-size-xl);
  font-variant-numeric: tabular-nums;
}

.row--empty .row__quantity strong {
  color: var(--color-text-muted);
}

.tag {
  margin-left: var(--space-1);
  padding: 0 var(--space-2);
  border-radius: var(--radius-pill);
  font-size: var(--font-size-xs);
  font-weight: 400;
}

.tag--low {
  background: var(--color-accent-soft);
  color: var(--color-accent);
}

.empty {
  margin-top: var(--space-5);
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
