/**
 * 後端錯誤一律是 ProblemDetails：
 *   { title, status, detail?, errors?: { fieldName: [message] } }
 * 這裡轉成畫面好用的形狀：message 給整體提示，fieldErrors 給表單欄位。
 */
export class ApiError extends Error {
  constructor({ status, message, fieldErrors = {} }) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.fieldErrors = fieldErrors
  }
}

export function toApiError(error) {
  if (error instanceof ApiError) {
    return error
  }

  if (!error.response) {
    return new ApiError({ status: 0, message: '無法連線到伺服器，請確認後端是否已啟動' })
  }

  const { status, data } = error.response
  const fieldErrors = {}
  for (const [field, messages] of Object.entries(data?.errors ?? {})) {
    fieldErrors[field] = Array.isArray(messages) ? messages[0] : String(messages)
  }

  const message = data?.detail || data?.title || defaultMessage(status)
  return new ApiError({ status, message, fieldErrors })
}

function defaultMessage(status) {
  if (status === 401) return '請重新登入'
  if (status === 403) return '你沒有權限執行這個操作'
  if (status === 404) return '找不到資料'
  return '發生錯誤，請稍後再試'
}
