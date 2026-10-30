<script setup>
import { onBeforeUnmount, onMounted, ref } from 'vue'
import { BrowserMultiFormatReader } from '@zxing/browser'
import { BarcodeFormat, DecodeHintType } from '@zxing/library'
import AppIcon from './AppIcon.vue'
import BaseButton from './BaseButton.vue'

/**
 * 用鏡頭掃條碼。掃到後發出 detected(code) 並停止鏡頭。
 *
 * - 優先使用後鏡頭。
 * - 同一組條碼要連續讀到 2 次才算數，避免畫面晃動時誤讀。
 * - 瀏覽器只允許在 HTTPS 或 localhost 使用鏡頭；不能用鏡頭時，可以改用手動輸入。
 */
const emit = defineEmits(['detected'])

const CONFIRM_READS = 2
const FORMATS = [
  BarcodeFormat.EAN_13,
  BarcodeFormat.EAN_8,
  BarcodeFormat.UPC_A,
  BarcodeFormat.UPC_E,
  BarcodeFormat.CODE_128,
  BarcodeFormat.CODE_39,
  BarcodeFormat.ITF,
]

const video = ref(null)
const status = ref('starting') // starting | scanning | error | stopped
const error = ref('')
const manual = ref('')
const torchOn = ref(false)
const canTorch = ref(false)

let controls = null
let lastText = ''
let readCount = 0

async function start() {
  status.value = 'starting'
  error.value = ''
  lastText = ''
  readCount = 0

  if (!window.isSecureContext || !navigator.mediaDevices?.getUserMedia) {
    fail('這個網址不能使用鏡頭。手機請用 https:// 開頭的網址開啟（見使用說明「在手機上使用」）。')
    return
  }

  const hints = new Map([[DecodeHintType.POSSIBLE_FORMATS, FORMATS]])
  const reader = new BrowserMultiFormatReader(hints, { delayBetweenScanAttempts: 120 })
  try {
    controls = await reader.decodeFromConstraints(
      { video: { facingMode: { ideal: 'environment' }, width: { ideal: 1280 }, height: { ideal: 720 } } },
      video.value,
      (result) => {
        if (!result) return
        const text = result.getText().trim()
        readCount = text === lastText ? readCount + 1 : 1
        lastText = text
        if (readCount >= CONFIRM_READS) accept(text)
      },
    )
    canTorch.value = typeof controls.switchTorch === 'function'
    status.value = 'scanning'
  } catch (e) {
    fail(describeCameraError(e))
  }
}

function accept(code) {
  stop()
  navigator.vibrate?.(60)
  emit('detected', code)
}

function stop() {
  controls?.stop()
  controls = null
  torchOn.value = false
  if (status.value !== 'error') status.value = 'stopped'
}

function fail(message) {
  stop()
  status.value = 'error'
  error.value = message
}

async function toggleTorch() {
  if (!controls?.switchTorch) return
  torchOn.value = !torchOn.value
  await controls.switchTorch(torchOn.value).catch(() => (torchOn.value = false))
}

function submitManual() {
  const code = manual.value.trim()
  if (!code) return
  stop()
  emit('detected', code)
}

function describeCameraError(e) {
  switch (e?.name) {
    case 'NotAllowedError':
    case 'SecurityError':
      return '沒有鏡頭權限。請在瀏覽器的網址列或設定中，允許這個網站使用相機，再按「重新開啟鏡頭」。'
    case 'NotFoundError':
    case 'OverconstrainedError':
      return '找不到可以使用的鏡頭。'
    case 'NotReadableError':
      return '鏡頭正被其他 App 使用中，請先關閉其他使用相機的 App。'
    default:
      return '無法開啟鏡頭，可以改用下方手動輸入條碼。'
  }
}

defineExpose({ start, stop })

onMounted(start)
onBeforeUnmount(stop)
</script>

