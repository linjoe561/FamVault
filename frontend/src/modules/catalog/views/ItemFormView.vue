<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import AppIcon from '@/shared/components/AppIcon.vue'
import BaseButton from '@/shared/components/BaseButton.vue'
import BaseSelect from '@/shared/components/BaseSelect.vue'
import ConfirmDialog from '@/shared/components/ConfirmDialog.vue'
import FormAlert from '@/shared/components/FormAlert.vue'
import PageHeader from '@/shared/components/PageHeader.vue'
import TextField from '@/shared/components/TextField.vue'
import { useAuthStore } from '@/modules/auth/store'
import BarcodeEditor from '../components/BarcodeEditor.vue'
import { itemsApi } from '../api'
import { useCatalogStore } from '../store'

const props = defineProps({
  id: { type: Number, default: null }, // null 代表新增
})

const auth = useAuthStore()
const catalog = useCatalogStore()
const route = useRoute()
const router = useRouter()

const isEdit = computed(() => props.id !== null)
const form = reactive({
  name: '',
  categoryId: null,
  unitId: null,
  locationId: null,
  safetyStock: '',
  shelfLifeDays: '',
  note: '',
  barcodes: [],
  isArchived: false,
})
const fieldErrors = ref({})
const error = ref('')
const loading = ref(true)
const saving = ref(false)
const deleteOpen = ref(false)

onMounted(async () => {
  try {
    await catalog.load()
    if (isEdit.value) {
      const item = await itemsApi.get(props.id)
      Object.assign(form, {
        ...item,
        safetyStock: item.safetyStock?.toString() ?? '',
        shelfLifeDays: item.shelfLifeDays?.toString() ?? '',
        note: item.note ?? '',
      })
    } else {
      form.unitId = catalog.units[0]?.id ?? null
      // 掃到未登錄的條碼時，會帶著 ?barcode= 過來
      if (typeof route.query.barcode === 'string' && route.query.barcode) {
        form.barcodes = [route.query.barcode.trim().toUpperCase()]
      }
    }
  } catch (e) {
    error.value = e.message
  } finally {
    loading.value = false
  }
})

function toNumber(value) {
  const text = String(value).trim()
  return text === '' ? null : Number(text)
}

async function save() {
  saving.value = true
  error.value = ''
  fieldErrors.value = {}
  const payload = {
    name: form.name,
    categoryId: form.categoryId,
    unitId: form.unitId ?? 0,
    locationId: form.locationId,
    safetyStock: toNumber(form.safetyStock),
    shelfLifeDays: toNumber(form.shelfLifeDays),
    note: form.note,
    barcodes: form.barcodes,
    isArchived: form.isArchived,
  }
  if (Number.isNaN(payload.safetyStock) || Number.isNaN(payload.shelfLifeDays)) {
    fieldErrors.value = {
      ...(Number.isNaN(payload.safetyStock) && { safetyStock: '請輸入數字' }),
      ...(Number.isNaN(payload.shelfLifeDays) && { shelfLifeDays: '請輸入數字' }),
    }
    saving.value = false
    return
  }

  try {
    if (isEdit.value) {
      await itemsApi.update(props.id, payload)
      router.push({ name: 'items' })
    } else {
      const created = await itemsApi.create(payload)
      // 從掃描頁「新增品項，接著登錄購買」過來時，存檔後直接帶到購買登錄
      router.push(
        route.query.next === 'purchase'
          ? { name: 'purchase-new', query: { itemId: created.id } }
          : { name: 'items' },
      )
    }
  } catch (e) {
    error.value = e.message
    fieldErrors.value = e.fieldErrors ?? {}
  } finally {
    saving.value = false
  }
}

async function remove() {
  await itemsApi.remove(props.id)
  router.replace({ name: 'items' })
}
</script>

