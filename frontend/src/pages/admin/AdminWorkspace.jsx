import { useSessionView } from '../../services/useSessionView'
import Dashboard from './Dashboard'
import AccountManagement from './AccountManagement'
import ProductManagement from './ProductManagement'
import CategoryManagement from './CategoryManagement'
import InventoryManagement from './InventoryManagement'
import ShopManagement from './ShopManagement'
import PaymentManagement from './PaymentManagement'
import AIManagement from './AIManagement'
import ReviewModeration from './ReviewModeration'
import './Dashboard.css'

const adminPages = [
  { id: 'payments', label: 'Thanh toán', group: 'Cửa hàng', Component: PaymentManagement },
  { id: 'reviews', label: 'Duyệt đánh giá', group: 'Nội dung & AI', Component: ReviewModeration },
  { id: 'dashboard', label: 'Dashboard', group: 'Tổng quan', Component: Dashboard },
  { id: 'accounts', label: 'Quản lý tài khoản', group: 'Cửa hàng', Component: AccountManagement },
  { id: 'products', label: 'Quản lý sản phẩm', group: 'Sản phẩm & kho', Component: ProductManagement },
  { id: 'categories', label: 'Quản lý danh mục', group: 'Sản phẩm & kho', Component: CategoryManagement },
  { id: 'inventory', label: 'Quản lý kho', group: 'Sản phẩm & kho', Component: InventoryManagement },
  { id: 'shop', label: 'Quản lý cửa hàng', group: 'Cửa hàng', Component: ShopManagement },
  { id: 'ai', label: 'Quản lý AI', group: 'Nội dung & AI', Component: AIManagement },
]
const adminMenuGroups = ['Tổng quan', 'Sản phẩm & kho', 'Cửa hàng', 'Nội dung & AI']

export default function AdminWorkspace({ user, onLogout }) {
  const [pageId, setPageId] = useSessionView('admin.page', 'dashboard',
    value => adminPages.some(item => item.id === value))
  if (user?.roleName !== 'Admin') return <p role="alert">Không có quyền truy cập.</p>
  const page = adminPages.find(item => item.id === pageId)
  const Page = page.Component

  return <section className="admin-dashboard" aria-label="Khu vực quản trị">
    <aside className="ad-sidebar ad-panel">
      <p className="ad-eyebrow">KBS / QUẢN TRỊ</p>
      <h2>Xin chào{user.fullName || user.username ? ', ' + (user.fullName || user.username) : ''}!</h2>
      <p className="ad-muted">Quản lý hoạt động cửa hàng.</p>
      <nav aria-label="Các trang quản trị">
        {adminMenuGroups.map(group => <div className="ad-nav-group" key={group}>
          <h3 className="ad-nav-label">{group}</h3>
          {adminPages.filter(item => item.group === group).map(item => <button type="button" key={item.id}
            aria-current={pageId === item.id ? 'page' : undefined}
            onClick={() => setPageId(item.id)}>{item.label}</button>)}
        </div>)}
      </nav>
      <div className="ad-session">
        {onLogout && <button type="button" onClick={onLogout}>Đăng xuất</button>}
      </div>
    </aside>
    {['payments', 'dashboard', 'categories', 'products', 'accounts', 'inventory', 'shop', 'ai', 'reviews'].includes(pageId) ? <Page key={pageId} user={user} /> : <>
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
