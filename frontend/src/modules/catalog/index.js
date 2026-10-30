/** 主檔：品項（含條碼）、分類、存放位置、單位。 */
export default {
  name: 'catalog',
  routes: [
    {
      path: '/items',
      name: 'items',
      component: () => import('./views/ItemsView.vue'),
      meta: { title: '品項' },
    },
    {
      path: '/items/new',
      name: 'item-new',
      component: () => import('./views/ItemFormView.vue'),
      meta: { title: '新增品項', navGroup: 'items' },
    },
    {
      path: '/items/:id(\\d+)',
      name: 'item-edit',
      component: () => import('./views/ItemFormView.vue'),
      props: (route) => ({ id: Number(route.params.id) }),
      meta: { title: '編輯品項', navGroup: 'items' },
    },
    {
      path: '/catalog/settings',
      name: 'catalog-settings',
      component: () => import('./views/CatalogSettingsView.vue'),
      meta: { requiresAdmin: true, title: '分類、位置與單位', navGroup: 'items' },
    },
  ],
  // 手機底部分頁列只放 5 項，品項的入口在手機上改放在「我的」頁面
  nav: [{ to: { name: 'items' }, label: '品項', icon: 'box', order: 20, mobile: false }],
}
