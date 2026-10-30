/** 首頁。之後會放到期、低庫存等儀表板資訊。 */
export default {
  name: 'home',
  routes: [
    {
      path: '/',
      name: 'home',
      component: () => import('./views/HomeView.vue'),
      meta: { title: '首頁' },
    },
  ],
  nav: [{ to: { name: 'home' }, label: '首頁', icon: 'home', order: 0 }],
}
