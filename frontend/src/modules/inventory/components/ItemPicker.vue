<script setup>
import { nextTick, ref, useId, watch } from 'vue'
import AppIcon from '@/shared/components/AppIcon.vue'
import ScanDialog from '@/shared/components/ScanDialog.vue'
import { itemsApi } from '@/modules/catalog/api'

/**
 * 選品項：輸入名稱或條碼即時搜尋，或按掃描鍵用鏡頭掃條碼。選好後顯示品項名稱，可以再換。
 * v-model 是完整的品項物件（含 unitName、shelfLifeDays、locationId）。
 */
defineProps({
  error: { type: String, default: '' },
  allowScan: { type: Boolean, default: true },
})
const item = defineModel({ type: Object, default: null })

const id = useId()
const text = ref('')
const results = ref([])
const open = ref(false)
const searching = ref(false)
const input = ref(null)
const scanOpen = ref(false)
const scanError = ref('')

async function onScanned(code) {
  scanError.value = ''
  try {
    select(await itemsApi.getByBarcode(code))
  } catch (e) {
    scanError.value = e.status === 404 ? `條碼 ${code} 還沒登錄，請先到「品項」新增，或用「掃描」頁處理` : e.message
  }
}

let timer
let requestId = 0
watch(text, (value) => {
  clearTimeout(timer)
  if (!value.trim()) {
    results.value = []
    return
  }
  timer = setTimeout(async () => {
    const current = ++requestId
    searching.value = true
    try {
      const page = await itemsApi.list({ search: value.trim(), pageSize: 8 })
      if (current === requestId) results.value = page.items
    } catch {
      results.value = []
    } finally {
      if (current === requestId) searching.value = false
    }
  }, 250)
})

function select(selected) {
  item.value = selected
  text.value = ''
  results.value = []
  open.value = false
}

async function change() {
  item.value = null
  await nextTick()
  input.value?.focus()
}

function selectFirst() {
  if (results.value.length > 0) select(results.value[0])
}

function close() {
  // 讓點選結果的 click 先發生
  setTimeout(() => (open.value = false), 150)
}
</script>

<template>
  <div class="picker">
    <label class="picker__label" :for="id">品項</label>

    <div v-if="item" class="picker__selected">
      <span class="picker__name">{{ item.name }}</span>
      <span class="picker__unit">以「{{ item.unitName }}」計</span>
      <button type="button" class="picker__change" @click="change">換一個</button>
    </div>

    <div v-else class="picker__box">
      <div class="picker__input" :class="{ 'picker__input--error': error }">
        <AppIcon name="search" :size="18" />
        <input
          :id="id"
          ref="input"
          v-model="text"
          role="combobox"
          :aria-expanded="open && results.length > 0"
          :aria-controls="`${id}-list`"
          autocomplete="off"
          placeholder="輸入名稱或條碼"
          @focus="open = true"
          @blur="close"
          @keydown.enter.prevent="selectFirst"
        />
        <button
          v-if="allowScan"
          type="button"
          class="picker__scan"
          aria-label="用鏡頭掃條碼"
          @click="scanOpen = true"
        >
          <AppIcon name="scan" :size="20" />
        </button>
      </div>
      <ul v-if="open && text.trim()" :id="`${id}-list`" class="picker__list card" role="listbox">
        <li v-for="result in results" :key="result.id" role="option" :aria-selected="false">
          <button type="button" class="picker__option" @mousedown.prevent @click="select(result)">
            <span>{{ result.name }}</span>
            <span class="picker__option-meta">{{ result.categoryName ?? '' }} · {{ result.unitName }}</span>
          </button>
        </li>
        <li v-if="!searching && results.length === 0" class="picker__empty">
          找不到「{{ text }}」。請先到
          <RouterLink :to="{ name: 'item-new' }" target="_blank">品項</RouterLink>
          建立
        </li>
      </ul>
    </div>
    <p v-if="scanError || error" class="picker__error">{{ scanError || error }}</p>
    <ScanDialog v-if="allowScan" v-model:open="scanOpen" title="掃描品項條碼" @detected="onScanned" />
  </div>
</template>

<style scoped>
.picker {
  position: relative;
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
}

.picker__label {
  font-size: var(--font-size-sm);
  font-weight: 500;
}

.picker__selected {
  display: flex;
  flex-wrap: wrap;
  align-items: baseline;
  gap: var(--space-2);
  min-height: var(--touch-target);
  padding: var(--space-2) 0;
}

.picker__name {
  font-weight: 700;
  font-size: var(--font-size-lg);
}

.picker__unit {
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
}

.picker__change {
  margin-left: auto;
  border: none;
  background: none;
  color: var(--color-primary);
  font-size: var(--font-size-sm);
  cursor: pointer;
}

.picker__box {
  position: relative;
}

.picker__input {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  min-height: var(--touch-target);
  padding: 0 var(--space-3);
  background: var(--color-surface);
  border: 1px solid var(--color-border-strong);
  border-radius: var(--radius-sm);
  color: var(--color-text-muted);
}

.picker__input:focus-within {
  border-color: var(--color-primary);
  box-shadow: 0 0 0 3px var(--color-primary-soft);
}

.picker__input--error {
  border-color: var(--color-danger);
}

.picker__input input {
  flex: 1;
  min-width: 0;
  border: none;
  background: transparent;
  font-size: var(--font-size-md);
  color: var(--color-text);
  outline: none;
}

.picker__scan {
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

.picker__scan:hover {
  background: var(--color-primary-soft);
}

.picker__list {
  position: absolute;
  z-index: 20;
  top: calc(100% + 4px);
  left: 0;
  right: 0;
  max-height: 280px;
  overflow-y: auto;
  margin: 0;
  padding: var(--space-1);
  list-style: none;
  box-shadow: var(--shadow-overlay);
}

.picker__option {
  display: flex;
  justify-content: space-between;
  gap: var(--space-3);
  width: 100%;
  min-height: var(--touch-target);
  padding: 0 var(--space-3);
  border: none;
  border-radius: var(--radius-sm);
  background: transparent;
  text-align: left;
  cursor: pointer;
}

.picker__option:hover {
  background: var(--color-surface-muted);
}

.picker__option-meta {
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
  white-space: nowrap;
}

.picker__empty {
  padding: var(--space-3);
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
}

.picker__error {
  color: var(--color-danger);
  font-size: var(--font-size-xs);
}
</style>
