import { useState } from 'react'
import { api } from '../../services/api'
import { dateTime, money, statusLabels, useStaffAction, useStaffData } from './staffHooks'
import { StaffNotice, StaffState } from './StaffState'
import './CustomerManager.css'

export default function CustomerManager({ onOpen }) {
  const state = useStaffData('customers')
  const [query, setQuery] = useState('')
  const [selectedId, setSelectedId] = useState(null)
  const customers = state.data || []
  const selected = customers.find((item) => item.id === selectedId)
  const filtered = customers.filter((item) =>
    [item.fullName, item.username, item.email, item.phoneNumber].filter(Boolean).join(' ')
      .toLocaleLowerCase('vi').includes(query.trim().toLocaleLowerCase('vi')))
  return <>
    <header className="sf-heading"><h1>Quản lý khách hàng</h1>
      <button type="button" onClick={state.reload}>Làm mới</button></header>
    <StaffState {...state} />
    {state.data && <div className="sf-split sf-customers">
      <section className="sf-panel sf-list-panel">
        <input aria-label="Tìm khách hàng" placeholder="Tên, email, điện thoại"
          value={query} onChange={(event) => setQuery(event.target.value)} />
        <div className="sf-table-wrap"><table><thead><tr>
          <th>Khách hàng</th><th>Đơn hàng</th><th>Chi tiêu (đơn hoàn tất)</th><th>Trạng thái</th>
        </tr></thead><tbody>{filtered.map((customer) => <tr key={customer.id}>
          <td><button type="button" aria-pressed={selectedId === customer.id}
            onClick={() => setSelectedId(customer.id)}>{customer.fullName || customer.username}</button>
            <small>{customer.email}</small></td>
          <td>{customer.orderCount}</td><td>{money(customer.totalSpent)}</td>
          <td>{customer.isActive ? 'Hoạt động' : 'Đã khóa'}</td>
        </tr>)}</tbody></table></div>
        {!filtered.length && <p className="sf-state">Không có khách hàng phù hợp.</p>}
      </section>
      {selected ? <CustomerDetails key={`${selected.id}-${selected.isActive}`} customer={selected}
        reload={state.reload} onOpen={onOpen} /> : <aside className="sf-panel">Chọn khách hàng để xem hồ sơ.</aside>}
    </div>}
  </>
}

function CustomerDetails({ customer, reload, onOpen }) {
  const orders = useStaffData(`customers/${customer.id}/orders`)
  const reviews = useStaffData(`customers/${customer.id}/reviews`)
  const action = useStaffAction()
  const [tab, setTab] = useState('orders')
  const changeStatus = async () => {
    if (!window.confirm(`${customer.isActive ? 'Khóa' : 'Mở khóa'} tài khoản ${customer.username}?`)) return
    if (await action.run(() => api.staff.customerStatus(customer.id, !customer.isActive))) reload()
  }
  const openChat = () => action.run(async () => {
    const conversation = await api.chat.open(customer.id)
    onOpen('chat', { conversationId: conversation.id })
  }, '')
  return <aside className="sf-panel sf-customer-detail">
    <h2>{customer.fullName || customer.username}</h2>
    <dl><div><dt>Email</dt><dd>{customer.email}</dd></div>
      <div><dt>Điện thoại</dt><dd>{customer.phoneNumber || '—'}</dd></div>
      <div><dt>Tham gia</dt><dd>{dateTime(customer.createdAt)}</dd></div>
      <div><dt>Chi tiêu (đơn hoàn tất)</dt><dd>{money(customer.totalSpent)}</dd></div>
    </dl>
    <div className="sf-toolbar"><button type="button" disabled={action.busy} onClick={changeStatus}>
      {customer.isActive ? 'Khóa tài khoản' : 'Mở khóa tài khoản'}</button>
      <button type="button" className="sf-primary" disabled={action.busy || !customer.isActive} onClick={openChat}>
        Chat ngay</button></div>
    <StaffNotice notice={action.notice} />
    <div className="sf-toolbar" role="group" aria-label="Thông tin khách hàng">
      <button type="button" aria-pressed={tab === 'orders'} onClick={() => setTab('orders')}>Lịch sử mua sắm</button>
      <button type="button" aria-pressed={tab === 'reviews'} onClick={() => setTab('reviews')}>Đánh giá</button>
    </div>
    {tab === 'orders' ? <><StaffState {...orders} />
      {orders.data && (!orders.data.length ? <p>Chưa có đơn hàng.</p>
        : <ul className="sf-activity">{orders.data.map((order) => <li key={order.id}>
          <button type="button" onClick={() => onOpen('orders', { orderId: order.id })}>Đơn #{order.id}</button>
          <p>{money(order.totalPrice)} · {statusLabels[order.orderStatus] || order.orderStatus}</p>
          <time>{dateTime(order.createdAt)}</time>
        </li>)}</ul>)}</>
      : <><StaffState {...reviews} />
        {reviews.data && (!reviews.data.length ? <p>Chưa có đánh giá.</p>
          : <ul className="sf-activity">{reviews.data.map((review) => <li key={review.id}>
            <strong>Sản phẩm #{review.productId} · {review.rating}/5</strong>
            <p className="sf-prewrap">{review.comment}</p><time>{dateTime(review.createdAt)}</time>
          </li>)}</ul>)}</>}
  </aside>
}
