import assert from 'node:assert/strict'
import { fileURLToPath } from 'node:url'
import { createElement } from 'react'
import { renderToString } from 'react-dom/server'
import { createServer } from 'vite'

const server = await createServer({
  root: fileURLToPath(new URL('../', import.meta.url)),
  server: { middlewareMode: true },
  appType: 'custom',
})

try {
  const { default: App } = await server.ssrLoadModule('/src/App.jsx')
  const { authStore } = await server.ssrLoadModule('/src/services/api.js')
  authStore.clear()
  const guest = renderToString(createElement(App))
  assert.match(guest, /auth-container/)
  assert.match(guest, /name="password"/)
  assert.doesNotMatch(guest, /home-wrapper|liquid-navbar/)
  assert.doesNotMatch(guest, /Quay về cửa hàng/)

  authStore.setSession({ accessToken: 'test-token', user: { roleName: 'Customer', username: 'test' } })
  const html = renderToString(createElement(App))
  assert.match(html, /home-wrapper/)
  assert.match(html, /liquid-navbar/)
  assert.match(html, /Âm thanh/)

  authStore.clear()
  const loggedOut = renderToString(createElement(App))
  assert.match(loggedOut, /auth-container/)
  assert.doesNotMatch(loggedOut, /home-wrapper|liquid-navbar/)

  const { default: Navbar } = await server.ssrLoadModule('/src/components/layout/Navbar.jsx')
  const nav = renderToString(createElement(Navbar, {
    route: 'shop',
    go() {},
    cartCount: 3,
    role: 'staff',
  }))
  assert.match(nav, /Giỏ <b>3<\/b>/)
  assert.doesNotMatch(nav, /Bán hàng|Thông tin cửa hàng/)
  assert.match(nav, /aria-current="page">Khám phá/)
  const { default: Home } = await server.ssrLoadModule('/src/pages/customer/Home.jsx')
  const featured = renderToString(createElement(Home, {
    products: [{ id: 12, name: 'Test keyboard', price: 2500000,
      images: [{ imageUrl: '/keyboard.png', altText: 'Keyboard photo' }] }],
    onNavigate() {}, onAdd() {}, busy: true,
  }))
  assert.match(featured, /Test keyboard/)
  assert.match(featured, /keyboard.png/)
  assert.doesNotMatch(featured, /KBS Minimal Pro|Hình ảnh Bàn phím/)
  const { default: Cart } = await server.ssrLoadModule('/src/pages/customer/Cart.jsx')
  const cart = renderToString(createElement(Cart, {
    cart: [{ id: 12, name: 'Test keyboard', price: 100, quantity: 2 }],
    busy: true, onUpdate() {}, onRemove() {}, onCheckout() {}, onNavigate() {},
  }))
  assert.match(cart, /200₫/)
  assert.match(cart, /disabled=""/)
  assert.doesNotMatch(cart, /href="\/checkout"/)
  console.log('PASS: Guests see login; authenticated users see Home/Navbar; cleared sessions hide Home/Navbar.')
} finally {
  await server.close()
}
