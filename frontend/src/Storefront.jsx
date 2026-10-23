import { useEffect, useMemo, useRef, useState, useSyncExternalStore } from 'react'
import { api, ApiError, authStore } from './services/api'
import Login from './pages/auth/Login'
import Register from './pages/auth/Register'
import Navbar from './components/Navbar'
import Footer from './components/Footer'
import Home from './pages/Home'
import Cart from './pages/customer/Cart'
import Likelist from './pages/customer/Likelist'
import ManaAccount from './pages/customer/Mana_account'
import SearchPage from './pages/customer/Search'


const money = (n) => new Intl.NumberFormat('vi-VN').format(n || 0) + '₫'

const Keyboard = ({ tone = 'charcoal' }) => (
  <div className={`keyboard keyboard--${tone}`}>
    <div className="keyboard__keys">
      {Array.from({ length: 25 }, (_, i) => (
        <i key={i} />
      ))}
    </div>
  </div>
)

function App() {
  const user = useSyncExternalStore(authStore.subscribe, () => authStore.user, () => authStore.user)
  const [search, setSearch] = useState('')
  const [retry, setRetry] = useState(0)
  const [notice, setNotice] = useState('')
  const pendingRoute = useRef('home')
  const [route, setRoute] = useState('home')
  const [products, setProducts] = useState([])
  const [selected, setSelected] = useState(null)
  const [cart, setCart] = useState([])
  const [cartError, setCartError] = useState('')
  const [cartBusy, setCartBusy] = useState(true)
  const cartLock = useRef(false)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const go = (next, item = null) => {
    if (!authStore.user && ['cart', 'checkout', 'orders', 'account', 'admin', 'likes'].includes(next)) {
      pendingRoute.current = next
      next = 'auth'
    }
    setNotice('')
    setSelected(item)
    setRoute(next)
    window.scrollTo(0, 0)
  }

  const login = async (credentials) => {
    await api.login(credentials)
    setCartBusy(true)
    setCartError('')
    go(pendingRoute.current)
    pendingRoute.current = 'home'
  }

  const logout = () => {
    authStore.clear()
    pendingRoute.current = 'home'
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
    if (!user) return
    let active = true
    api.cart()
      .then((response) => { if (active) receiveCart(response) })
      .catch((err) => { if (active) setCartError(err.message) })
      .finally(() => { if (active) setCartBusy(false) })
    return () => { active = false }
  }, [user, retry])

  useEffect(() => {
    let active = true
    api.products({ page: 1, pageSize: 100 })
      .then((r) => {
        if (active) {
          setProducts(
            (r.items || []).map((p) => ({
              ...p,
              brand: p.brandName || 'KBS',
              category: p.categoryName || 'Sản phẩm',
              layout: p.layout || 'Tiêu chuẩn',
              connection: p.connectionType || 'Có dây',
              color: 'charcoal',
            }))
          )
        }
      })
      .catch(() => { if (active) setError('Không thể tải sản phẩm. Kiểm tra kết nối rồi thử lại.') })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [retry])

  const privateRoute = ['cart', 'checkout', 'orders', 'account', 'admin', 'likes'].includes(route)
  const visibleRoute = !user && privateRoute ? 'auth' : route
  const page =
    visibleRoute === 'auth' ? (
      <Login onLogin={login} onNavigate={go} />
    ) : visibleRoute === 'register' ? (
      <Register onRegister={api.register} onNavigate={go} />
    ) : visibleRoute === 'home' ? (
      <Home onNavigate={go} />
    ) : route === 'shop' ? (
      <Catalog products={products} loading={loading} error={error} onSelect={(p) => go('detail', p)} onAdd={addCart} busy={!!user && cartBusy} query={search} onQuery={setSearch}
        onRetry={() => { setLoading(true); setError(''); setRetry((n) => n + 1) }} />
    ) : route === 'search' ? (
      <SearchPage onSelect={(p) => go('detail', p)} onAdd={addCart} busy={!!user && cartBusy} />
    ) : route === 'detail' ? (
      <Detail key={selected?.id} busy={!!user && cartBusy} product={selected || products[0]} onBack={() => go('shop')} onAdd={addCart} />
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
      <Orders />
    ) : route === 'admin' ? (
      user.roleName?.toLowerCase() === 'admin' ? <Admin /> : <Empty title="Không có quyền truy cập" />
    ) : route === 'account' ? (
      <ManaAccount key={user.id} user={user} onNavigate={go} onLogout={logout} />
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

export function Catalog({ products, loading, error, onSelect, onAdd, busy, query: q, onQuery, onRetry }) {
  const [category, setCategory] = useState('Tất cả')

  const list = useMemo(
    () =>
      products.filter(
        (p) =>
          (category === 'Tất cả' || p.category === category) &&
          `${p.name} ${p.brand}`.toLowerCase().includes(q.toLowerCase())
      ),
    [products, category, q]
  )

  const cats = ['Tất cả', ...new Set(products.map((p) => p.category))]

  return (
    <>
      <section className="hero glass-hero">
        <div>
          <p className="eyebrow">KBS / 2026 COLLECTION</p>
          <h1>
            Cảm giác gõ,<br />
            <em>trong veo.</em>
          </h1>
          <p>Những bàn phím được chọn lọc cho nhịp làm việc và khoảnh khắc sáng tạo của bạn.</p>
          <button className="ink-button" onClick={() => document.querySelector('#catalog')?.scrollIntoView()}>
            Khám phá bộ sưu tập →
          </button>
        </div>
        <div className="hero-key">
          <Keyboard tone="hero" />
        </div>
      </section>

      <section className="catalog-page" id="catalog">
        <div className="section-heading">
          <div>
            <p className="eyebrow">SẢN PHẨM</p>
            <h2>Tìm đúng chất của bạn</h2>
          </div>
          <span>{list.length} lựa chọn</span>
        </div>

        <div className="glass-toolbar">
          <input
            value={q}
            onChange={(e) => onQuery(e.target.value)}
            aria-label="Tìm sản phẩm"
            placeholder="Tìm bàn phím hoặc thương hiệu"
          />
          <div className="chips">
            {cats.map((c) => (
              <button
                key={c}
                className={category === c ? 'selected' : ''}
                onClick={() => setCategory(c)}
              >
                {c}
              </button>
            ))}
          </div>
        </div>

        {error && <p className="notice" role="alert">{error} <button onClick={onRetry}>Thử lại</button></p>}

        <div className="product-grid">
          {loading
            ? Array.from({ length: 6 }, (_, i) => <div className="product-skeleton" key={i} />)
            : list.map((p) => (
                <ProductCard key={p.id} product={p} onSelect={onSelect} onAdd={onAdd} busy={busy} />
              ))}
        </div>

        {!loading && !error && !list.length && (
          <Empty title="Không tìm thấy sản phẩm" text="Thử đổi từ khóa hoặc danh mục bạn đang chọn." />
        )}
      </section>
    </>
  )
}

function ProductCard({ product, onSelect, onAdd, busy }) {
  const image = product.images?.[0]
  return (
    <article className="glass-card product-card">
      <button className="product-image" aria-label={'Xem ' + product.name} onClick={() => onSelect(product)}>
        {image?.imageUrl ? (
          <img src={image.imageUrl} alt={image.altText || product.name} />
        ) : (
          <Keyboard tone={product.color} />
        )}
      </button>
      <div>
        <p className="eyebrow">{product.brand}</p>
        <h3>{product.name}</h3>
        <p className="subtle">
          {product.layout} · {product.connection}
        </p>
        <div className="card-foot">
          <strong>{money(product.price)}</strong>
          <button className="plus" disabled={busy} onClick={() => onAdd(product)} title="Thêm vào giỏ">
            +
          </button>
        </div>
      </div>
    </article>
  )
}

export function Detail({ product, onBack, onAdd, busy }) {
  const [qty, setQty] = useState(1)
  if (!product) return <Empty title="Không tìm thấy sản phẩm" action="Về cửa hàng" onAction={onBack} />
  return (
    <section className="page-wrap detail-page">
      <button className="back" onClick={onBack}>
        ← Trở lại cửa hàng
      </button>
      <div className="detail-grid">
        <div className="glass-card detail-visual">
          {product.images?.[0]?.imageUrl ? (
            <img src={product.images[0].imageUrl} alt={product.name} />
          ) : (
            <Keyboard tone={product.color} />
          )}
        </div>
        <div className="glass-card detail-info">
          <p className="eyebrow">{product.brand}</p>
          <h1>{product.name}</h1>
          <strong className="big-price">{money(product.price)}</strong>
          <p>{product.description || 'Thiết kế cân bằng giữa cảm giác gõ, hiệu năng và vẻ đẹp tối giản.'}</p>
          <dl>
            <div>
              <dt>Layout</dt>
              <dd>{product.layout}</dd>
            </div>
            <div>
              <dt>Kết nối</dt>
              <dd>{product.connection}</dd>
            </div>
            <div>
              <dt>Switch</dt>
              <dd>{product.switchType || 'Tuỳ chọn'}</dd>
            </div>
          </dl>
          <div className="buy-row">
            <div className="stepper">
              <button aria-label="Giảm số lượng" disabled={qty <= 1} onClick={() => setQty(Math.max(1, qty - 1))}>−</button>
              <span>{qty}</span>
              <button aria-label="Tăng số lượng" disabled={qty >= 100} onClick={() => setQty(Math.min(100, qty + 1))}>+</button>
            </div>
            <button
              className="ink-button"
              disabled={busy}
              onClick={async () => {
                if (await onAdd(product, qty)) onBack()
              }}
            >
              Thêm vào giỏ
            </button>
          </div>
        </div>
      </div>
    </section>
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

export function Orders() {
  const [orders, setOrders] = useState(null)
  const [error, setError] = useState('')

  useEffect(() => {
    api.myOrders()
      .then(setOrders)
      .catch((e) => setError(e.message))
  }, [])

  return (
    <section className="page-wrap">
      <PageTitle eyebrow="TÀI KHOẢN" title="Lịch sử đơn hàng" />
      {error ? (
        <Empty title="Chưa thể tải đơn hàng" text={error} />
      ) : orders === null ? (
        <div className="glass-card loading-panel">Đang tải đơn hàng…</div>
      ) : (
        <div className="order-list">
          {orders.length ? (
            orders.map((o) => (
              <article className="glass-card order-card" key={o.id}>
                <div>
                  <p className="eyebrow">ĐƠN #{o.id}</p>
                  <h3>{new Date(o.createdAt).toLocaleDateString('vi-VN')}</h3>
                </div>
                <span className={`status ${o.orderStatus?.toLowerCase() || ''}`}>{o.orderStatus}</span>
                <strong>{money(o.totalPrice)}</strong>
                <p>{o.orderDetails?.map((x) => `${x.productName} × ${x.quantity}`).join(', ') || ''}</p>
              </article>
            ))
          ) : (
            <Empty title="Bạn chưa có đơn hàng" text="Khi đặt hàng, đơn của bạn sẽ xuất hiện tại đây." />
          )}
        </div>
      )}
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
