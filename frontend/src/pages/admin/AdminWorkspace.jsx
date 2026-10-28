import { useState } from 'react'
import Dashboard from './Dashboard'
import AccountManagement from './AccountManagement'
import ProductManagement from './ProductManagement'
import CategoryManagement from './CategoryManagement'
import InventoryManagement from './InventoryManagement'
import ShopManagement from './ShopManagement'
import AIManagement from './AIManagement'
import './Dashboard.css'

const adminPages = [
  { id: 'dashboard', label: 'Dashboard', Component: Dashboard },
  { id: 'accounts', label: 'Quản lý tài khoản', Component: AccountManagement },
  { id: 'products', label: 'Quản lý sản phẩm', Component: ProductManagement },
  { id: 'categories', label: 'Quản lý danh mục', Component: CategoryManagement },
  { id: 'inventory', label: 'Quản lý kho', Component: InventoryManagement },
  { id: 'shop', label: 'Quản lý cửa hàng', Component: ShopManagement },
  { id: 'ai', label: 'Quản lý AI', Component: AIManagement },
]

export default function AdminWorkspace({ user, onLogout }) {
  const [pageId, setPageId] = useState('dashboard')
  if (user?.roleName !== 'Admin') return <p role="alert">Không có quyền truy cập.</p>
  const page = adminPages.find(item => item.id === pageId)
  const Page = page.Component

  return <section className="admin-dashboard" aria-label="Khu vực quản trị">
    <aside className="ad-sidebar ad-panel">
      <p className="ad-eyebrow">KBS / QUẢN TRỊ</p>
      <h2>Xin chào{user.fullName || user.username ? ', ' + (user.fullName || user.username) : ''}!</h2>
      <p className="ad-muted">Quản lý hoạt động cửa hàng.</p>
      <nav aria-label="Các trang quản trị">
        {adminPages.map(item => <button type="button" key={item.id}
          aria-current={pageId === item.id ? 'page' : undefined}
          onClick={() => setPageId(item.id)}>{item.label}</button>)}
      </nav>
      <div className="ad-session">
        {onLogout && <button type="button" onClick={onLogout}>Đăng xuất</button>}
      </div>
    </aside>
    {['dashboard', 'categories', 'products'].includes(pageId) ? <Page key={pageId} user={user} /> : <>
      <div className="ad-main">
        <header className="ad-heading"><h1>{page.label}</h1></header>
        <Page key={pageId} />
      </div>
      <aside className="ad-details ad-panel" aria-label="Chi tiết dữ liệu">
        <p className="ad-eyebrow">CHI TIẾT DỮ LIỆU</p>
        <p className="ad-empty">Chưa có dữ liệu chi tiết.</p>
      </aside>
    </>}
  </section>
}