<template>
  <RouterLink :to="{ name: 'items' }" class="back">
    <AppIcon name="back" :size="18" />
    品項
  </RouterLink>
  <PageHeader :title="isEdit ? '編輯品項' : '新增品項'" />

  <p v-if="loading" class="muted">載入中…</p>

  <form v-else class="form card" novalidate @submit.prevent="save">
    <FormAlert :message="error" />

    <TextField v-model="form.name" label="名稱" hint="例如：鮮奶 936ml、衛生紙" :error="fieldErrors.name" />

    <div class="grid">
      <BaseSelect v-model="form.categoryId" label="分類" placeholder="未分類" :error="fieldErrors.categoryId">
        <template v-for="parent in catalog.categoryTree" :key="parent.id">
          <option :value="parent.id">{{ parent.name }}</option>
          <option v-for="child in parent.children" :key="child.id" :value="child.id">　└ {{ child.name }}</option>
        </template>
      </BaseSelect>
      <BaseSelect v-model="form.unitId" label="單位" :error="fieldErrors.unitId">
        <option v-for="unit in catalog.units" :key="unit.id" :value="unit.id">{{ unit.name }}</option>
      </BaseSelect>
      <BaseSelect
        v-model="form.locationId"
        label="預設存放位置"
        placeholder="不指定"
        hint="購買登錄時會自動帶入"
        :error="fieldErrors.locationId"
      >
        <option v-for="location in catalog.locations" :key="location.id" :value="location.id">
          {{ location.name }}
        </option>
      </BaseSelect>
    </div>

    <div class="grid">
      <TextField
        v-model="form.shelfLifeDays"
        label="預設保存天數"
        inputmode="numeric"
        hint="購買登錄時自動算出到期日，可留空"
        :error="fieldErrors.shelfLifeDays"
      />
      <TextField
        v-model="form.safetyStock"
        label="安全庫存量"
        inputmode="decimal"
        hint="剩下少於這個數量時提醒補貨，可留空"
        :error="fieldErrors.safetyStock"
      />
    </div>

    <BarcodeEditor v-model="form.barcodes" :error="fieldErrors.barcodes" />

    <div class="field">
      <label class="field__label" for="item-note">備註</label>
      <textarea id="item-note" v-model="form.note" class="field__textarea" rows="3" maxlength="500" />
      <p v-if="fieldErrors.note" class="field__error">{{ fieldErrors.note }}</p>
    </div>

    <label v-if="isEdit" class="toggle">
      <input v-model="form.isArchived" type="checkbox" />
      <span>停用這個品項（不再出現在清單與購買登錄，資料會保留）</span>
    </label>

    <div class="actions">
      <BaseButton v-if="isEdit && auth.isAdmin" variant="danger" class="actions__delete" @click="deleteOpen = true">
        <AppIcon name="trash" :size="18" />
        刪除
      </BaseButton>
      <BaseButton variant="ghost" @click="$router.push({ name: 'items' })">取消</BaseButton>
      <BaseButton type="submit" :loading="saving">儲存</BaseButton>
    </div>
  </form>

  <ConfirmDialog
    v-model:open="deleteOpen"
    title="刪除品項"
    :message="`確定要永久刪除「${form.name}」嗎？如果只是暫時不買，建議改用「停用」。`"
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
}

.form {
  display: flex;
  flex-direction: column;
  gap: var(--space-5);
  padding: var(--space-5);
}

.grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: var(--space-4);
}

.field {
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
}

.field__label {
  font-size: var(--font-size-sm);
  font-weight: 500;
}

.field__textarea {
  padding: var(--space-2) var(--space-3);
  background: var(--color-surface);
  border: 1px solid var(--color-border-strong);
  border-radius: var(--radius-sm);
  font-size: var(--font-size-md);
  resize: vertical;
}

.field__textarea:focus {
  border-color: var(--color-primary);
  outline: none;
  box-shadow: 0 0 0 3px var(--color-primary-soft);
}

.field__error {
  color: var(--color-danger);
  font-size: var(--font-size-xs);
}

.toggle {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  min-height: var(--touch-target);
  font-size: var(--font-size-sm);
}

.toggle input {
  flex-shrink: 0;
  width: 20px;
  height: 20px;
  accent-color: var(--color-primary);
}

.actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--space-2);
}

.actions__delete {
  margin-right: auto;
}
</style>
