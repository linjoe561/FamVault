import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { categoriesApi, locationsApi, unitsApi } from './api'

/**
 * 分類、位置、單位：很少變動，載入一次後各頁面共用。
 * 在設定頁修改後呼叫 load({ force: true }) 重新載入。
 */
export const useCatalogStore = defineStore('catalog', () => {
  const categories = ref([])
  const locations = ref([])
  const units = ref([])
  let loading = null

  function load({ force = false } = {}) {
    if (loading && !force) return loading
    loading = Promise.all([categoriesApi.list(), locationsApi.list(), unitsApi.list()])
      .then(([c, l, u]) => {
        categories.value = c
        locations.value = l
        units.value = u
      })
      .catch((error) => {
        loading = null
        throw error
      })
    return loading
  }

  /** 大分類，以及各自底下的小分類，方便顯示成兩層。 */
  const categoryTree = computed(() =>
    categories.value
      .filter((c) => c.parentId === null)
      .map((parent) => ({ ...parent, children: categories.value.filter((c) => c.parentId === parent.id) })),
  )

  return { categories, locations, units, categoryTree, load }
})
