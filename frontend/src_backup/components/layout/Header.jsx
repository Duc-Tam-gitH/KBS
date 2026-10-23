import './Header.css'

export default function Header({ route, go, count, user, isAdmin, logout }) {
  return (
    <header className="glass-nav">
      <button className="brand" onClick={() => go('home')}>
        <span>K</span>KBS
      </button>
      <nav>
        <button className={route === 'home' ? 'active' : ''} onClick={() => go('home')}>
          Khám phá
        </button>
        {user && (
          <button className={route === 'orders' ? 'active' : ''} onClick={() => go('orders')}>
            Đơn hàng
          </button>
        )}
        {isAdmin && (
          <button className={route === 'admin' ? 'active' : ''} onClick={() => go('admin')}>
            Quản trị
          </button>
        )}
      </nav>
      <div className="nav-actions">
        <button
          className={`soft-button ${route === 'auth' || route === 'register' ? 'active' : ''}`}
          onClick={() => go(user ? 'orders' : 'auth')}
        >
          {user ? user.username : 'Đăng nhập'}
        </button>
        {user && (
          <button className="soft-button icon" onClick={logout} title="Đăng xuất">
            ↪
          </button>
        )}
        <button className="cart-button" onClick={() => go('cart')}>
          Giỏ <b>{count}</b>
        </button>
      </div>
    </header>
  )
}
