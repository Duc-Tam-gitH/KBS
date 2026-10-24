import { useEffect, useRef, useState, useSyncExternalStore } from 'react'
import { api, ApiError, authStore } from './services/api'
import Login from './pages/auth/Login'
import Register from './pages/auth/Register'
import Navbar from './components/Navbar'
import Footer from './components/Footer'
import Home from './pages/Home'
import Cart from './pages/customer/Cart'
import Likelist from './pages/customer/Likelist'
import Account from './pages/customer/Account'
import SearchPage from './pages/customer/Search'
import Detail from './pages/customer/productdetail'
import Orders from './pages/customer/orderProduct'
import StaffWorkspace from './pages/Staff/StaffWorkspace'
import SupportChat from './pages/Staff/SupportChat'

export { default as Detail } from './pages/customer/productdetail'
export { default as Orders } from './pages/customer/orderProduct'


const money = (n) => new Intl.NumberFormat('vi-VN').format(n || 0) + '₫'

const linkedProduct = () => {
  if (typeof window === 'undefined') return null
  const id = new URLSearchParams(window.location.hash.slice(1)).get('product')
  return id && /^[1-9]\d*$/.test(id) && Number.isSafeInteger(Number(id)) ? { id: Number(id) } : null
}

function App() {
  const user = useSyncExternalStore(authStore.subscribe, () => authStore.user, () => authStore.user)
  const [retry, setRetry] = useState(0)
  const [notice, setNotice] = useState('')
  const pendingRoute = useRef('home')
  const pendingProduct = useRef(null)
  const [route, setRoute] = useState(() => linkedProduct() ? 'detail' : 'home')
  const [selected, setSelected] = useState(linkedProduct)
  const [cart, setCart] = useState([])
  const [cartError, setCartError] = useState('')
  const [cartBusy, setCartBusy] = useState(true)
  const cartLock = useRef(false)

  const go = (next, item = null) => {
    if (next === 'auth' && route === 'detail' && selected) {
      pendingRoute.current = 'detail'
      pendingProduct.current = selected
    }
    if (!authStore.user && ['cart', 'checkout', 'orders', 'account', 'admin', 'likes', 'staff', 'support'].includes(next)) {
      pendingRoute.current = next
      next = 'auth'
    }
    setNotice('')
    setSelected(item)
    setRoute(next)
    const url = new URL(window.location.href)
    url.hash = next === 'detail' && item?.id ? `product=${item.id}` : ''
    window.history.replaceState(null, '', url)
    window.scrollTo(0, 0)
  }

  const login = async (credentials) => {
    await api.login(credentials)
    setCartBusy(true)
    setCartError('')
    go(pendingRoute.current, pendingProduct.current)
    pendingRoute.current = 'home'
    pendingProduct.current = null
  }

  const logout = () => {
    authStore.clear()
    pendingRoute.current = 'home'
    pendingProduct.current = null
    setCartError('')
    setCart([])
    go('auth')
  }


  const receiveCart = (response) => setCart((response.items || []).map((item) => ({
    id: item.productId, name: item.productName, price: item.unitPrice, quantity: item.quantity,
  })))

  const mutateCart = async (operation) => {
    if (cartLock.current || cartBusy) return false
    cartLock.current = true
    setCartBusy(true)
    setCartError('')
    const sessionUser = authStore.user
    try {
      const response = await operation()
      if (authStore.user !== sessionUser) return false
      receiveCart(response)
      return true
    } catch (err) {
      if (authStore.user === sessionUser) setCartError(err.message || 'Không thể cập nhật giỏ hàng.')
      return false
    } finally {
      cartLock.current = false
      if (authStore.user === sessionUser) setCartBusy(false)
    }
  }

  const addCart = async (product, quantity = 1) => {
    if (!user) { pendingRoute.current = 'shop'; go('auth'); return false }
    const added = await mutateCart(() => api.addCartItem(product.id, quantity))
    if (added) setNotice('Đã thêm sản phẩm vào giỏ hàng.')
    return added
  }
  const updateCart = (id, quantity) => mutateCart(() => api.updateCartItem(id, quantity))
  const removeCart = (id) => mutateCart(async () => {
    await api.removeCartItem(id)
    return api.cart()
  })

  useEffect(() => {
    const openLinkedProduct = () => {
      const product = linkedProduct()
      if (product) { setSelected(product); setRoute('detail') }
    }
    window.addEventListener('hashchange', openLinkedProduct)
    return () => window.removeEventListener('hashchange', openLinkedProduct)
  }, [])

  useEffect(() => {
    if (!user) return
    let active = true
    api.cart()
      .then((response) => { if (active) receiveCart(response) })
      .catch((err) => { if (active) setCartError(err.message) })
      .finally(() => { if (active) setCartBusy(false) })
    return () => { active = false }
  }, [user, retry])


  const privateRoute = ['cart', 'checkout', 'orders', 'account', 'admin', 'likes', 'staff', 'support'].includes(route)
  const visibleRoute = !user && privateRoute ? 'auth' : route
  const page =
    visibleRoute === 'auth' ? (
      <Login onLogin={login} onNavigate={go} />
    ) : visibleRoute === 'register' ? (
      <Register onRegister={api.register} onNavigate={go} />
    ) : visibleRoute === 'home' ? (
      <Home onNavigate={go} />
    ) : route === 'shop' || route === 'search' ? (
      <SearchPage onSelect={(p) => go('detail', p)} onAdd={addCart} busy={!!user && cartBusy} />
    ) : route === 'detail' ? (
      <Detail key={`${selected?.id}-${user?.id || 'guest'}`} busy={!!user && cartBusy}
        product={selected} onBack={() => go('shop')} onAdd={addCart} user={user}
        onNavigate={go} onSelect={(product) => go('detail', product)} />
    ) : route === 'cart' ? (
      <Cart cart={cart} busy={cartBusy || !!cartError} onUpdate={updateCart} onRemove={removeCart}
        onCheckout={() => go('checkout')} onNavigate={go} />
    ) : route === 'likes' ? (
      <Likelist
        key={user?.id}
        onNavigate={go}
        onAddToCart={(p) => addCart(p, 1)}
        onSelectProduct={(p) => go('detail', p)}
        busy={!!user && cartBusy}
        user={user}
      />
    ) : route === 'checkout' ? (
      <Checkout
        cart={cart}
        onPlaced={() => setCart([])}
        onDone={() => go('orders')}
      />
    ) : route === 'orders' ? (
      <Orders key={user.id} user={user} onNavigate={go} onAdd={addCart} busy={cartBusy} />
    ) : route === 'staff' ? (
      <StaffWorkspace key={user.id} user={user} onNavigate={go} onLogout={logout} />
    ) : route === 'support' ? (
      <SupportChat key={user.id} user={user} />
    ) : route === 'admin' ? (
      user.roleName?.toLowerCase() === 'admin' ? <Admin /> : <Empty title="Không có quyền truy cập" />
    ) : route === 'account' ? (
      <Account key={user.id} user={user} onNavigate={go} onLogout={logout} />
    ) : (
      <Empty title="Trang không tồn tại" action="Về trang chủ" onAction={() => go('home')} />
    )

  return (
    <div className="min-h-screen bg-deep text-light font-body overflow-x-hidden">
      <Navbar
        route={visibleRoute}
        onNavigate={go}
        cartCount={user ? cart.reduce((n, x) => n + x.quantity, 0) : 0}
        user={user}
        role={user?.roleName?.toLowerCase() || 'customer'}
        onLogout={logout}
      />
      <main className={visibleRoute === 'home' ? '' : 'store-pages'}>
        {notice && <p className="notice" role="status">{notice}</p>}
        {user && cartError && <div className="notice" role="alert">{cartError}
          <button className="back" onClick={() => { setCartBusy(true); setCartError(''); setRetry(n => n + 1) }}>
            Thử lại
          </button>
        </div>}
        {page}
      </main>
      <Footer onNavigate={go} />
    </div>
  )
}

