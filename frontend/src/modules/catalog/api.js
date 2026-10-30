import { http } from '@/shared/api/http'

function lookupApi(path) {
  return {
    list: () => http.get(path).then((r) => r.data),
    create: (form) => http.post(path, form).then((r) => r.data),
    update: (id, form) => http.put(`${path}/${id}`, form).then((r) => r.data),
    remove: (id) => http.delete(`${path}/${id}`),
  }
}

export const categoriesApi = lookupApi('/categories')
export const locationsApi = lookupApi('/locations')
export const unitsApi = lookupApi('/units')

export const itemsApi = {
  /** @param {{ search?: string, categoryId?: number, locationId?: number, includeArchived?: boolean, page?: number, pageSize?: number }} query */
  list: (query) => http.get('/items', { params: query }).then((r) => r.data),
  get: (id) => http.get(`/items/${id}`).then((r) => r.data),
  getByBarcode: (code) => http.get(`/items/by-barcode/${encodeURIComponent(code)}`).then((r) => r.data),
  create: (form) => http.post('/items', form).then((r) => r.data),
  update: (id, form) => http.put(`/items/${id}`, form).then((r) => r.data),
  addBarcode: (id, code) => http.post(`/items/${id}/barcodes`, { code }).then((r) => r.data),
  remove: (id) => http.delete(`/items/${id}`),
}
