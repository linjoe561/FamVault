<script setup>
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import AppIcon from '@/shared/components/AppIcon.vue'
import BaseButton from '@/shared/components/BaseButton.vue'
import BaseSelect from '@/shared/components/BaseSelect.vue'
import FormAlert from '@/shared/components/FormAlert.vue'
import PageHeader from '@/shared/components/PageHeader.vue'
import TextField from '@/shared/components/TextField.vue'
import { addDays, formatMoney, todayIso } from '@/shared/utils/format'
import { itemsApi } from '@/modules/catalog/api'
import { useCatalogStore } from '@/modules/catalog/store'
import ItemPicker from '../components/ItemPicker.vue'
import { purchasesApi } from '../api'

const catalog = useCatalogStore()
const route = useRoute()
const router = useRouter()

const form = reactive({ purchaseDate: todayIso(), store: '', note: '' })
const lines = ref([])
const fieldErrors = ref({})
const error = ref('')
const saving = ref(false)

let nextKey = 1
function newLine(item = null) {
  const line = reactive({
    key: nextKey++,
    item: null,
    quantity: '1',
    expiryDate: '',
    expiryTouched: false, // 使用者自己改過到期日，就不再自動計算
    locationId: null,
    unitPrice: '',
  })
  if (item) applyItem(line, item)
  return line
}

/** 選了品項：帶入預設位置，並依預設保存天數算到期日。 */
function applyItem(line, item) {
  line.item = item
  line.locationId = item?.locationId ?? null
  line.expiryTouched = false
  line.expiryDate = defaultExpiry(item)
}

function defaultExpiry(item) {
  return item?.shelfLifeDays ? addDays(form.purchaseDate, item.shelfLifeDays) : ''
}

// 改了購買日期，沒手動改過的到期日跟著重算
watch(
  () => form.purchaseDate,
  () => {
    for (const line of lines.value) {
      if (!line.expiryTouched && line.item) line.expiryDate = defaultExpiry(line.item)
    }
  },
)

function addLine() {
  lines.value.push(newLine())
}

function removeLine(index) {
  lines.value.splice(index, 1)
  if (lines.value.length === 0) addLine()
}

const total = computed(() =>
  lines.value.reduce((sum, l) => {
    const price = Number(l.unitPrice)
    const quantity = Number(l.quantity)
    return l.unitPrice !== '' && Number.isFinite(price) && Number.isFinite(quantity) ? sum + price * quantity : sum
  }, 0),
)

function lineError(index, field) {
  return fieldErrors.value[`lines[${index}].${field}`] ?? ''
}

async function submit() {
  error.value = ''
  fieldErrors.value = {}
  const missing = lines.value.findIndex((l) => !l.item)
  if (missing >= 0) {
    fieldErrors.value = { [`lines[${missing}].itemId`]: '請選擇品項' }
    return
  }

  saving.value = true
  try {
    const purchase = await purchasesApi.create({
      purchaseDate: form.purchaseDate,
      store: form.store,
      note: form.note,
      lines: lines.value.map((l) => ({
        itemId: l.item.id,
        quantity: Number(l.quantity),
        unitPrice: l.unitPrice === '' ? null : Number(l.unitPrice),
        expiryDate: l.expiryDate || null,
        locationId: l.locationId,
      })),
    })
    router.push({ name: 'purchases', query: { created: purchase.id } })
  } catch (e) {
    error.value = e.message
    fieldErrors.value = e.fieldErrors ?? {}
  } finally {
    saving.value = false
  }
}

onMounted(async () => {
  try {
    await catalog.load()
    // 從庫存明細按「買了」過來時，直接帶入該品項
    const itemId = Number(route.query.itemId)
    lines.value = [newLine(itemId ? await itemsApi.get(itemId) : null)]
  } catch (e) {
    error.value = e.message
    lines.value = [newLine()]
  }
})
</script>

