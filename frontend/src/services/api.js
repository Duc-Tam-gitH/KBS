const API_URL = (import.meta.env.VITE_API_URL || 'https://localhost:7013').replace(/\/$/, '')

// Access token chỉ nằm trong bộ nhớ của tab. Không dùng localStorage để tránh
// token tồn tại lâu và bị đọc bởi script nếu trang gặp XSS.
let accessToken = null
let currentUser = null
const listeners = new Set()
const notify = () => listeners.forEach(listener => listener())

export const authStore = {
  subscribe(listener) { listeners.add(listener); return () => listeners.delete(listener) },
  get token() { return accessToken },
  get user() { return currentUser },
  setSession(session) { accessToken = session.accessToken; currentUser = session.user; notify() },
  clear() { accessToken = null; currentUser = null; notify() },
  hasRole(role) { return currentUser?.roleName === role },
}

export class ApiError extends Error {
  constructor(message, status) { super(message); this.status = status }
}

async function request(path, options = {}) {
  const headers = new Headers(options.headers)
  const multipart = options.body instanceof FormData
  headers.set('Accept', 'application/json')
  if (options.body !== undefined && !multipart) headers.set('Content-Type', 'application/json')
  if (accessToken) headers.set('Authorization', `Bearer ${accessToken}`)
  const requestToken = accessToken
  const response = await fetch(`${API_URL}${path}`, { ...options, headers,
    body: multipart ? options.body : options.body === undefined ? undefined : JSON.stringify(options.body) })
  if (response.status === 401 && requestToken && requestToken === accessToken) authStore.clear()
  if (!response.ok) {
    const payload = await response.json().catch(() => null)
    const validationMessage = Object.values(payload?.errors || {}).flat().join(' ')
    throw new ApiError(
      validationMessage || payload?.message || 'Không thể kết nối đến máy chủ.',
      response.status,
    )
  }
  return response.status === 204 ? null : response.json()
}

