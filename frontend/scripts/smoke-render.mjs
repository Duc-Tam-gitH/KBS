import assert from 'node:assert/strict'
import { readFileSync, readdirSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { createElement } from 'react'
import { renderToString } from 'react-dom/server'
import { createServer } from 'vite'

const root = fileURLToPath(new URL('../', import.meta.url))
const server = await createServer({ root, server: { middlewareMode: true }, appType: 'custom' })
try {
  const { default: App } = await server.ssrLoadModule('/src/App.tsx')
  const html = renderToString(createElement(App))
  assert.match(html, /Feel Every/)
  assert.match(html, /Keystroke/)
  assert.match(html, /KBS/)
  assert.match(html, /aria-pressed="true"/)
  assert.doesNotMatch(html, /TCKR|bg-cream|text-ink|bg-white/)
  const { Detail, Checkout, Admin, Orders } = await server.ssrLoadModule('/src/Storefront.jsx')
  const { default: SearchPage } = await server.ssrLoadModule('/src/pages/customer/Search.jsx')
  const { default: Login } = await server.ssrLoadModule('/src/pages/auth/Login.jsx')
  const { default: Register } = await server.ssrLoadModule('/src/pages/auth/Register.jsx')
  const { default: Cart } = await server.ssrLoadModule('/src/pages/customer/Cart.jsx')
  const { default: Navigation } = await server.ssrLoadModule('/src/components/Navbar.tsx')
  const product = { id: 7, name: 'KBS Test', brand: 'KBS', price: 1200000, category: 'Keyboard' }
  const noop = () => {}
  const { default: StaffWorkspace } = await server.ssrLoadModule('/src/pages/Staff/StaffWorkspace.jsx')
  assert.match(renderToString(createElement(StaffWorkspace, { user: null })), /Không có quyền truy cập/)
  const staffUser = { id: 1, username: 'staff', roleName: 'Staff' }
  assert.match(renderToString(createElement(StaffWorkspace, {
    user: staffUser, onNavigate: noop, onLogout: noop,
  })), /Tổng quan/)
  for (const filename of ['StaffDashboard', 'OrderManager', 'Inventory', 'CustomerManager', 'SupportChat']) {
    const { default: Page } = await server.ssrLoadModule(`/src/pages/Staff/${filename}.jsx`)
    const markup = renderToString(createElement(Page, { user: staffUser, onOpen: noop }))
    assert.match(markup, /Đang tải/)
    assert.doesNotMatch(markup, /MOCK_|Nhân viên A|Nguyễn Văn A|placeholder\.com/)
  }
  const staffDirectory = new URL('../src/pages/Staff/', import.meta.url)
  for (const filename of readdirSync(staffDirectory).filter((name) => name.endsWith('.css'))) {
    assert.doesNotMatch(readFileSync(new URL(filename, staffDirectory), 'utf8'), /#[0-9a-f]{3,8}\b|rgba?\(/i)
  }
  const search = renderToString(createElement(SearchPage, { onSelect: noop, onAdd: noop }))
  assert.match(search, /search-sidebar/)
  assert.match(search, /name="search"/)
  assert.match(search, /name="categoryId"/)
  assert.match(search, /name="brandId"/)
  assert.match(search, /name="minPrice"/)
  assert.match(search, /name="maxPrice"/)
  assert.match(search, /Đang tìm sản phẩm/)
  const searchNav = renderToString(createElement(Navigation, {
    route: 'search', onNavigate: noop, cartCount: 0, user: null, role: 'customer', onLogout: noop,
  }))
  assert.doesNotMatch(searchNav, /aria-label="Tìm kiếm"/)
  assert.match(searchNav, /aria-label="Sản phẩm yêu thích"/)
  assert.doesNotMatch(searchNav, /<form/)
  const detail = renderToString(createElement(Detail, { product, onBack: noop, onAdd: noop, busy: true }))
  assert.match(detail, /Đang tải chi tiết sản phẩm/)
  assert.doesNotMatch(detail, /KBS Test/)
  assert.match(renderToString(createElement(Detail, { onBack: noop })), /Không tìm thấy sản phẩm/)
  assert.match(renderToString(createElement(Login, { onLogin: noop, onNavigate: noop })), /name="password"/)
  assert.match(renderToString(createElement(Register, { onRegister: noop, onNavigate: noop })), /confirmPassword/)
  const cart = [{ ...product, quantity: 2 }]
  assert.match(renderToString(createElement(Cart, { cart, busy: false, onUpdate: noop,
    onRemove: noop, onCheckout: noop, onNavigate: noop })), /2.400.000₫/)
  assert.match(renderToString(createElement(Checkout, { cart, onPlaced: noop, onDone: noop })), /name="address"/)
  assert.match(renderToString(createElement(Checkout, { cart: [], onPlaced: noop, onDone: noop })), /Không có sản phẩm/)
  assert.match(renderToString(createElement(Orders)), /Đăng nhập để xem đơn hàng/)
  assert.match(renderToString(createElement(Orders, { user: { id: 1 } })), /Đang tải đơn hàng/)
  assert.match(renderToString(createElement(Admin)), /Đang tổng hợp/)
  const navProps = { route: 'shop', onNavigate: noop, onSearch: noop, cartCount: 2,
    user: { username: 'test' }, role: 'customer', onLogout: noop }
  const customerNav = renderToString(createElement(Navigation, navProps))
  assert.match(customerNav, /Giỏ hàng: 2 sản phẩm/)
  assert.doesNotMatch(customerNav, /Quản trị/)
  assert.match(renderToString(createElement(Navigation, { ...navProps, role: 'admin' })), /Quản trị/)
  const { api, authStore } = await server.ssrLoadModule('/src/services/api.js')
  authStore.setSession({ accessToken: 'test', user: { username: 'admin', roleName: 'Admin' } })
  assert.match(renderToString(createElement(App)), /Quản trị/)
  authStore.clear()
  assert.doesNotMatch(renderToString(createElement(App)), /Quản trị/)
  const originalFetch = globalThis.fetch
  const requests = []
  try {
    globalThis.fetch = async (url, options) => {
      requests.push({ url: String(url), ...options })
      if (String(url).endsWith('/api/auth/login')) {
        return Response.json({ accessToken: 'customer-token', user: { username: 'test', roleName: 'Customer' } })
      }
      if (options.method === 'DELETE') return new Response(null, { status: 204 })
      return Response.json({ items: [] })
    }
    await api.products({ page: 1, pageSize: 100 })
    assert.equal(requests.at(-1).headers.get('Authorization'), null)
    await api.product(7)
    assert.ok(requests.at(-1).url.endsWith('/api/products/7'))
    await api.productReviews(7, { rating: 5, page: 2 })
    assert.ok(requests.at(-1).url.endsWith('/api/products/7/reviews?rating=5&page=2'))
    await api.register({ username: 'test', email: 'test@example.com', password: 'Example123' })
    assert.equal(requests.at(-1).method, 'POST')
    await api.login({ loginIdentifier: 'test', password: 'Example123' })
    assert.equal(authStore.user.username, 'test')
    await api.confirmOrderReceipt(7)
    assert.ok(requests.at(-1).url.endsWith('/api/orders/7/receipt'))
    assert.equal(requests.at(-1).method, 'PUT')
    assert.equal(requests.at(-1).headers.get('Authorization'), 'Bearer customer-token')
    await api.addProductReview(7, { rating: 5, comment: 'Good keyboard' })
    assert.ok(requests.at(-1).url.endsWith('/api/products/7/reviews'))
    assert.equal(requests.at(-1).method, 'POST')
    assert.equal(requests.at(-1).headers.get('Authorization'), 'Bearer customer-token')
    assert.deepEqual(JSON.parse(requests.at(-1).body), { rating: 5, comment: 'Good keyboard' })
    await api.addCartItem(7, 2)
    assert.equal(requests.at(-1).headers.get('Authorization'), 'Bearer customer-token')
    assert.equal(requests.at(-1).method, 'POST')
    assert.deepEqual(JSON.parse(requests.at(-1).body), { productId: 7, quantity: 2 })
    await api.updateCartItem(7, 3)
    assert.equal(requests.at(-1).method, 'PUT')
    assert.deepEqual(JSON.parse(requests.at(-1).body), { quantity: 3 })
    await api.removeCartItem(7)
    assert.equal(requests.at(-1).method, 'DELETE')
    const order = { customerName: 'Test', phoneNumber: '0901000001', shippingAddress: 'Test address',
      paymentMethod: 'COD', orderItems: [{ productId: 7, quantity: 2 }] }
    await api.createOrder(order)
    assert.ok(requests.at(-1).url.endsWith('/api/orders'))
    assert.deepEqual(JSON.parse(requests.at(-1).body), order)
    let notifications = 0
    const unsubscribe = authStore.subscribe(() => notifications++)
    globalThis.fetch = async () => Response.json({ message: 'Expired' }, { status: 401 })
    await assert.rejects(api.cart(), /Expired/)
    assert.equal(authStore.user, null)
    assert.equal(notifications, 1)
    unsubscribe()
    // Phản hồi đến muộn từ phiên cũ không được đăng xuất phiên đăng nhập mới.
    authStore.setSession({ accessToken: 'old-token', user: { username: 'old' } })
    let resolveOld
    globalThis.fetch = () => new Promise(resolve => { resolveOld = resolve })
    const oldRequest = api.cart()
    authStore.setSession({ accessToken: 'new-token', user: { username: 'new' } })
    resolveOld(Response.json({ message: 'Old session' }, { status: 401 }))
    await assert.rejects(oldRequest, /Old session/)
    assert.equal(authStore.user.username, 'new')
  } finally {
    globalThis.fetch = originalFetch
    authStore.clear()
  }
  const css = readFileSync(new URL('../src/index.css', import.meta.url), 'utf8')
  const palette = ['#050512', '#121226', '#2D2E54', '#FF2A6D', '#05D9E8', '#8A9BB3', '#E0F2FE']
  assert.deepEqual([...css.matchAll(/#[0-9a-f]{6}/gi)].map(m => m[0].toUpperCase()), palette)
  const luminance = hex => {
    const rgb = hex.slice(1).match(/../g).map(x => parseInt(x, 16) / 255)
      .map(v => v <= 0.04045 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4)
    return rgb[0] * 0.2126 + rgb[1] * 0.7152 + rgb[2] * 0.0722
  }
  // Tech White (#E0F2FE) on dark backgrounds (#050512, #121226, #2D2E54)
  for (const background of palette.slice(0, 3)) {
    assert.ok((luminance('#E0F2FE') + 0.05) / (luminance(background) + 0.05) >= 4.5)
  }
  // Steel Gray (#8A9BB3) on Void Black and Cyber Metal
  for (const background of palette.slice(0, 2)) {
    assert.ok((luminance('#8A9BB3') + 0.05) / (luminance(background) + 0.05) >= 4.5)
  }
  // Void Black (#050512) on neon buttons (#FF2A6D, #05D9E8)
  for (const neon of ['#FF2A6D', '#05D9E8']) {
    assert.ok((luminance(neon) + 0.05) / (luminance('#050512') + 0.05) >= 4.5)
  }
  console.log('PASS: Landing, auth, catalog, cart, checkout, orders and admin render; role navigation and palette checks pass.')
} finally { await server.close() }
