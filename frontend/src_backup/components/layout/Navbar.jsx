import { useState } from 'react'
import './Navbar.css'

/* ===== Cấu hình menu theo role ===== */
const MENU_ITEMS = [
  { key: 'home',       label: 'Trang chủ',          roles: ['customer', 'staff'] },
  { key: 'shop',       label: 'Khám phá',           roles: ['customer', 'staff'] },
  { key: 'orders',     label: 'Lịch sử mua',        roles: ['customer', 'staff'] },
  { key: 'account',    label: 'Quản lý tài khoản',  roles: ['customer', 'staff'] },
  { key: 'admin',      label: 'Quản trị',          roles: ['admin'] },
]

/* Map route con -> route menu cha để highlight đúng */
const ACTIVE_MAP = {
  'order-detail': 'orders',
  'checkout': 'cart',
  'pos-checkout': 'pos',
}

function isMenuActive(route, key) {
  if (route === key) return true
  const parent = ACTIVE_MAP[route]
  return parent === key
}

export default function Navbar({ route, go, cartCount = 0, user, role = 'customer', logout }) {
  const [menuOpen, setMenuOpen] = useState(false)

  const items = MENU_ITEMS.filter((item) => item.roles.includes(role))
  const isCartActive = route === 'cart' || route === 'checkout'

  const handleGo = (key) => {
    setMenuOpen(false)
    go(key)
  }

  return (
    <header className="liquid-navbar">
      {/* Brand */}
      <button className="liquid-brand" onClick={() => handleGo('home')} aria-label="Về trang chủ KBS">
        <span>K</span>KBS
      </button>

      {/* Nút hamburger (chỉ hiện mobile) */}
      <button
        className="liquid-hamburger"
        onClick={() => setMenuOpen((v) => !v)}
        aria-label="Mở menu"
        aria-expanded={menuOpen}
        aria-controls="liquid-main-nav"
      >
        <span className={menuOpen ? 'open' : ''}>☰</span>
      </button>

      {/* Nav chính */}
      <nav
        id="liquid-main-nav"
        className={`liquid-nav ${menuOpen ? 'is-open' : ''}`}
        aria-label="Điều hướng chính"
      >
        {items.map((item) => (
          <button
            key={item.key}
            className={isMenuActive(route, item.key) ? 'active' : ''}
            aria-current={isMenuActive(route, item.key) ? 'page' : undefined}
            onClick={() => handleGo(item.key)}
          >
            {item.label}
          </button>
        ))}

        {/* Trên mobile: gộp user/logout vào drawer */}
        <div className="liquid-nav-mobile-actions">
          <button onClick={() => handleGo(user ? 'account' : 'auth')}>
            {user ? user.username : 'Đăng nhập'}
          </button>
          {user && (
            <button onClick={() => { setMenuOpen(false); logout() }} aria-label="Đăng xuất">
              ↪ Đăng xuất
            </button>
          )}
        </div>
      </nav>

      {/* Actions (desktop) */}
      <div className="liquid-actions">
        <button onClick={() => go(user ? 'account' : 'auth')}>
          {user ? user.username : 'Đăng nhập'}
        </button>
        {user && (
          <button onClick={logout} aria-label="Đăng xuất">↪</button>
        )}
        <button
          className={`liquid-cart ${isCartActive ? 'active' : ''}`}
          onClick={() => go('cart')}
          aria-current={isCartActive ? 'page' : undefined}
        >
          Giỏ <b>{cartCount}</b>
        </button>
      </div>
    </header>
  )
}