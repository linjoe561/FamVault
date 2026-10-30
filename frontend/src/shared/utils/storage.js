// localStorage 在無痕模式或被封鎖時可能丟出例外，統一包起來。
export const storage = {
  get(key) {
    try {
      return localStorage.getItem(key)
    } catch {
      return null
    }
  },
  set(key, value) {
    try {
      localStorage.setItem(key, value)
    } catch {
      // 無法保存時，只是下次需要重新登入
    }
  },
  remove(key) {
    try {
      localStorage.removeItem(key)
    } catch {
      // 忽略
    }
  },
}