<template>
  <div class="scanner">
    <div class="scanner__viewport" :class="{ 'scanner__viewport--off': status !== 'scanning' }">
      <video ref="video" class="scanner__video" muted playsinline />
      <div v-if="status === 'scanning'" class="scanner__frame" aria-hidden="true">
        <span class="scanner__line" />
      </div>
      <p v-if="status === 'starting'" class="scanner__state">正在開啟鏡頭…</p>
      <div v-else-if="status === 'error'" class="scanner__state scanner__state--error" role="alert">
        <p>{{ error }}</p>
        <BaseButton variant="secondary" @click="start">重新開啟鏡頭</BaseButton>
      </div>
      <div v-else-if="status === 'stopped'" class="scanner__state">
        <BaseButton variant="secondary" @click="start">再掃一次</BaseButton>
      </div>
      <button
        v-if="status === 'scanning' && canTorch"
        type="button"
        class="scanner__torch"
        :aria-pressed="torchOn"
        @click="toggleTorch"
      >
        {{ torchOn ? '關閉手電筒' : '打開手電筒' }}
      </button>
    </div>
    <p v-if="status === 'scanning'" class="scanner__hint">把條碼放進框框裡，對準後會自動讀取</p>

    <form class="scanner__manual" @submit.prevent="submitManual">
      <label class="visually-hidden" for="manual-barcode">手動輸入條碼</label>
      <input
        id="manual-barcode"
        v-model="manual"
        inputmode="numeric"
        autocomplete="off"
        placeholder="掃不到？手動輸入條碼數字"
      />
      <BaseButton type="submit" variant="secondary">
        <AppIcon name="search" :size="18" />
        查詢
      </BaseButton>
    </form>
  </div>
</template>

<style scoped>
.scanner {
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
}

.scanner__viewport {
  position: relative;
  aspect-ratio: 4 / 3;
  overflow: hidden;
  border-radius: var(--radius-md);
  background: var(--color-text);
}

.scanner__viewport--off {
  background: var(--color-surface-muted);
}

.scanner__video {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.scanner__viewport--off .scanner__video {
  visibility: hidden;
}

/* 取景框：中間留空，四周變暗 */
.scanner__frame {
  position: absolute;
  inset: 25% 10%;
  border: 2px solid var(--color-surface);
  border-radius: var(--radius-sm);
  box-shadow: 0 0 0 100vmax rgb(0 0 0 / 35%);
}

.scanner__line {
  position: absolute;
  left: 6%;
  right: 6%;
  top: 50%;
  height: 2px;
  background: var(--color-accent);
  animation: sweep 1.6s ease-in-out infinite alternate;
}

@keyframes sweep {
  from {
    transform: translateY(-300%);
  }
  to {
    transform: translateY(300%);
  }
}

@media (prefers-reduced-motion: reduce) {
  .scanner__line {
    animation: none;
  }
}

.scanner__state {
  position: absolute;
  inset: 0;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: var(--space-3);
  padding: var(--space-5);
  color: var(--color-text-muted);
  text-align: center;
  font-size: var(--font-size-sm);
}

.scanner__state--error {
  color: var(--color-danger);
}

.scanner__torch {
  position: absolute;
  right: var(--space-3);
  bottom: var(--space-3);
  min-height: 36px;
  padding: 0 var(--space-3);
  border: none;
  border-radius: var(--radius-pill);
  background: rgb(0 0 0 / 50%);
  color: var(--color-surface);
  font-size: var(--font-size-xs);
  cursor: pointer;
}

.scanner__hint {
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
  text-align: center;
}

.scanner__manual {
  display: flex;
  gap: var(--space-2);
}

.scanner__manual input {
  flex: 1;
  min-width: 0;
  min-height: var(--touch-target);
  padding: 0 var(--space-3);
  background: var(--color-surface);
  border: 1px solid var(--color-border-strong);
  border-radius: var(--radius-sm);
  font-size: var(--font-size-md);
}

.scanner__manual input:focus {
  border-color: var(--color-primary);
  outline: none;
  box-shadow: 0 0 0 3px var(--color-primary-soft);
}
</style>
