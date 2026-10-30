import { useState } from 'react'
import { useHomeContent } from './useHomeContent'
import { ShoppingBag, Heart, Menu, X, UserRound, LogOut } from 'lucide-react'

type Props = {
  route: string
  onNavigate: (route: string) => void
  cartCount: number
  user: { username: string } | null
  role: string
  onLogout: () => void
}

export default function Navbar({ route, onNavigate, cartCount, user, role, onLogout }: Props) {
  const get = useHomeContent()
  const [menuOpen, setMenuOpen] = useState(false)
  const links = [
    { route: 'home', label: 'Trang chủ' },
    { route: 'shop', label: 'Khám phá' },
    ...(user && role !== 'staff' ? [{ route: 'orders', label: 'Đơn hàng' }] : []),
    ...(role === 'admin' ? [{ route: 'admin', label: 'Quản trị' }] : []),
    ...(['staff', 'admin'].includes(role) ? [{ route: 'staff', label: 'Nhân viên' }]
      : [{ route: 'support', label: 'Hỗ trợ' }]),
  ]
  const navigate = (next: string) => { setMenuOpen(false); onNavigate(next) }
  const navButton = 'px-3 py-2 rounded-full text-sm transition-colors hover:bg-border hover:text-accent'
  const iconButton = 'flex h-10 w-10 shrink-0 items-center justify-center rounded-full transition-colors hover:bg-border hover:text-accent'
  return (
    <div className="fixed bottom-4 md:bottom-6 left-1/2 -translate-x-1/2 z-50 w-[calc(100%-1.5rem)] max-w-3xl">
      {menuOpen && <div className="md:hidden glass-dark rounded-3xl p-3 mb-2 grid grid-cols-2 gap-2 border border-border">
        {links.map(link => <button key={link.route} className={navButton + (route === link.route ? ' bg-primary text-deep font-semibold hover:text-deep' : '')}
          aria-current={route === link.route ? 'page' : undefined} onClick={() => navigate(link.route)}>
          {link.label}
        </button>)}
      </div>}
      <nav aria-label="Điều hướng chính" className="glass-dark flex items-center gap-1 md:gap-2 rounded-full px-3 py-3 border border-border shadow-xl shadow-deep">
        <button onClick={() => navigate('home')} aria-label="Trang chủ"
          className="px-2 md:px-4 font-display font-bold text-xl tracking-tight text-light hover:text-accent transition-colors">{get('hero.text2')}</button>
        <div className="hidden md:flex items-center gap-1">
          {links.map(link => <button key={link.route}
            className={navButton + (route === link.route ? ' bg-primary text-deep font-semibold hover:text-deep' : '')}
            aria-current={route === link.route ? 'page' : undefined} onClick={() => navigate(link.route)}>
            {link.label}
          </button>)}
        </div>
        <div className="flex-1" />
        {role !== 'staff' && <button className={iconButton + (route === 'likes' ? ' bg-primary text-deep hover:text-deep' : '')}
          aria-label="Sản phẩm yêu thích"
          title="Sản phẩm yêu thích"
          onClick={() => navigate('likes')}>
          <Heart size={18} className={route === 'likes' ? 'fill-deep stroke-deep' : ''} />
        </button>}
        {role !== 'staff' && <button className={iconButton + ' relative' + (route === 'cart' ? ' bg-primary text-deep hover:text-deep' : '')}
          aria-label={'Giỏ hàng: ' + cartCount + ' sản phẩm'}
          onClick={() => navigate('cart')}>
          <ShoppingBag size={18} />
          {cartCount > 0 && <span className="absolute -top-1 -right-1 rounded-full bg-primary text-deep font-bold text-xs px-1.5 shadow">
            {cartCount}
          </span>}
        </button>}
        <button className={iconButton} title={user?.username || 'Đăng nhập'}
          aria-label={user ? 'Tài khoản' : 'Đăng nhập'} onClick={() => navigate(user ? 'account' : 'auth')}>
          <UserRound size={18} />
        </button>
        {user && <button className={iconButton + ' hidden md:flex'} onClick={onLogout} aria-label="Đăng xuất">
          <LogOut size={18} />
        </button>}
        <button className={iconButton + ' md:hidden'} aria-label="Menu" aria-expanded={menuOpen}
          onClick={() => { setMenuOpen(!menuOpen) }}>
          {menuOpen ? <X size={18} /> : <Menu size={18} />}
        </button>
      </nav>
    </div>
  )
}
