import { dateTime, useStaffData } from './staffHooks'
import { StaffState } from './StaffState'
import './StaffDashboard.css'

export default function StaffDashboard({ onOpen }) {
  const state = useStaffData('summary')
  const summary = state.data
  return <div className="sf-dashboard">
    <header className="sf-heading"><h1>Tổng quan</h1>
      <button type="button" disabled={state.loading} onClick={state.reload}>Làm mới</button></header>
    <StaffState {...state} />
    {summary && <>
      <div className="sf-metrics">
        {[
          ['Đơn mới chờ xử lý', summary.newOrdersCount, 'orders', { status: 'PENDING' }],
          ['Đơn đang xử lý', summary.processingOrdersCount, 'orders', { status: 'CONFIRMED' }],
          ['Tin chưa đọc', summary.unreadMessagesCount, 'chat', {}],
          ['Hội thoại chờ tiếp nhận', summary.unassignedConversations, 'chat', {}],
          ['Sản phẩm tồn thấp', summary.lowStockCount, 'inventory', {}],
        ].map(([label, value, page, params]) => <button type="button" className="sf-metric" key={label}
          onClick={() => onOpen(page, params)}><span>{label}</span><strong>{value}</strong></button>)}
      </div>
      <section className="sf-panel"><h2>Biến động kho gần đây do bạn thực hiện</h2>
        {!summary.recentMovements.length ? <p>Chưa có hoạt động kho.</p>
          : <ul className="sf-activity">{summary.recentMovements.map((item) => <li key={item.id}>
            <strong>{item.productName}</strong> · {item.delta > 0 ? '+' : ''}{item.delta}
            <p>{item.reason}</p><time dateTime={item.createdAt}>{dateTime(item.createdAt)}</time>
          </li>)}</ul>}
      </section>
    </>}
  </div>
}