export const api = {
  shop: (signal) => request('/api/shop', { signal }),
  login: async (credentials) => { const session = await request('/api/auth/login', { method: 'POST', body: credentials }); authStore.setSession(session); return session },
  register: (payload) => request('/api/auth/register', { method: 'POST', body: payload }),
  products: (params = {}) => request(`/api/products?${new URLSearchParams(params)}`),
  product: (id) => request(`/api/products/${id}`),
  productReviews: (id, params = {}) => request(`/api/products/${id}/reviews?${new URLSearchParams(params)}`),
  addProductReview: (id, payload) => request(`/api/products/${id}/reviews`, { method: 'POST', body: payload }),
  brands: () => request('/api/brands'),
  categories: () => request('/api/categories'),
  myOrders: () => request('/api/orders/mine'),
  confirmOrderReceipt: (id) => request(`/api/orders/${id}/receipt`, { method: 'PUT' }),
  staff: {
    get: (resource) => request(`/api/staff/${resource}`),
    orders: () => request('/api/orders'),
    orderStatus: (id, orderStatus) => request(`/api/orders/${id}/status`, {
      method: 'PUT', body: { orderStatus },
    }),
    tracking: (id) => request(`/api/staff/orders/${id}/tracking`, {
      method: 'PUT',
    }),
    note: (id, content) => request(`/api/staff/orders/${id}/notes`, { method: 'POST', body: { content } }),
    adjustStock: (id, payload) => request(`/api/staff/inventory/${id}/adjustments`, {
      method: 'POST', body: payload,
    }),
    threshold: (id, lowStockThreshold) => request(`/api/staff/inventory/${id}/threshold`, {
      method: 'PUT', body: { lowStockThreshold },
    }),
    customerStatus: (id, isActive) => request(`/api/staff/customers/${id}/status`, {
      method: 'PUT', body: { isActive },
    }),
  },
  chat: {
    conversations: () => request('/api/chat/conversations'),
    open: (customerId) => request(`/api/chat/conversations${customerId ? `?customerId=${customerId}` : ''}`, {
      method: 'POST',
    }),
    claim: (id) => request(`/api/chat/conversations/${id}/claim`, { method: 'PUT' }),
    messages: (id, beforeId, afterId) => request(`/api/chat/conversations/${id}/messages?${
      new URLSearchParams({ ...(beforeId ? { beforeId } : {}), ...(afterId ? { afterId } : {}) })}`),
    send: (id, content) => request(`/api/chat/conversations/${id}/messages`, {
      method: 'POST', body: { content },
    }),
    read: (id, throughMessageId) => request(`/api/chat/conversations/${id}/read`, {
      method: 'PUT', body: { throughMessageId },
    }),
  },
  createOrder: (payload) => request('/api/orders', { method: 'POST', body: payload }),
  cancelOrder: (id) => request(`/api/orders/${id}/cancel`, { method: 'PUT' }),
  paymentOptions: (signal) => request('/api/payments/options', { signal }),
  createPayment: (id) => request(`/api/payments/orders/${id}`, { method: 'POST' }),
  paymentStatus: (id, signal) => request(`/api/payments/orders/${id}`, { signal }),
  confirmBankPayment: (id) => request(`/api/payments/orders/${id}/bank-confirmation`, { method: 'PUT' }),
  cart: () => request('/api/cart'),
  addCartItem: (productId, quantity) =>
    request('/api/cart/items', { method: 'POST', body: { productId, quantity } }),
  updateCartItem: (productId, quantity) =>
    request(`/api/cart/items/${productId}`, {
      method: 'PUT',
      body: { quantity },
    }),
  removeCartItem: (productId) =>
    request(`/api/cart/items/${productId}`, { method: 'DELETE' }),
  likelist: () => request('/api/likelist'),
  addLikeItem: (productId) =>
    request(`/api/likelist/items/${productId}`, { method: 'PUT' }),
  removeLikeItem: (productId) =>
    request(`/api/likelist/items/${productId}`, { method: 'DELETE' }),
  admin: {
    shopAvailability: (isOpen) => request('/api/admin/shop/availability', { method: 'PUT', body: { isOpen } }),
    shop: (signal) => request('/api/admin/shop', { signal }),
    saveShop: (body) => request('/api/admin/shop', { method: 'PUT', body }),
    uploadShopImage: (kind, file) => {
      const body = new FormData(); body.append('file', file)
      return request(`/api/admin/shop/media/${kind}`, { method: 'PUT', body })
    },
    deleteShopImage: (kind) => request(`/api/admin/shop/media/${kind}`, { method: 'DELETE' }),
    users: (signal) => request('/api/users', { signal }),
    userRoles: (signal) => request('/api/users/roles', { signal }),
    createUser: (body) => request('/api/users', { method: 'POST', body }),
    saveUser: (id, body) => request(`/api/users/${id}`, { method: 'PUT', body }),
    userStatus: (id, isActive) => request(`/api/users/${id}/status`, { method: 'PUT', body: { isActive } }),
    deleteUser: (id) => request(`/api/users/${id}`, { method: 'DELETE' }),
    aiModules: (signal) => request('/api/admin/ai/modules', { signal }),
    aiEnabled: (id, isEnabled) => request(`/api/admin/ai/modules/${encodeURIComponent(id)}/enabled`, {
      method: 'PUT', body: { isEnabled },
    }),
    aiEvents: (moduleId, beforeId, signal) => request(`/api/admin/ai/events?${new URLSearchParams({
      ...(moduleId ? { moduleId } : {}), ...(beforeId ? { beforeId } : {}), limit: 50,
    })}`, { signal }),
    products: (signal) => request('/api/products/management', { signal }),
    categories: (signal) => request('/api/categories', { signal }),
    createCategory: (payload) => request('/api/categories', { method: 'POST', body: payload }),
    updateCategory: (id, payload) => request(`/api/categories/${id}`, { method: 'PUT', body: payload }),
    deleteCategory: (id) => request(`/api/categories/${id}`, { method: 'DELETE' }),
    dashboard: (signal) => request('/api/admin/dashboard', { signal }),
    orders: (signal) => request('/api/orders', { signal }),
    updateOrderStatus: (id, orderStatus) => request(`/api/orders/${id}/status`, { method: 'PUT', body: { orderStatus } }),
    updateProduct: (id, payload) => request(`/api/products/${id}`, { method: 'PUT', body: payload }),
    inventory: (productId) => request(`/api/inventory/product/${productId}`),
    updateInventory: (productId, stockQuantity, reason) => request(`/api/inventory/product/${productId}`, {
      method: 'PUT', body: { stockQuantity, reason },
    }),
    createProduct: (payload) => request('/api/products', { method: 'POST', body: payload }),
  },
}

export { API_URL }
