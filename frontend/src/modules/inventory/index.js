/** 庫存查詢、購買登錄、用掉／丟棄。 */
export default {
  name: 'inventory',
  routes: [
    {
      path: '/stock',
      name: 'stock',
      component: () => import('./views/StockView.vue'),
      meta: { title: '庫存' },
    },
    {
      path: '/stock/items/:itemId(\\d+)',
      name: 'stock-item',
      component: () => import('./views/StockItemView.vue'),
      props: (route) => ({ itemId: Number(route.params.itemId) }),
      meta: { title: '庫存明細', navGroup: 'stock' },
    },
    {
      path: '/scan',
      name: 'scan',
      component: () => import('./views/ScanView.vue'),
      meta: { title: '掃描' },
    },
    {
      path: '/purchases/new',
      name: 'purchase-new',
      component: () => import('./views/PurchaseFormView.vue'),
      meta: { title: '購買登錄' },
    },
    {
      path: '/purchases',
      name: 'purchases',
      component: () => import('./views/PurchasesView.vue'),
      meta: { title: '購買紀錄', navGroup: 'purchase-new' },
    },
  ],
  nav: [
    { to: { name: 'stock' }, label: '庫存', icon: 'jar', order: 10 },
    // featured：手機底部分頁列正中央的大按鈕
    { to: { name: 'scan' }, label: '掃描', icon: 'scan', order: 12, featured: true },
    { to: { name: 'purchase-new' }, label: '購買', icon: 'cart', order: 15 },
  ],
}
