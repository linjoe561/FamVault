import { http } from '@/shared/api/http'

export const purchasesApi = {
  list: (query) => http.get('/purchases', { params: query }).then((r) => r.data),
  get: (id) => http.get(`/purchases/${id}`).then((r) => r.data),
  create: (form) => http.post('/purchases', form).then((r) => r.data),
  remove: (id) => http.delete(`/purchases/${id}`),
}

export const stockApi = {
  /** @param {{ search?: string, categoryId?: number, locationId?: number, status?: 'all'|'inStock'|'outOfStock'|'low'|'expiring', page?: number, pageSize?: number }} query */
  overview: (query) => http.get('/stock', { params: query }).then((r) => r.data),
  item: (itemId) => http.get(`/stock/items/${itemId}`).then((r) => r.data),
  /** kind: 'consume' 用掉 | 'discard' 丟棄；batchId 留空時先到期先用 */
  consume: (form) => http.post('/stock/consume', form).then((r) => r.data),
  updateBatch: (batchId, form) => http.put(`/stock/batches/${batchId}`, form).then((r) => r.data),
}

export const transactionLabels = {
  purchase: '購買',
  consume: '用掉',
  discard: '丟棄',
  adjust: '調整',
}
