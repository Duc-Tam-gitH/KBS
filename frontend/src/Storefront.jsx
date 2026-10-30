import { useEffect, useRef, useState, useSyncExternalStore } from 'react'
import { api, authStore } from './services/api'
import Login from './pages/auth/Login'
import Register from './pages/auth/Register'
import Navbar from './components/Navbar'
import Footer from './components/Footer'
import Home from './pages/Home'
import { useShop } from './components/ShopProvider'
import Cart from './pages/customer/Cart'
import Checkout from './pages/customer/Checkout'
import Likelist from './pages/customer/Likelist'
import Account from './pages/customer/Account'
import SearchPage from './pages/customer/Search'
import Detail from './pages/customer/productdetail'
import Orders from './pages/customer/orderProduct'
import StaffWorkspace from './pages/Staff/StaffWorkspace'
import SupportChat from './pages/Staff/SupportChat'
import Admin from './pages/admin/AdminWorkspace'

export { default as Admin } from './pages/admin/AdminWorkspace'

export { default as Detail } from './pages/customer/productdetail'
export { default as Orders } from './pages/customer/orderProduct'




const linkedProduct = () => {
  if (typeof window === 'undefined') return null
  const id = new URLSearchParams(window.location.hash.slice(1)).get('product')
  return id && /^[1-9]\d*$/.test(id) && Number.isSafeInteger(Number(id)) ? { id: Number(id) } : null
}

function App() {
  const shopState = useShop()
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
    if (authStore.hasRole('Admin')) { next = 'admin'; item = null }
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
    imageUrl: item.imageUrl, color: item.color, layout: item.layout,
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
    if (!user || user.roleName === 'Admin') return
    let active = true
    api.cart()
      .then((response) => { if (active) receiveCart(response) })
      .catch((err) => { if (active) setCartError(err.message) })
      .finally(() => { if (active) setCartBusy(false) })
    return () => { active = false }
  }, [user, retry])


  // Admin luôn dùng workspace riêng, kể cả khi URL đang trỏ tới sản phẩm hay trang nhân viên.
  if (user?.roleName === 'Admin') return <main className="min-h-screen bg-deep text-light font-body">
    <Admin key={user.id} user={user} onLogout={logout} />
  </main>

  if (shopState.shop?.isOpen === false) return <main className="store-pages min-h-screen bg-deep text-light">
    <section className="ad-panel mx-auto max-w-xl p-8">
      <h1 className="text-3xl font-bold">Website đang tạm đóng</h1>
      <p className="my-4">Cửa hàng đang bảo trì. Vui lòng quay lại sau.</p>
      {route === 'auth' ? <Login onLogin={login} onNavigate={() => {}} />
        : <button type="button" className="back" onClick={() => setRoute('auth')}>Đăng nhập quản trị</button>}
      {user && <button type="button" className="back" onClick={logout}>Đăng xuất</button>}
    </section>
  </main>

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
        key={user.id}
        cart={cart}
        user={user}
        busy={cartBusy}
        cartError={cartError}
        onBack={() => go('cart')}
        onShop={() => go('shop')}
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
      <Empty title="Không có quyền truy cập" action="Về trang chủ" onAction={() => go('home')} />
    ) : route === 'account' ? (
      <Account key={user.id} user={user} onNavigate={go} onLogout={logout} />
    ) : (
      <Empty title="Trang không tồn tại" action="Về trang chủ" onAction={() => go('home')} />
    )

  return (
    <div className="min-h-screen bg-deep text-light font-body overflow-x-hidden">
      {visibleRoute !== 'admin' && <Navbar
        route={visibleRoute}
        onNavigate={go}
        cartCount={user ? cart.reduce((n, x) => n + x.quantity, 0) : 0}
        user={user}
        role={user?.roleName?.toLowerCase() || 'customer'}
        onLogout={logout}
      />}
      <main className={visibleRoute === 'home' ? '' : 'store-pages'}>
        {notice && <p className="notice" role="status">{notice}</p>}
        {user && cartError && <div className="notice" role="alert">{cartError}
          <button className="back" onClick={() => { setCartBusy(true); setCartError(''); setRetry(n => n + 1) }}>
            Thử lại
          </button>
        </div>}
        {page}
      </main>
      {visibleRoute !== 'admin' && <Footer onNavigate={go} />}
    </div>
  )
}

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
