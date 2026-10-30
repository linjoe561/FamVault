/** 家庭成員管理（只有管理者看得到）。 */
export default {
  name: 'members',
  routes: [
    {
      path: '/members',
      name: 'members',
      component: () => import('./views/MembersView.vue'),
      meta: { requiresAdmin: true, title: '家庭成員' },
    },
  ],
  nav: [{ to: { name: 'members' }, label: '成員', icon: 'users', order: 90, adminOnly: true }],
}
