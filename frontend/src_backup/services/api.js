const API_URL = (import.meta.env.VITE_API_URL || 'https://localhost:7013').replace(/\/$/, '')

// Access token chỉ nằm trong bộ nhớ của tab. Không dùng localStorage để tránh
// token tồn tại lâu và bị đọc bởi script nếu trang gặp XSS.
let accessToken = null
let currentUser = null

export const authStore = {
  get token() { return accessToken },
  get user() { return currentUser },
  setSession(session) { accessToken = session.accessToken; currentUser = session.user },
  clear() { accessToken = null; currentUser = null },
  hasRole(role) { return currentUser?.roleName === role },
}

export class ApiError extends Error {
  constructor(message, status) { super(message); this.status = status }
}

async function request(path, options = {}) {
  const headers = new Headers(options.headers)
  headers.set('Accept', 'application/json')
  if (options.body !== undefined) headers.set('Content-Type', 'application/json')
  if (accessToken) headers.set('Authorization', `Bearer ${accessToken}`)
  const response = await fetch(`${API_URL}${path}`, { ...options, headers, body: options.body === undefined ? undefined : JSON.stringify(options.body) })
  if (response.status === 401) authStore.clear()
  if (!response.ok) {
    const payload = await response.json().catch(() => null)
    throw new ApiError(payload?.message || 'Không thể kết nối đến máy chủ.', response.status)
  }
  return response.status === 204 ? null : response.json()
}

export const api = {
  login: async (credentials) => { const session = await request('/api/auth/login', { method: 'POST', body: credentials }); authStore.setSession(session); return session },
  register: (payload) => request('/api/auth/register', { method: 'POST', body: payload }),
  products: (params = {}) => request(`/api/products?${new URLSearchParams(params)}`),
  product: (id) => request(`/api/products/${id}`),
  brands: () => request('/api/brands'),
  categories: () => request('/api/categories'),
  myOrders: () => request('/api/orders/mine'),
  createOrder: (payload) => request('/api/orders', { method: 'POST', body: payload }),
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
  admin: {
    dashboard: () => request('/api/admin/dashboard'),
    orders: () => request('/api/orders'),
    updateOrderStatus: (id, orderStatus) => request(`/api/orders/${id}/status`, { method: 'PUT', body: { orderStatus } }),
    updateProduct: (id, payload) => request(`/api/products/${id}`, { method: 'PUT', body: payload }),
    inventory: (productId) => request(`/api/inventory/product/${productId}`),
    updateInventory: (productId, stockQuantity) => request(`/api/inventory/product/${productId}`, { method: 'PUT', body: { stockQuantity } }),
    createProduct: (payload) => request('/api/products', { method: 'POST', body: payload }),
  },
}

export { API_URL }