<template>
  <PageHeader title="購買登錄" subtitle="買回家的東西記在這裡，庫存會自動增加">
    <template #actions>
      <RouterLink :to="{ name: 'purchases' }" class="history-link">
        <AppIcon name="history" :size="18" />
        購買紀錄
      </RouterLink>
    </template>
  </PageHeader>

  <form class="form" novalidate @submit.prevent="submit">
    <FormAlert :message="error" />

    <section class="card head">
      <TextField v-model="form.purchaseDate" label="購買日期" type="date" :error="fieldErrors.purchaseDate" />
      <TextField v-model="form.store" label="店家" hint="可留空，例如：全聯" :error="fieldErrors.store" />
    </section>

    <section v-for="(line, index) in lines" :key="line.key" class="card line">
      <header class="line__header">
        <span class="line__index">第 {{ index + 1 }} 樣</span>
        <button v-if="lines.length > 1" type="button" class="line__remove" @click="removeLine(index)">
          <AppIcon name="close" :size="16" />
          移除
        </button>
      </header>

      <ItemPicker
        :model-value="line.item"
        :error="lineError(index, 'itemId')"
        @update:model-value="(item) => applyItem(line, item)"
      />

      <div class="line__grid">
        <TextField
          v-model="line.quantity"
          :label="line.item ? `數量（${line.item.unitName}）` : '數量'"
          inputmode="decimal"
          :error="lineError(index, 'quantity')"
        />
        <TextField
          v-model="line.expiryDate"
          label="到期日"
          type="date"
          :hint="line.item?.shelfLifeDays && !line.expiryTouched ? `依預設 ${line.item.shelfLifeDays} 天自動算出` : '沒有期限可留空'"
          :error="lineError(index, 'expiryDate')"
          @input="line.expiryTouched = true"
        />
        <BaseSelect v-model="line.locationId" label="放在哪裡" placeholder="不指定" :error="lineError(index, 'locationId')">
          <option v-for="location in catalog.locations" :key="location.id" :value="location.id">{{ location.name }}</option>
        </BaseSelect>
        <TextField
          v-model="line.unitPrice"
          label="單價"
          inputmode="decimal"
          hint="可留空"
          :error="lineError(index, 'unitPrice')"
        />
      </div>
    </section>

    <BaseButton variant="secondary" block @click="addLine">
      <AppIcon name="plus" :size="18" />
      再加一樣
    </BaseButton>

    <section class="card head">
      <TextField v-model="form.note" label="備註" hint="可留空" :error="fieldErrors.note" />
    </section>

    <footer class="submit">
      <p class="submit__total">
        共 {{ lines.length }} 樣<span v-if="total > 0">，{{ formatMoney(total) }}</span>
      </p>
      <BaseButton type="submit" :loading="saving">登錄</BaseButton>
    </footer>
  </form>
</template>

<style scoped>
.history-link {
  display: inline-flex;
  align-items: center;
  gap: var(--space-1);
  min-height: var(--touch-target);
  font-size: var(--font-size-sm);
  text-decoration: none;
}

.form {
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
}

.head {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: var(--space-4);
  padding: var(--space-4) var(--space-5);
}

.line {
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
  padding: var(--space-4) var(--space-5) var(--space-5);
}

.line__header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.line__index {
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
}

.line__remove {
  display: inline-flex;
  align-items: center;
  gap: 2px;
  min-height: 36px;
  border: none;
  background: none;
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
  cursor: pointer;
}

.line__remove:hover {
  color: var(--color-danger);
}

.line__grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: var(--space-3);
}

@media (min-width: 720px) {
  .line__grid {
    grid-template-columns: repeat(4, minmax(0, 1fr));
  }
}

.submit {
  position: sticky;
  bottom: calc(var(--tabbar-height) + env(safe-area-inset-bottom) + var(--space-2));
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-3);
  padding: var(--space-3) var(--space-3) var(--space-3) var(--space-5);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-pill);
  box-shadow: var(--shadow-overlay);
}

@media (min-width: 900px) {
  .submit {
    bottom: var(--space-4);
  }
}

.submit__total {
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
}
</style>
