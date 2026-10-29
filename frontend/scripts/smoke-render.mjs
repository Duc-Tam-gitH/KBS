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
  const { Detail, Admin, Orders } = await server.ssrLoadModule('/src/Storefront.jsx')
  const { default: Checkout } = await server.ssrLoadModule('/src/pages/customer/Checkout.jsx')
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
  assert.match(renderToString(createElement(StaffWorkspace, {
    user: staffUser, onNavigate: noop, onLogout: noop,
  })), />AI<\/button>/)
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
  const checkoutMarkup = renderToString(createElement(Checkout, { cart, onPlaced: noop, onDone: noop }))
  assert.match(checkoutMarkup, /co-summary/)
  assert.match(checkoutMarkup, /2.400.000₫/)
  assert.match(checkoutMarkup, /type="radio" name="payment"/)
  assert.doesNotMatch(checkoutMarkup, /GIAM50K|ZaloPay|Nguyễn Văn A|Đã bao gồm VAT/)
  for (const name of ['province', 'district', 'ward']) {
    assert.match(checkoutMarkup, new RegExp('<select name="' + name + '"[^>]*disabled'))
  }
  assert.equal((checkoutMarkup.match(/name="payment"/g) || []).length, 2)
  const checkoutLoading = renderToString(createElement(Checkout, { cart, busy: true }))
  assert.match(checkoutLoading, /Đang tải giỏ hàng/)
  assert.doesNotMatch(checkoutLoading, /type="submit"/)
  const checkoutError = renderToString(createElement(Checkout, { cart, cartError: 'Không thể tải giỏ hàng' }))
  assert.match(checkoutError, /role="alert"/)
  assert.doesNotMatch(checkoutError, /type="submit"/)
  assert.match(renderToString(createElement(Orders)), /Đăng nhập để xem đơn hàng/)
  assert.match(renderToString(createElement(Orders, { user: { id: 1 } })), /Đang tải đơn hàng/)
  assert.match(renderToString(createElement(Admin)), /Không có quyền truy cập/)
  const adminMarkup = renderToString(createElement(Admin, { user: { roleName: 'Admin' } }))
  assert.match(adminMarkup, /Đang tổng hợp/)
  assert.match(adminMarkup, /Xin chào/)
  assert.match(adminMarkup, /ad-sidebar/)
  assert.match(adminMarkup, /ad-details/)
  assert.doesNotMatch(adminMarkup, /Về cửa hàng/)
  for (const label of ['Dashboard', 'Quản lý tài khoản', 'Quản lý sản phẩm', 'Quản lý danh mục',
    'Quản lý kho', 'Quản lý cửa hàng', 'Quản lý AI']) {
    assert.ok(adminMarkup.includes(label), 'Thiếu trang quản trị: ' + label)
  }
  const { default: AccountManagement } = await server.ssrLoadModule('/src/pages/admin/AccountManagement.jsx')
  assert.match(renderToString(createElement(AccountManagement)), /Đang tải tài khoản/)
  const { default: InventoryManagement } = await server.ssrLoadModule('/src/pages/admin/InventoryManagement.jsx')
  assert.match(renderToString(createElement(InventoryManagement)), /Đang tải tồn kho/)
  for (const filename of ['ShopManagement']) {
    const { default: Page } = await server.ssrLoadModule('/src/pages/admin/' + filename + '.jsx')
    assert.match(renderToString(createElement(Page)), /chưa được triển khai/)
  }
  const { default: AIManagement } = await server.ssrLoadModule('/src/pages/admin/AIManagement.jsx')
  const aiMarkup = renderToString(createElement(AIManagement))
  assert.match(aiMarkup, /Đang tải danh sách AI/)
  assert.match(aiMarkup, /ALL AI/)
  assert.match(aiMarkup, /Thông tin AI được chọn/)
  assert.match(aiMarkup, /Chọn một AI/)
  assert.doesNotMatch(aiMarkup, /chưa được triển khai/)
  const staffAiMarkup = renderToString(createElement(AIManagement, { readOnly: true }))
  assert.match(staffAiMarkup, /Theo dõi AI/)
  assert.match(staffAiMarkup, /Chế độ chỉ đọc/)
  assert.match(staffAiMarkup, /ALL AI/)
  assert.match(staffAiMarkup, /Thông tin AI được chọn/)
  assert.doesNotMatch(staffAiMarkup, /role="switch"/)
  assert.doesNotMatch(readFileSync(new URL('../src/pages/admin/AIManagement.css', import.meta.url), 'utf8'),
    /#[0-9a-f]{3,8}\b|rgba?\(/i)
  const { default: CategoryManagement, CategoryForm } =
    await server.ssrLoadModule('/src/pages/admin/CategoryManagement.jsx')
  const { default: ProductManagement, ProductTable } =
    await server.ssrLoadModule('/src/pages/admin/ProductManagement.jsx')
  const { filterProducts } = await server.ssrLoadModule('/src/pages/admin/filterProducts.js')
  const { default: ProductForm } = await server.ssrLoadModule('/src/pages/admin/ProductForm.jsx')
  assert.match(renderToString(createElement(ProductManagement)), /Đang tải sản phẩm/)
  assert.match(renderToString(createElement(ProductTable, { products: [] })), /Chưa có sản phẩm nào/)
  assert.match(renderToString(createElement(ProductTable, { products: [], hasFilters: true })), /Không tìm thấy/)
  assert.deepEqual(filterProducts([], '', '', ''), [])
  // Verify filtering against existing DB-shaped records supplied by the service contract.
  const filterRecords = [{ id: 1, name: 'Alpha', categoryId: 2, isActive: true },
    { id: 2, name: 'Beta', categoryId: 3, isActive: false }]
  assert.deepEqual(filterProducts(filterRecords, ' ALPHA ', '2', 'active'), [filterRecords[0]])
  assert.deepEqual(filterProducts(filterRecords, '#2', '', 'inactive'), [filterRecords[1]])
  assert.deepEqual(filterProducts(filterRecords, 'Alpha', '', 'inactive'), [])
  const productForm = renderToString(createElement(ProductForm, { categories: [], brands: [], onSave: noop }))
  assert.match(productForm, /name="brandId"/)
  assert.match(productForm, /name="price"/)
  assert.match(productForm, /Cần có danh mục và thương hiệu/)
  assert.doesNotMatch(productForm, /SKU|iPhone|placeholder\.com/)
  assert.doesNotMatch(readFileSync(new URL('../src/pages/admin/ProductManagement.css', import.meta.url), 'utf8'),
    /#[0-9a-f]{3,8}\b|rgba?\(/i)
  assert.match(renderToString(createElement(CategoryManagement)), /Đang tải danh mục/)
  const categoryForm = renderToString(createElement(CategoryForm, { onSave: noop, onCancel: noop }))
  assert.match(categoryForm, /name="name"/)
  assert.match(categoryForm, /name="description"/)
  assert.match(categoryForm, /name="isActive"/)
  const { DashboardContent, DashboardDetails } = await server.ssrLoadModule('/src/pages/admin/Dashboard.jsx')
  assert.match(renderToString(createElement(DashboardContent, { tab: 'orders', data: [] })), /Chưa có đơn hàng/)
  assert.match(renderToString(createElement(DashboardContent, {
    tab: 'revenue', data: { revenueLast7Days: [] },
  })), /Chưa có dữ liệu trong 7 ngày/)
  assert.match(renderToString(createElement(DashboardDetails)), /Chọn một chỉ số/)
  const adminDirectory = new URL('../src/pages/admin/', import.meta.url)
  assert.doesNotMatch(readFileSync(new URL('Dashboard.css', adminDirectory), 'utf8'), /#[0-9a-f]{3,8}\b|rgba?\(/i)
  assert.doesNotMatch(readFileSync(new URL('Dashboard.jsx', adminDirectory), 'utf8'),
    /Codexr|Sage|placeholder|Anna Jones|Carlos Brown|Joel Carrier/)
  const navProps = { route: 'shop', onNavigate: noop, onSearch: noop, cartCount: 2,
    user: { username: 'test' }, role: 'customer', onLogout: noop }
  const customerNav = renderToString(createElement(Navigation, navProps))
  assert.match(customerNav, /Giỏ hàng: 2 sản phẩm/)
  assert.doesNotMatch(customerNav, /Quản trị/)
  assert.match(renderToString(createElement(Navigation, { ...navProps, role: 'admin' })), /Quản trị/)
  const staffNav = renderToString(createElement(Navigation, { ...navProps, role: 'staff' }))
  assert.doesNotMatch(staffNav, /Giỏ hàng|Sản phẩm yêu thích|Đơn hàng/)
  assert.match(staffNav, /Nhân viên/)
  assert.match(staffNav, /Tài khoản/)
  for (const role of ['customer', 'admin']) {
    const markup = renderToString(createElement(Navigation, { ...navProps, role }))
    assert.match(markup, /Giỏ hàng: 2 sản phẩm/)
    assert.match(markup, /Sản phẩm yêu thích/)
    assert.match(markup, /Đơn hàng/)
  }
  const { api, authStore } = await server.ssrLoadModule('/src/services/api.js')
  authStore.setSession({ accessToken: 'test', user: { username: 'admin', roleName: 'Admin' } })
  const adminApp = renderToString(createElement(App))
  assert.match(adminApp, /Khu vực quản trị/)
  assert.match(adminApp, /Đang tổng hợp/)
  assert.doesNotMatch(adminApp, /Feel Every|Keystroke|Giỏ hàng|Về cửa hàng|Chức năng nhân viên|<footer/)
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
    const controller = new AbortController()
    await api.admin.dashboard(controller.signal)
    assert.ok(requests.at(-1).url.endsWith('/api/admin/dashboard'))
    assert.equal(requests.at(-1).signal, controller.signal)
    assert.equal(requests.at(-1).headers.get('Authorization'), 'Bearer customer-token')
    await api.admin.orders(controller.signal)
    assert.ok(requests.at(-1).url.endsWith('/api/orders'))
    assert.equal(requests.at(-1).signal, controller.signal)
    await api.admin.categories(controller.signal)
    assert.ok(requests.at(-1).url.endsWith('/api/categories'))
    assert.equal(requests.at(-1).signal, controller.signal)
    await api.admin.products(controller.signal)
    assert.ok(requests.at(-1).url.endsWith('/api/products/management'))
    assert.equal(requests.at(-1).signal, controller.signal)
    await api.admin.createCategory({ name: '', description: null, isActive: false })
    assert.equal(requests.at(-1).method, 'POST')
    assert.ok(requests.at(-1).url.endsWith('/api/categories'))
    assert.equal(JSON.parse(requests.at(-1).body).isActive, false)
    await api.admin.updateCategory(7, { name: '', isActive: true })
    assert.equal(requests.at(-1).method, 'PUT')
    assert.ok(requests.at(-1).url.endsWith('/api/categories/7'))
    await api.admin.deleteCategory(7)
    assert.equal(requests.at(-1).method, 'DELETE')
    assert.ok(requests.at(-1).url.endsWith('/api/categories/7'))
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
    await api.paymentOptions(controller.signal)
    assert.ok(requests.at(-1).url.endsWith('/api/payments/options'))
    assert.equal(requests.at(-1).signal, controller.signal)
    await api.createPayment(7)
    assert.ok(requests.at(-1).url.endsWith('/api/payments/orders/7'))
    assert.equal(requests.at(-1).method, 'POST')
    assert.equal(requests.at(-1).body, undefined)
    await api.confirmBankPayment(7)
    assert.ok(requests.at(-1).url.endsWith('/api/payments/orders/7/bank-confirmation'))
    assert.equal(requests.at(-1).method, 'PUT')
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
    const passwordError = 'Mật khẩu phải có ít nhất 8 ký tự, gồm chữ và số.'
    globalThis.fetch = async () => Response.json({
      message: 'Vui lòng kiểm tra lại dữ liệu nhập.',
      errors: { Password: [passwordError] },
    }, { status: 400 })
    await assert.rejects(api.register({}), { message: passwordError, status: 400 })
    globalThis.fetch = async () => Response.json({ message: 'Email đã tồn tại.' }, { status: 400 })
    await assert.rejects(api.register({}), { message: 'Email đã tồn tại.', status: 400 })
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
