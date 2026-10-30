<script setup>
import { onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import AppIcon from '@/shared/components/AppIcon.vue'
import BaseButton from '@/shared/components/BaseButton.vue'
import ConfirmDialog from '@/shared/components/ConfirmDialog.vue'
import FormAlert from '@/shared/components/FormAlert.vue'
import PageHeader from '@/shared/components/PageHeader.vue'
import { formatDate, formatMoney, formatQuantity } from '@/shared/utils/format'
import { purchasesApi } from '../api'

const PAGE_SIZE = 20

const route = useRoute()
const purchases = ref([])
const total = ref(0)
const page = ref(1)
const loading = ref(false)
const error = ref('')
const notice = ref(route.query.created ? '登錄完成，庫存已更新' : '')
const expanded = ref(new Set(route.query.created ? [Number(route.query.created)] : []))

const deleting = ref(null)
const deleteOpen = ref(false)

async function load({ append = false } = {}) {
  loading.value = true
  error.value = ''
  try {
    const result = await purchasesApi.list({ page: page.value, pageSize: PAGE_SIZE })
    purchases.value = append ? [...purchases.value, ...result.items] : result.items
    total.value = result.total
  } catch (e) {
    error.value = e.message
  } finally {
    loading.value = false
  }
}

function toggle(id) {
  const next = new Set(expanded.value)
  next.has(id) ? next.delete(id) : next.add(id)
  expanded.value = next
}

function summary(purchase) {
  const names = purchase.lines.map((l) => `${l.itemName} ×${formatQuantity(l.quantity)}`)
  return names.length > 3 ? `${names.slice(0, 3).join('、')}… 共 ${names.length} 樣` : names.join('、')
}

function openDelete(purchase) {
  deleting.value = purchase
  deleteOpen.value = true
}

async function remove() {
  await purchasesApi.remove(deleting.value.id)
  purchases.value = purchases.value.filter((p) => p.id !== deleting.value.id)
  total.value -= 1
  notice.value = '已刪除這筆購買，庫存也一併扣回'
}

onMounted(load)
</script>

<template>
  <RouterLink :to="{ name: 'purchase-new' }" class="back">
    <AppIcon name="back" :size="18" />
    購買登錄
  </RouterLink>
  <PageHeader title="購買紀錄" subtitle="最近買了什麼、誰登錄的">
    <template #actions>
      <BaseButton @click="$router.push({ name: 'purchase-new' })">
        <AppIcon name="plus" :size="18" />
        登錄
      </BaseButton>
    </template>
  </PageHeader>

  <FormAlert :message="error" />
  <FormAlert :message="notice" tone="success" />

  <p v-if="!loading && purchases.length === 0" class="empty">還沒有購買紀錄。</p>

  <ul class="purchases">
    <li v-for="purchase in purchases" :key="purchase.id" class="purchase card">
      <button
        type="button"
        class="purchase__head"
        :aria-expanded="expanded.has(purchase.id)"
        @click="toggle(purchase.id)"
      >
        <div class="purchase__main">
          <p class="purchase__title">
            {{ formatDate(purchase.purchaseDate) }}
            <span v-if="purchase.store" class="purchase__store">{{ purchase.store }}</span>
          </p>
          <p class="purchase__summary">{{ summary(purchase) }}</p>
        </div>
        <div class="purchase__side">
          <p v-if="purchase.totalAmount !== null" class="purchase__total">{{ formatMoney(purchase.totalAmount) }}</p>
          <p class="purchase__who">{{ purchase.createdByName ?? '—' }}</p>
        </div>
      </button>

      <div v-if="expanded.has(purchase.id)" class="purchase__detail">
        <table class="lines">
          <thead>
            <tr>
              <th scope="col">品項</th>
              <th scope="col">數量</th>
              <th scope="col">到期日</th>
              <th scope="col">位置</th>
              <th scope="col">單價</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="line in purchase.lines" :key="line.id">
              <td>
                <RouterLink :to="{ name: 'stock-item', params: { itemId: line.itemId } }">{{ line.itemName }}</RouterLink>
              </td>
              <td>
                {{ formatQuantity(line.quantity) }} {{ line.unitName }}
                <span v-if="line.remainingQuantity !== line.quantity" class="muted">
                  （剩 {{ formatQuantity(line.remainingQuantity) }}）
                </span>
              </td>
              <td>{{ line.expiryDate ? formatDate(line.expiryDate) : '—' }}</td>
              <td>{{ line.locationName ?? '—' }}</td>
              <td>{{ line.unitPrice !== null ? formatMoney(line.unitPrice) : '—' }}</td>
            </tr>
          </tbody>
        </table>
        <p v-if="purchase.note" class="muted">備註：{{ purchase.note }}</p>
        <div v-if="purchase.canDelete" class="purchase__actions">
          <BaseButton variant="danger" @click="openDelete(purchase)">
            <AppIcon name="trash" :size="18" />
            登錄錯了，刪除這筆
          </BaseButton>
        </div>
      </div>
    </li>
  </ul>

  <p v-if="loading" class="empty">載入中…</p>
  <div v-else-if="purchases.length < total" class="more">
    <BaseButton variant="secondary" @click="page++; load({ append: true })">再載入更多</BaseButton>
  </div>

  <ConfirmDialog
    v-model:open="deleteOpen"
    title="刪除購買紀錄"
    :message="`確定要刪除 ${deleting ? formatDate(deleting.purchaseDate) : ''} 的這筆購買嗎？這次登錄的庫存也會一起刪除。`"
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

.muted {
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
}

.purchases {
  display: grid;
  gap: var(--space-2);
  margin: var(--space-3) 0 0;
  padding: 0;
  list-style: none;
}

.purchase__head {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  width: 100%;
  min-height: 64px;
  padding: var(--space-3) var(--space-4);
  border: none;
  background: transparent;
  text-align: left;
  cursor: pointer;
}

.purchase__main {
  flex: 1;
  min-width: 0;
}

.purchase__title {
  font-weight: 500;
}

.purchase__store {
  margin-left: var(--space-2);
  color: var(--color-text-muted);
  font-weight: 400;
  font-size: var(--font-size-sm);
}

.purchase__summary {
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.purchase__side {
  text-align: right;
  flex-shrink: 0;
}

.purchase__total {
  font-weight: 500;
  font-variant-numeric: tabular-nums;
}

.purchase__who {
  color: var(--color-text-muted);
  font-size: var(--font-size-xs);
}

.purchase__detail {
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
  padding: 0 var(--space-4) var(--space-4);
  border-top: 1px solid var(--color-border);
  overflow-x: auto;
}

.lines {
  width: 100%;
  border-collapse: collapse;
  font-size: var(--font-size-sm);
}

.lines th {
  padding: var(--space-2) var(--space-2) var(--space-1) 0;
  color: var(--color-text-muted);
  font-weight: 400;
  font-size: var(--font-size-xs);
  text-align: left;
  white-space: nowrap;
}

.lines td {
  padding: var(--space-2) var(--space-2) var(--space-2) 0;
  border-top: 1px solid var(--color-border);
  white-space: nowrap;
}

.purchase__actions {
  display: flex;
  justify-content: flex-end;
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
