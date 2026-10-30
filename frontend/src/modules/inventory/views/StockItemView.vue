<script setup>
import { onMounted, ref } from 'vue'
import AppIcon from '@/shared/components/AppIcon.vue'
import BaseButton from '@/shared/components/BaseButton.vue'
import ExpiryBadge from '@/shared/components/ExpiryBadge.vue'
import FormAlert from '@/shared/components/FormAlert.vue'
import { formatDate, formatDateTime, formatQuantity } from '@/shared/utils/format'
import BatchEditDialog from '../components/BatchEditDialog.vue'
import ConsumeDialog from '../components/ConsumeDialog.vue'
import { stockApi, transactionLabels } from '../api'

const props = defineProps({
  itemId: { type: Number, required: true },
})

const detail = ref(null)
const loading = ref(true)
const error = ref('')
const notice = ref('')

const consumeOpen = ref(false)
const consumeBatch = ref(null)
const consumeKind = ref('consume')
const editOpen = ref(false)
const editBatch = ref(null)

async function load() {
  loading.value = true
  error.value = ''
  try {
    detail.value = await stockApi.item(props.itemId)
  } catch (e) {
    error.value = e.message
  } finally {
    loading.value = false
  }
}

function openConsume(kind, batch = null) {
  consumeKind.value = kind
  consumeBatch.value = batch
  consumeOpen.value = true
}

function openEdit(batch) {
  editBatch.value = batch
  editOpen.value = true
}

function onConsumed(updated, { kind, quantity }) {
  detail.value = updated
  notice.value = `已記錄${kind === 'discard' ? '丟棄' : '用掉'} ${formatQuantity(quantity)} ${updated.unitName}`
}

function onEdited(updated) {
  detail.value = updated
  notice.value = '已更新'
}

function describeChange(transaction) {
  const sign = transaction.quantityChange > 0 ? '+' : '−'
  return `${sign}${formatQuantity(Math.abs(transaction.quantityChange))}`
}

onMounted(load)
</script>

<template>
  <RouterLink :to="{ name: 'stock' }" class="back">
    <AppIcon name="back" :size="18" />
    庫存
  </RouterLink>

  <FormAlert :message="error" />
  <p v-if="loading && !detail" class="muted">載入中…</p>

  <template v-if="detail">
    <header class="summary card">
      <div class="summary__main">
        <h1 class="summary__name">{{ detail.name }}</h1>
        <p class="summary__meta">
          {{ detail.categoryName ?? '未分類' }}
          <span v-if="detail.isArchived"> · 已停用</span>
          <span v-if="detail.safetyStock !== null"> · 安全庫存 {{ formatQuantity(detail.safetyStock) }} {{ detail.unitName }}</span>
        </p>
      </div>
      <p class="summary__quantity">
        <strong>{{ formatQuantity(detail.quantity) }}</strong> {{ detail.unitName }}
        <span v-if="detail.isLow" class="tag tag--low">該補貨</span>
      </p>
      <div class="summary__actions">
        <BaseButton :disabled="detail.quantity === 0" @click="openConsume('consume')">
          <AppIcon name="minus" :size="18" />
          用掉
        </BaseButton>
        <BaseButton variant="secondary" :disabled="detail.quantity === 0" @click="openConsume('discard')">
          <AppIcon name="trash" :size="18" />
          丟棄
        </BaseButton>
        <BaseButton
          variant="ghost"
          @click="$router.push({ name: 'purchase-new', query: { itemId: detail.itemId } })"
        >
          <AppIcon name="cart" :size="18" />
          買了
        </BaseButton>
      </div>
    </header>

    <FormAlert :message="notice" tone="success" />

    <section class="section">
      <h2 class="section__title">家裡的這些（先到期的排前面）</h2>
      <p v-if="detail.batches.length === 0" class="muted">家裡沒有了。</p>
      <ul class="batches">
        <li v-for="batch in detail.batches" :key="batch.id" class="batch card">
          <div class="batch__body">
            <p class="batch__quantity">
              <strong>{{ formatQuantity(batch.quantity) }}</strong> {{ detail.unitName }}
              <span v-if="batch.quantity !== batch.initialQuantity" class="muted">
                （買了 {{ formatQuantity(batch.initialQuantity) }}）
              </span>
            </p>
            <p class="batch__meta">
              <ExpiryBadge :date="batch.expiryDate" :status="batch.expiryStatus" />
              <span>{{ batch.locationName ?? '未指定位置' }}</span>
              <span>{{ formatDate(batch.purchasedOn) }} 買的</span>
            </p>
          </div>
          <div class="batch__actions">
            <BaseButton variant="ghost" :aria-label="`用掉這一批`" @click="openConsume(batch.expiryStatus === 'expired' ? 'discard' : 'consume', batch)">
              <AppIcon name="minus" :size="18" />
            </BaseButton>
            <BaseButton variant="ghost" :aria-label="`修改這一批`" @click="openEdit(batch)">
              <AppIcon name="edit" :size="18" />
            </BaseButton>
          </div>
        </li>
      </ul>
    </section>

    <section class="section">
      <h2 class="section__title">
        <AppIcon name="history" :size="18" />
        最近的紀錄
      </h2>
      <p v-if="detail.history.length === 0" class="muted">還沒有任何紀錄。</p>
      <ul class="history">
        <li v-for="t in detail.history" :key="t.id" class="history__row">
          <span class="history__type" :class="`history__type--${t.type}`">{{ transactionLabels[t.type] }}</span>
          <span class="history__change">{{ describeChange(t) }}</span>
          <span class="history__who">{{ t.userName ?? '—' }}</span>
          <span class="history__when">{{ formatDateTime(t.occurredAt) }}</span>
          <span v-if="t.note" class="history__note">{{ t.note }}</span>
        </li>
      </ul>
    </section>

    <RouterLink :to="{ name: 'item-edit', params: { id: detail.itemId } }" class="item-link">
      <AppIcon name="edit" :size="16" />
      修改品項資料（名稱、條碼、保存天數…）
    </RouterLink>

    <ConsumeDialog
      v-model:open="consumeOpen"
      :item="detail"
      :batch="consumeBatch"
      :initial-kind="consumeKind"
      @done="onConsumed"
    />
    <BatchEditDialog v-model:open="editOpen" :batch="editBatch" :unit-name="detail.unitName" @done="onEdited" />
  </template>
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

