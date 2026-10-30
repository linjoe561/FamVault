/** 登入、初始設定與個人帳號。 */
export default {
  name: 'auth',
  routes: [
    {
      path: '/login',
      name: 'login',
      component: () => import('./views/LoginView.vue'),
      meta: { public: true, layout: 'blank', title: '登入' },
    },
    {
      path: '/setup',
      name: 'setup',
      component: () => import('./views/SetupView.vue'),
      meta: { public: true, layout: 'blank', title: '初始設定' },
    },
    {
      path: '/account',
      name: 'account',
      component: () => import('./views/AccountView.vue'),
      meta: { title: '我的帳號' },
    },
  ],
  nav: [{ to: { name: 'account' }, label: '我的', icon: 'user', order: 100 }],
}
