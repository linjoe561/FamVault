<script setup>
import { computed, onMounted, ref } from 'vue'
import AppIcon from '@/shared/components/AppIcon.vue'
import BaseButton from '@/shared/components/BaseButton.vue'
import ConfirmDialog from '@/shared/components/ConfirmDialog.vue'
import FormAlert from '@/shared/components/FormAlert.vue'
import PageHeader from '@/shared/components/PageHeader.vue'
import LookupDialog from '../components/LookupDialog.vue'
import { categoriesApi, locationsApi, unitsApi } from '../api'
import { useCatalogStore } from '../store'

const catalog = useCatalogStore()

const tabs = [
  { key: 'category', label: '分類', api: categoriesApi },
  { key: 'location', label: '位置', api: locationsApi },
  { key: 'unit', label: '單位', api: unitsApi },
]
const activeKey = ref('category')
const active = computed(() => tabs.find((t) => t.key === activeKey.value))

const error = ref('')
const notice = ref('')
const editing = ref(null)
const editOpen = ref(false)
const deleting = ref(null)
const deleteOpen = ref(false)

/** 列表：分類依兩層排列（小分類縮排），其他直接列出。 */
const rows = computed(() => {
  if (activeKey.value === 'category') {
    return catalog.categoryTree.flatMap((parent) => [
      { ...parent, depth: 0 },
      ...parent.children.map((child) => ({ ...child, depth: 1 })),
    ])
  }
  const list = activeKey.value === 'location' ? catalog.locations : catalog.units
  return list.map((record) => ({ ...record, depth: 0 }))
})

// 大分類才能當上層；其餘限制（例如有小分類的分類不能再往下放）由後端檢查並回傳訊息
const parentOptions = computed(() => catalog.categoryTree.filter((c) => c.id !== editing.value?.id))

async function refresh() {
  try {
    await catalog.load({ force: true })
  } catch (e) {
    error.value = e.message
  }
}

function openCreate() {
  editing.value = null
  editOpen.value = true
}

function openEdit(record) {
  editing.value = record
  editOpen.value = true
}

function openDelete(record) {
  deleting.value = record
  deleteOpen.value = true
}

async function save(form) {
  const saved = editing.value
    ? await active.value.api.update(editing.value.id, form)
    : await active.value.api.create(form)
  notice.value = `已儲存「${saved.name}」`
  await refresh()
}

async function remove() {
  await active.value.api.remove(deleting.value.id)
  notice.value = `已刪除「${deleting.value.name}」`
  await refresh()
}

function selectTab(key) {
  activeKey.value = key
  notice.value = ''
}

onMounted(refresh)
</script>

<template>
  <RouterLink :to="{ name: 'items' }" class="back">
    <AppIcon name="back" :size="18" />
    品項
  </RouterLink>
  <PageHeader title="分類、位置與單位" subtitle="建立品項時可以選擇的項目">
    <template #actions>
      <BaseButton @click="openCreate">
        <AppIcon name="plus" :size="18" />
        新增{{ active.label }}
      </BaseButton>
    </template>
  </PageHeader>

  <div class="tabs" role="tablist" aria-label="項目類型">
    <button
      v-for="tab in tabs"
      :key="tab.key"
      type="button"
      role="tab"
      class="tabs__tab"
      :class="{ 'tabs__tab--active': tab.key === activeKey }"
      :aria-selected="tab.key === activeKey"
      @click="selectTab(tab.key)"
    >
      {{ tab.label }}
    </button>
  </div>

  <FormAlert :message="error" />
  <FormAlert :message="notice" tone="success" />

  <ul class="rows card" role="tabpanel">
    <li v-for="row in rows" :key="row.id" class="row" :class="{ 'row--child': row.depth === 1 }">
      <div class="row__body">
        <p class="row__name">{{ row.name }}</p>
        <p class="row__meta">
          <span v-if="row.description">{{ row.description }} · </span>{{ row.itemCount }} 個品項
        </p>
      </div>
      <BaseButton variant="ghost" :aria-label="`編輯 ${row.name}`" @click="openEdit(row)">
        <AppIcon name="edit" :size="18" />
      </BaseButton>
      <BaseButton variant="ghost" :aria-label="`刪除 ${row.name}`" @click="openDelete(row)">
        <AppIcon name="trash" :size="18" />
      </BaseButton>
    </li>
    <li v-if="rows.length === 0" class="row row--empty">還沒有任何{{ active.label }}</li>
  </ul>

  <LookupDialog
    v-model:open="editOpen"
    :kind="activeKey"
    :label="active.label"
    :record="editing"
    :parent-options="parentOptions"
    :save="save"
  />
  <ConfirmDialog
    v-model:open="deleteOpen"
    :title="`刪除${active.label}`"
    :message="`確定要刪除「${deleting?.name ?? ''}」嗎？`"
    confirm-text="刪除"
    danger
    :on-confirm="remove"
  />
</template>

<style scoped>
.back {
  display: inline-flex;
  align-items: center;
  gap: var(--space-1);
  min-height: var(--touch-target);
  margin-bottom: var(--space-1);
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
  text-decoration: none;
}

.tabs {
  display: inline-flex;
  gap: var(--space-1);
  padding: var(--space-1);
  margin-bottom: var(--space-4);
  background: var(--color-surface-muted);
  border-radius: var(--radius-pill);
}

.tabs__tab {
  min-height: 40px;
  padding: 0 var(--space-5);
  border: none;
  border-radius: var(--radius-pill);
  background: transparent;
  color: var(--color-text-muted);
  cursor: pointer;
}

.tabs__tab--active {
  background: var(--color-surface);
  color: var(--color-text);
  font-weight: 500;
  box-shadow: var(--shadow-card);
}

.rows {
  margin: var(--space-3) 0 0;
  padding: 0;
  list-style: none;
}

.row {
  display: flex;
  align-items: center;
  gap: var(--space-1);
  min-height: 60px;
  padding: var(--space-2) var(--space-2) var(--space-2) var(--space-4);
  border-bottom: 1px solid var(--color-border);
}

.row:last-child {
  border-bottom: none;
}

.row--child {
  padding-left: var(--space-6);
}

.row--child .row__name::before {
  content: '└ ';
  color: var(--color-border-strong);
}

.row--empty {
  justify-content: center;
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
}

.row__body {
  flex: 1;
  min-width: 0;
}

.row__meta {
  color: var(--color-text-muted);
  font-size: var(--font-size-xs);
}
</style>
