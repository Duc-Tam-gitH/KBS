import assert from 'node:assert/strict'
import { createElement } from 'react'
import { renderToString } from 'react-dom/server'
import { createServer } from 'vite'

const server = await createServer({ server: { middlewareMode: true }, appType: 'custom' })
const originalFetch = globalThis.fetch
try {
  const { default: Likelist, normalizeItem } = await server.ssrLoadModule('/src/pages/customer/Likelist.jsx')
  const html = renderToString(createElement(Likelist))
  assert.match(html, /Đăng nhập để xem/)
  assert.doesNotMatch(html, /KBS-75 Pro|KBS Alice|Tải mẫu/)
  const product = { productId: 3, productName: 'Keyboard', price: 100, isActive: true, stockQuantity: 2 }
  assert.equal(normalizeItem(product).inStock, true)
  assert.equal(normalizeItem({ ...product, isActive: false }).inStock, false)
  assert.equal(normalizeItem({ ...product, stockQuantity: 0 }).inStock, false)
  assert.equal(normalizeItem(product).id, 3)
  const { api, authStore } = await server.ssrLoadModule('/src/services/api.js')
  authStore.setSession({ accessToken: 'test-token', user: { id: 1 } })
  const requests = []
  globalThis.fetch = async (url, options) => {
    requests.push({ url: String(url), ...options })
    return options.method ? new Response(null, { status: 204 }) : Response.json([])
  }
  assert.deepEqual(await api.likelist(), [])
  await api.addLikeItem(3)
  await api.removeLikeItem(3)
  assert.ok(requests[0].url.endsWith('/api/likelist'))
  assert.ok(requests[1].url.endsWith('/api/likelist/items/3'))
  assert.equal(requests[1].method, 'PUT')
  assert.equal(requests[2].method, 'DELETE')
  assert.equal(requests[1].body, undefined)
  assert.equal(requests[0].headers.get('Authorization'), 'Bearer test-token')
  globalThis.fetch = async () => Response.json({ message: 'Lỗi kiểm thử' }, { status: 500 })
  await assert.rejects(api.likelist(), /Lỗi kiểm thử/)
  authStore.clear()
  console.log('Likelist checks passed')
} finally {
  globalThis.fetch = originalFetch
  await server.close()
}
