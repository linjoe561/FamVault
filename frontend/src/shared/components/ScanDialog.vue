<script setup>
import AppDialog from './AppDialog.vue'
import BarcodeScanner from './BarcodeScanner.vue'

/**
 * 在表單中「掃描填入」用的對話框：掃到後回傳條碼並關閉。
 * 鏡頭只在對話框開啟時啟動（關閉時元件卸載，鏡頭跟著關掉）。
 */
defineProps({
  title: { type: String, default: '掃描條碼' },
})
const emit = defineEmits(['detected'])
const open = defineModel('open', { type: Boolean, default: false })

function onDetected(code) {
  emit('detected', code)
  open.value = false
}
</script>

<template>
  <AppDialog v-model:open="open" :title="title">
    <BarcodeScanner v-if="open" @detected="onDetected" />
  </AppDialog>
</template>
