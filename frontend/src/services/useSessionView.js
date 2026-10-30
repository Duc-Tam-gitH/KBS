import { useEffect, useState } from 'react'
import { authStore } from './api'

// Only navigation/filter state is saved; pages still fetch fresh data on mount.
export function useSessionView(name, initial, valid = () => true) {
  const owner = authStore.user?.id ?? authStore.user?.username ?? 'guest'
  const key = `kbs.view.${owner}.${name}`
  const [value, setValue] = useState(() => {
    try {
      const stored = JSON.parse(sessionStorage.getItem(key))
      if (stored !== null && valid(stored)) return stored
    } catch { /* Missing, corrupt or blocked storage. */ }
    return typeof initial === 'function' ? initial() : initial
  })
  useEffect(() => {
    try { sessionStorage.setItem(key, JSON.stringify(value)) } catch { /* Keep in-memory navigation. */ }
  }, [key, value])
  return [value, setValue]
}
