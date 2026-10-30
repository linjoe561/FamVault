<script setup>
import { ref } from 'vue'
import AppIcon from '@/shared/components/AppIcon.vue'
import BarcodeScanner from '@/shared/components/BarcodeScanner.vue'
import BaseButton from '@/shared/components/BaseButton.vue'
import ExpiryBadge from '@/shared/components/ExpiryBadge.vue'
import FormAlert from '@/shared/components/FormAlert.vue'
import PageHeader from '@/shared/components/PageHeader.vue'
import { formatQuantity } from '@/shared/utils/format'
import { itemsApi } from '@/modules/catalog/api'
import ConsumeDialog from '../components/ConsumeDialog.vue'
import ItemPicker from '../components/ItemPicker.vue'
import { stockApi } from '../api'

/**
 * 掃描流程：
 * - 掃到已登錄的條碼 → 顯示庫存，可以直接「買了」或「用掉」
 * - 掃到沒登錄過的條碼 → 新增品項（自動帶入條碼），或綁定到現有品項
 */
const state = ref('scanning') // scanning | looking | found | unknown
const code = ref('')
const stock = ref(null) // 找到時的庫存明細
const error = ref('')
const notice = ref('')
const consumeOpen = ref(false)
const linking = ref(false)
const linkItem = ref(null)
const linkError = ref('')

async function onDetected(scanned) {
  code.value = scanned.trim().toUpperCase()
  state.value = 'looking'
  error.value = ''
  notice.value = ''
  try {
    const item = await itemsApi.getByBarcode(code.value)
    stock.value = await stockApi.item(item.id)
    state.value = 'found'
  } catch (e) {
    if (e.status === 404) {
      state.value = 'unknown'
    } else {
      error.value = e.message
      state.value = 'scanning'
    }
  }
}

function scanAgain() {
  state.value = 'scanning'
  code.value = ''
  stock.value = null
  error.value = ''
  linking.value = false
  linkItem.value = null
  linkError.value = ''
}

async function link(item) {
  linkItem.value = item
  if (!item) return
  linkError.value = ''
  try {
    await itemsApi.addBarcode(item.id, code.value)
    stock.value = await stockApi.item(item.id)
    notice.value = `已把條碼 ${code.value} 加到「${item.name}」，下次掃描就認得了`
    linking.value = false
    state.value = 'found'
  } catch (e) {
    linkError.value = e.message
  }
}

function onConsumed(detail, { kind, quantity }) {
  stock.value = detail
  notice.value = `已記錄${kind === 'discard' ? '丟棄' : '用掉'} ${formatQuantity(quantity)} ${detail.unitName}`
}
</script>

<template>
  <PageHeader title="掃描條碼" subtitle="掃商品條碼，馬上知道家裡有沒有" />

  <FormAlert :message="error" />

  <BarcodeScanner v-if="state === 'scanning'" @detected="onDetected" />

  <p v-else-if="state === 'looking'" class="muted">查詢條碼 {{ code }}…</p>

  <!-- 已登錄的條碼 -->
  <section v-else-if="state === 'found' && stock" class="result card">
    <p class="result__code"><AppIcon name="barcode" :size="16" /> {{ code }}</p>
    <h2 class="result__name">{{ stock.name }}</h2>
    <p class="result__quantity">
      家裡有 <strong>{{ formatQuantity(stock.quantity) }}</strong> {{ stock.unitName }}
      <span v-if="stock.isLow" class="tag tag--low">該補貨</span>
    </p>
    <p v-if="stock.batches.length" class="result__meta">
      最快到期：
      <ExpiryBadge :date="stock.batches[0].expiryDate" :status="stock.batches[0].expiryStatus" />
      <span v-if="stock.batches[0].locationName">· {{ stock.batches[0].locationName }}</span>
    </p>
    <p v-if="stock.isArchived" class="muted">這個品項已停用，要登錄購買請先到品項中取消停用。</p>

    <FormAlert :message="notice" tone="success" />

    <div class="result__actions">
      <BaseButton
        :disabled="stock.isArchived"
        @click="$router.push({ name: 'purchase-new', query: { itemId: stock.itemId } })"
      >
        <AppIcon name="cart" :size="18" />
        買了
      </BaseButton>
      <BaseButton variant="secondary" :disabled="stock.quantity === 0" @click="consumeOpen = true">
        <AppIcon name="minus" :size="18" />
        用掉／丟棄
      </BaseButton>
      <BaseButton variant="ghost" @click="$router.push({ name: 'stock-item', params: { itemId: stock.itemId } })">
        看庫存明細
      </BaseButton>
    </div>
    <BaseButton variant="secondary" block @click="scanAgain">
      <AppIcon name="scan" :size="18" />
      再掃一個
    </BaseButton>

    <ConsumeDialog v-model:open="consumeOpen" :item="stock" @done="onConsumed" />
  </section>

  <!-- 沒登錄過的條碼 -->
  <section v-else-if="state === 'unknown'" class="result card">
    <p class="result__code"><AppIcon name="barcode" :size="16" /> {{ code }}</p>
    <h2 class="result__name">這個條碼還沒登錄</h2>
    <p class="muted">如果是第一次買的東西，請新增品項；如果是家裡已經有的東西換了包裝，可以綁定到現有品項。</p>

    <div class="result__choices">
      <BaseButton
        block
        @click="$router.push({ name: 'item-new', query: { barcode: code, next: 'purchase' } })"
      >
        <AppIcon name="plus" :size="18" />
        新增品項，接著登錄購買
      </BaseButton>
      <BaseButton variant="secondary" block @click="$router.push({ name: 'item-new', query: { barcode: code } })">
        只新增品項
      </BaseButton>
      <BaseButton v-if="!linking" variant="secondary" block @click="linking = true">
        <AppIcon name="link" :size="18" />
        綁定到現有品項
      </BaseButton>
    </div>

    <div v-if="linking" class="result__link">
      <ItemPicker :model-value="linkItem" :error="linkError" :allow-scan="false" @update:model-value="link" />
    </div>

    <BaseButton variant="ghost" block @click="scanAgain">
      <AppIcon name="scan" :size="18" />
      再掃一個
    </BaseButton>
  </section>
</template>

<style scoped>
.muted {
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
}

.result {
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
  padding: var(--space-5);
}

.result__code {
  display: inline-flex;
  align-items: center;
  gap: var(--space-1);
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
  font-variant-numeric: tabular-nums;
}

.result__name {
  font-size: var(--font-size-xl);
}

.result__quantity {
  color: var(--color-text-muted);
}

.result__quantity strong {
  color: var(--color-text);
  font-family: var(--font-heading);
  font-size: var(--font-size-xl);
}

.result__meta {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--space-2);
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
}

.result__actions {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-2);
  margin-top: var(--space-2);
}

.result__choices {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
  margin-top: var(--space-2);
}

.result__link {
  padding: var(--space-4);
  background: var(--color-bg);
  border-radius: var(--radius-sm);
}

.tag {
  margin-left: var(--space-1);
  padding: 0 var(--space-2);
  border-radius: var(--radius-pill);
  font-size: var(--font-size-xs);
}

.tag--low {
  background: var(--color-accent-soft);
  color: var(--color-accent);
}
</style>
