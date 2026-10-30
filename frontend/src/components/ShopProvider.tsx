import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import { api, API_URL } from '../services/api'

export type PublicShop = {
  isOpen: boolean; homeContent: Record<string, string>
  name: string | null; slogan: string | null; description: string | null
  hotline: string | null; email: string | null; warehouseAddress: string | null
  logoUrl: string | null; bannerUrl: string | null; updatedAt: string | null
}
const ShopContext = createContext<{ shop: PublicShop | null; error: string; loading: boolean }>({
  shop: null, error: '', loading: true,
})
export const useShop = () => useContext(ShopContext)
export const shopImageUrl = (path: string, updatedAt?: string | null) =>
  `${API_URL}${path}?v=${encodeURIComponent(updatedAt || '')}`

export default function ShopProvider({ children }: { children: ReactNode }) {
  const [shop, setShop] = useState<PublicShop | null>(null)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)
  useEffect(() => {
    let controller: AbortController | null = null
    const refresh = () => {
      controller?.abort()
      const next = new AbortController(); controller = next
      api.shop(next.signal).then((data: PublicShop) => {
        if (!next.signal.aborted) { setShop(data); setError(''); setLoading(false) }
      }).catch((err: Error) => {
        if (!next.signal.aborted) { setError(err.message); setLoading(false) }
      })
    }
    refresh()
    window.addEventListener('focus', refresh)
    window.addEventListener('shop-settings-changed', refresh)
    const timer = window.setInterval(() => { if (!document.hidden) refresh() }, 30000)
    return () => {
      controller?.abort(); window.clearInterval(timer)
      window.removeEventListener('focus', refresh)
      window.removeEventListener('shop-settings-changed', refresh)
    }
  }, [])
  return <ShopContext.Provider value={{ shop, error, loading }}>{children}</ShopContext.Provider>
}