.summary {
  display: grid;
  grid-template-columns: 1fr auto;
  gap: var(--space-3) var(--space-4);
  padding: var(--space-5);
  margin-bottom: var(--space-4);
}

.summary__name {
  font-size: var(--font-size-xl);
}

.summary__meta {
  margin-top: var(--space-1);
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
}

.summary__quantity {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
}

.summary__quantity strong {
  color: var(--color-text);
  font-family: var(--font-heading);
  font-size: 2.25rem;
  line-height: 1.1;
}

.summary__actions {
  grid-column: 1 / -1;
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-2);
}

.tag {
  margin-top: var(--space-1);
  padding: 0 var(--space-2);
  border-radius: var(--radius-pill);
  font-size: var(--font-size-xs);
}

.tag--low {
  background: var(--color-accent-soft);
  color: var(--color-accent);
}

.section {
  margin-top: var(--space-5);
}

.section__title {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  margin-bottom: var(--space-3);
  font-size: var(--font-size-md);
}

.batches {
  display: grid;
  gap: var(--space-2);
  margin: 0;
  padding: 0;
  list-style: none;
}

.batch {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  padding: var(--space-3) var(--space-2) var(--space-3) var(--space-4);
}

.batch__body {
  flex: 1;
  min-width: 0;
}

.batch__quantity strong {
  font-size: var(--font-size-lg);
}

.batch__meta {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--space-1) var(--space-2);
  margin-top: 2px;
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
}

.batch__actions {
  display: flex;
}

.history {
  margin: 0;
  padding: 0;
  list-style: none;
}

.history__row {
  display: grid;
  grid-template-columns: auto auto 1fr auto;
  align-items: center;
  gap: var(--space-1) var(--space-3);
  padding: var(--space-3) 0;
  border-bottom: 1px solid var(--color-border);
  font-size: var(--font-size-sm);
}

.history__type {
  padding: 0 var(--space-2);
  border-radius: var(--radius-pill);
  background: var(--color-surface-muted);
  font-size: var(--font-size-xs);
}

.history__type--purchase {
  background: var(--color-primary-soft);
  color: var(--color-primary-hover);
}

.history__type--discard {
  background: var(--color-danger-soft);
  color: var(--color-danger);
}

.history__change {
  font-variant-numeric: tabular-nums;
  font-weight: 500;
}

.history__who {
  color: var(--color-text-muted);
}

.history__when {
  color: var(--color-text-muted);
  font-size: var(--font-size-xs);
  white-space: nowrap;
}

.history__note {
  grid-column: 1 / -1;
  color: var(--color-text-muted);
  font-size: var(--font-size-xs);
}

.item-link {
  display: inline-flex;
  align-items: center;
  gap: var(--space-1);
  min-height: var(--touch-target);
  margin-top: var(--space-5);
  font-size: var(--font-size-sm);
  text-decoration: none;
}
</style>
