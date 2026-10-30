import auth from '@/modules/auth'
import catalog from '@/modules/catalog'
import home from '@/modules/home'
import members from '@/modules/members'

/**
 * 啟用中的前端模組。新增模組時：
 * 1. 在 src/modules/<名稱>/index.js 匯出 { name, routes, nav }
 * 2. 在這裡加入一行 import 並放進陣列
 */
export const modules = [home, catalog, members, auth]

export const routes = modules.flatMap((m) => m.routes ?? [])

/** 導覽項目依 order 排序；adminOnly 的項目只有管理者看得到。 */
export const navItems = modules.flatMap((m) => m.nav ?? []).sort((a, b) => a.order - b.order)
