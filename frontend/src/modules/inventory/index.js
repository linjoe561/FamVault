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
    { to: { name: 'purchase-new' }, label: '購買', icon: 'cart', order: 15 },
  ],
}
