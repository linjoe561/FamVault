const WEEKDAYS = ['日', '一', '二', '三', '四', '五', '六']

/** 數量去掉多餘的 0：1.500 → 1.5、2.000 → 2 */
export function formatQuantity(value) {
  if (value === null || value === undefined || value === '') return ''
  return String(Number(Number(value).toFixed(3)))
}

/** 金額：1234.5 → $1,234.5 */
export function formatMoney(value) {
  if (value === null || value === undefined) return ''
  return `$${Number(value).toLocaleString('zh-TW', { maximumFractionDigits: 2 })}`
}

/** 今天（本地時區）的 YYYY-MM-DD，給 <input type="date"> 用 */
export function todayIso() {
  return toIsoDate(new Date())
}

export function toIsoDate(date) {
  const y = date.getFullYear()
  const m = String(date.getMonth() + 1).padStart(2, '0')
  const d = String(date.getDate()).padStart(2, '0')
  return `${y}-${m}-${d}`
}

/** YYYY-MM-DD 加減天數 */
export function addDays(iso, days) {
  const [y, m, d] = iso.split('-').map(Number)
  return toIsoDate(new Date(y, m - 1, d + days))
}

/** 兩個 YYYY-MM-DD 相差幾天（to - from） */
export function daysBetween(fromIso, toIso) {
  const [y1, m1, d1] = fromIso.split('-').map(Number)
  const [y2, m2, d2] = toIso.split('-').map(Number)
  return Math.round((Date.UTC(y2, m2 - 1, d2) - Date.UTC(y1, m1 - 1, d1)) / 86400000)
}

/** 2026-10-30 → 10/30（五）；不同年份時加上年份 */
export function formatDate(iso) {
  if (!iso) return ''
  const [y, m, d] = iso.split('-').map(Number)
  const weekday = WEEKDAYS[new Date(y, m - 1, d).getDay()]
  const prefix = y === new Date().getFullYear() ? '' : `${y}/`
  return `${prefix}${m}/${d}（${weekday}）`
}

/** 異動時間：今天 14:05、昨天 09:30、10/28 18:00 */
export function formatDateTime(value) {
  const date = new Date(value)
  const time = date.toLocaleTimeString('zh-TW', { hour: '2-digit', minute: '2-digit', hour12: false })
  const diff = daysBetween(toIsoDate(date), todayIso())
  if (diff === 0) return `今天 ${time}`
  if (diff === 1) return `昨天 ${time}`
  return `${date.getMonth() + 1}/${date.getDate()} ${time}`
}

/** 到期日的白話說明：已過期 2 天、今天到期、明天到期、還有 5 天 */
export function describeExpiry(iso) {
  if (!iso) return '沒有期限'
  const days = daysBetween(todayIso(), iso)
  if (days < 0) return `已過期 ${-days} 天`
  if (days === 0) return '今天到期'
  if (days === 1) return '明天到期'
  if (days <= 30) return `還有 ${days} 天`
  return `${formatDate(iso)} 到期`
}
