<script setup>
import { computed, reactive, ref, watch } from 'vue'
import AppDialog from '@/shared/components/AppDialog.vue'
import BaseButton from '@/shared/components/BaseButton.vue'
import FormAlert from '@/shared/components/FormAlert.vue'
import TextField from '@/shared/components/TextField.vue'
import { membersApi, roleLabels } from '../api'

const props = defineProps({
  member: { type: Object, default: null }, // null 代表新增
})
const emit = defineEmits(['saved'])
const open = defineModel('open', { type: Boolean, default: false })

const isEdit = computed(() => !!props.member)
const form = reactive({ displayName: '', userName: '', password: '', role: 'Member', isActive: true })
const fieldErrors = ref({})
const error = ref('')
const submitting = ref(false)

watch(open, (isOpen) => {
  if (!isOpen) return
  const m = props.member
  Object.assign(form, {
    displayName: m?.displayName ?? '',
    userName: m?.userName ?? '',
    password: '',
    role: m?.role ?? 'Member',
    isActive: m?.isActive ?? true,
  })
  fieldErrors.value = {}
  error.value = ''
})

async function submit() {
  submitting.value = true
  error.value = ''
  fieldErrors.value = {}
  try {
    const saved = isEdit.value
      ? await membersApi.update(props.member.id, {
          displayName: form.displayName,
          role: form.role,
          isActive: form.isActive,
        })
      : await membersApi.create({
          displayName: form.displayName,
          userName: form.userName,
          password: form.password,
          role: form.role,
        })
    emit('saved', saved)
    open.value = false
  } catch (e) {
    error.value = e.message
    fieldErrors.value = e.fieldErrors ?? {}
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <AppDialog v-model:open="open" :title="isEdit ? '編輯成員' : '新增成員'">
    <form class="form" novalidate @submit.prevent="submit">
      <FormAlert :message="error" />
      <TextField v-model="form.displayName" label="顯示名稱" :error="fieldErrors.displayName" />
      <TextField
        v-if="!isEdit"
        v-model="form.userName"
        label="帳號"
        hint="登入用，英文字母、數字與 . _ -"
        autocapitalize="none"
        autocomplete="off"
        :error="fieldErrors.userName"
      />
      <TextField
        v-if="!isEdit"
        v-model="form.password"
        label="初始密碼"
        type="password"
        hint="至少 8 個字元，請對方登入後自行修改"
        autocomplete="new-password"
        :error="fieldErrors.password"
      />

      <fieldset class="choice">
        <legend class="choice__legend">角色</legend>
        <label v-for="(label, value) in roleLabels" :key="value" class="choice__option">
          <input v-model="form.role" type="radio" name="role" :value="value" />
          <span>{{ label }}</span>
        </label>
      </fieldset>
      <p v-if="fieldErrors.role" class="form__error">{{ fieldErrors.role }}</p>

      <label v-if="isEdit" class="toggle">
        <input v-model="form.isActive" type="checkbox" />
        <span>啟用帳號（停用後無法登入）</span>
      </label>

      <div class="form__actions">
        <BaseButton variant="ghost" @click="open = false">取消</BaseButton>
        <BaseButton type="submit" :loading="submitting">{{ isEdit ? '儲存' : '新增' }}</BaseButton>
      </div>
    </form>
  </AppDialog>
</template>

<style scoped>
.form {
  display: flex;
  flex-direction: column;
  gap: var(--space-4);
}

.choice {
  display: flex;
  gap: var(--space-2);
  margin: 0;
  padding: 0;
  border: none;
}

.choice__legend {
  margin-bottom: var(--space-1);
  font-size: var(--font-size-sm);
  font-weight: 500;
}

.choice__option {
  flex: 1;
  position: relative;
}

.choice__option input {
  position: absolute;
  opacity: 0;
}

.choice__option span {
  display: grid;
  place-items: center;
  min-height: var(--touch-target);
  border: 1px solid var(--color-border-strong);
  border-radius: var(--radius-sm);
  cursor: pointer;
}

.choice__option input:checked + span {
  border-color: var(--color-primary);
  background: var(--color-primary-soft);
  color: var(--color-primary-hover);
  font-weight: 500;
}

.choice__option input:focus-visible + span {
  outline: 2px solid var(--color-accent);
  outline-offset: 2px;
}

.toggle {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  min-height: var(--touch-target);
}

.toggle input {
  width: 20px;
  height: 20px;
  accent-color: var(--color-primary);
}

.form__error {
  color: var(--color-danger);
  font-size: var(--font-size-xs);
}

.form__actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--space-2);
}
</style>
