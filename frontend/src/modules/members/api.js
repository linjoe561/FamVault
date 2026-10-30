import { http } from '@/shared/api/http'

export const membersApi = {
  list: () => http.get('/users').then((r) => r.data),
  create: (form) => http.post('/users', form).then((r) => r.data),
  update: (id, form) => http.put(`/users/${id}`, form).then((r) => r.data),
  resetPassword: (id, newPassword) => http.post(`/users/${id}/reset-password`, { newPassword }),
}

export const roleLabels = { Admin: '管理者', Member: '成員' }
