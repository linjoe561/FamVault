/**
 * 家庭成員管理（只有管理者看得到）。
 * 手機底部分頁列空間有限，入口放在「我的」頁面，不佔導覽項目。
 */
export default {
  name: 'members',
  routes: [
    {
      path: '/members',
      name: 'members',
      component: () => import('./views/MembersView.vue'),
      meta: { requiresAdmin: true, title: '家庭成員', navGroup: 'account' },
    },
  ],
}
