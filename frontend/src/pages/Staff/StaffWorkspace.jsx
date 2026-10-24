import { useState } from 'react'
import StaffDashboard from './StaffDashboard'
import OrderManager from './OrderManager'
import Inventory from './Inventory'
import CustomerManager from './CustomerManager'
import SupportChat from './SupportChat'
import './Staff.css'

const menus = [
  ['dashboard', 'Tổng quan'], ['orders', 'Quản lý đơn hàng'], ['inventory', 'Kho hàng'],
  ['customers', 'Khách hàng'], ['chat', 'Hỗ trợ & Chat'],
]

export default function StaffWorkspace({ user, onNavigate, onLogout }) {
  const [view, setView] = useState({ page: 'dashboard' })
  if (!['Admin', 'Staff'].includes(user?.roleName)) return <p role="alert">Không có quyền truy cập.</p>
  const open = (page, params = {}) => setView({ page, ...params })
  return <section className="staff-workspace">
    <aside className="sf-sidebar">
      <h2>KBS · Nhân viên</h2>
      <nav aria-label="Chức năng nhân viên">
        {menus.map(([key, label]) => <button type="button" key={key}
          aria-current={view.page === key ? 'page' : undefined} onClick={() => open(key)}>{label}</button>)}
      </nav>
      <div className="sf-session"><strong>{user.fullName || user.username}</strong><p>{user.roleName}</p>
        <button type="button" onClick={() => onNavigate('home')}>Về cửa hàng</button>
        <button type="button" onClick={onLogout}>Đăng xuất</button>
      </div>
    </aside>
    <div className="sf-content">
      {view.page === 'dashboard' && <StaffDashboard onOpen={open} />}
      {view.page === 'orders' && <OrderManager initialOrderId={view.orderId} initialStatus={view.status} />}
      {view.page === 'inventory' && <Inventory />}
      {view.page === 'customers' && <CustomerManager onOpen={open} />}
      {view.page === 'chat' && <SupportChat key={view.conversationId || 'inbox'}
        user={user} initialConversationId={view.conversationId} />}
    </div>
  </section>
}