export function Checkout({ cart, onPlaced, onDone }) {
  const [status, setStatus] = useState('')
  const submitting = useRef(false)
  const total = cart.reduce((n, x) => n + x.price * x.quantity, 0)

  const submit = async (e) => {
    e.preventDefault()
    if (submitting.current) return
    submitting.current = true
    setStatus('loading')
    const f = new FormData(e.currentTarget)
    try {
      await api.createOrder({
        customerName: f.get('name'),
        phoneNumber: f.get('phone'),
        shippingAddress: f.get('address'),
        paymentMethod: f.get('payment'),
        orderItems: cart.map((x) => ({ productId: x.id, quantity: x.quantity })),
      })
      setStatus('done')
      onPlaced()
    } catch (err) {
      submitting.current = false
      setStatus(err instanceof ApiError ? err.message : 'Không thể tạo đơn hàng.')
    }
  }

  if (!cart.length && status !== 'done') {
    return (
      <section className="page-wrap">
        <Empty title="Không có sản phẩm để thanh toán" text="Quay lại cửa hàng để thêm sản phẩm." />
      </section>
    )
  }

  if (status === 'done') {
    return (
      <section className="page-wrap">
        <Empty
          title="Đặt hàng thành công"
          text="Đơn hàng đã được ghi nhận. Bạn có thể theo dõi trong lịch sử đơn."
          action="Xem đơn hàng"
          onAction={onDone}
        />
      </section>
    )
  }

  return (
    <section className="page-wrap">
      <PageTitle eyebrow="THANH TOÁN" title="Một bước thật nhẹ nhàng" />
      <form className="glass-card checkout-form" onSubmit={submit}>
        <label>
          Họ và tên
          <input name="name" required />
        </label>
        <label>
          Số điện thoại
          <input name="phone" required pattern="[0-9+ ()-]{8,20}" />
        </label>
        <label>
          Địa chỉ giao hàng
          <textarea name="address" required />
        </label>
        <label>
          Thanh toán
          <select name="payment" defaultValue="COD">
            <option value="COD">Thanh toán khi nhận hàng</option>
            <option value="BANKING">Chuyển khoản</option>
          </select>
        </label>
        <div className="checkout-total">
          <span>Tổng cộng</span>
          <strong>{money(total)}</strong>
        </div>
        <button className="ink-button" disabled={status === 'loading'}>
          {status === 'loading' ? 'Đang xử lý…' : 'Đặt hàng'}
        </button>
        {status && status !== 'loading' && <p className="notice">{status}</p>}
      </form>
    </section>
  )
}

