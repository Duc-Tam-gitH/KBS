import { homeFields } from './homeDefaults'
import { useShop } from './ShopProvider'

const defaults = Object.fromEntries(homeFields.map(field => [field.key, field.defaultValue]))
export const homeText = (overrides: Record<string, string> | undefined, key: string) => overrides?.[key] ?? defaults[key] ?? ''
export function useHomeContent() {
  const { shop } = useShop()
  return (key: string) => homeText(shop?.homeContent, key)
}