export function Admin() {
  const [dashboard, setDashboard] = useState(null)
  const [error, setError] = useState('')

  useEffect(() => {
    api.admin.dashboard()
      .then(setDashboard)
      .catch((e) => setError(e.message))
  }, [])

  return (
    <section className="page-wrap">
      <PageTitle eyebrow="ADMIN CONSOLE" title="Nhịp vận hành hôm nay" />
      {error ? (
        <Empty title="Không thể tải dashboard" text={error} />
      ) : !dashboard ? (
        <div className="glass-card loading-panel">Đang tổng hợp số liệu…</div>
      ) : (
        <>
          <div className="metric-grid">
            <Metric label="Doanh thu" value={money(dashboard.revenue)} />
            <Metric label="Tổng đơn" value={dashboard.totalOrders} />
            <Metric label="Đang chờ" value={dashboard.pendingOrders} />
            <Metric label="Sắp hết hàng" value={dashboard.lowStockProducts} />
          </div>
          <div className="glass-card chart">
            <p className="eyebrow">DOANH THU 7 NGÀY</p>
            {dashboard.revenueLast7Days?.map((x) => (
              <div className="bar" key={x.date}>
                <span>{x.date}</span>
                <i
                  style={{
                    width: `${Math.min(
                      100,
                      (Number(x.revenue) / Math.max(1, Number(dashboard.revenue))) * 100
                    )}%`,
                  }}
                />
                <b>{money(x.revenue)}</b>
              </div>
            ))}
          </div>
        </>
      )}
    </section>
  )
}

const PageTitle = ({ eyebrow, title }) => (
  <div className="page-title">
    <p className="eyebrow">{eyebrow}</p>
    <h1>{title}</h1>
  </div>
)

const Metric = ({ label, value }) => (
  <div className="glass-card metric">
    <span>{label}</span>
    <strong>{value}</strong>
  </div>
)

function Empty({ title, text, action, onAction }) {
  return (
    <div className="glass-card empty">
      <h3>{title}</h3>
      <p>{text}</p>
      {action && (
        <button className="ink-button" onClick={onAction}>
          {action}
        </button>
      )}
    </div>
  )
}

export default App
