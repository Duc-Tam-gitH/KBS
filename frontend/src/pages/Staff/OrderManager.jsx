import { useState } from 'react'
import { api } from '../../services/api'
import ConfirmBankPayment from '../../components/ConfirmBankPayment'
import { dateTime, money, nextStatuses, statusLabels, useStaffAction, useStaffData } from './staffHooks'
import { StaffNotice, StaffState } from './StaffState'
import './OrderManager.css'

export default function OrderManager({ initialOrderId = null, initialStatus = '' }) {
  const state = useStaffData('orders')
  const [selectedId, setSelectedId] = useState(initialOrderId)
  const [status, setStatus] = useState(initialStatus)
  const [query, setQuery] = useState('')
  const orders = state.data || []
  const selected = orders.find((order) => order.id === selectedId)
  const filtered = orders.filter((order) => (!status || order.orderStatus === status)
    && `${order.id} ${order.customerName} ${order.phoneNumber}`.toLocaleLowerCase('vi')
      .includes(query.trim().toLocaleLowerCase('vi')))
  return <>
    <header className="sf-heading"><h1>Quản lý đơn hàng</h1>
      <button type="button" onClick={state.reload}>Làm mới</button></header>
    <StaffState {...state} />
    {state.data && <div className="sf-split sf-orders">
      <section className="sf-panel sf-list-panel">
        <div className="sf-toolbar">
          <input aria-label="Tìm đơn hàng" placeholder="Mã đơn, người nhận, điện thoại"
            value={query} onChange={(event) => setQuery(event.target.value)} />
          <select aria-label="Trạng thái đơn" value={status} onChange={(event) => setStatus(event.target.value)}>
            <option value="">Tất cả</option>
            {[...new Set([...orders.map((order) => order.orderStatus), ...(status ? [status] : [])])]
              .map((value) => <option key={value} value={value}>{statusLabels[value] || value}</option>)}
          </select>
        </div>
        <div className="sf-table-wrap"><table><thead><tr>
          <th>Mã đơn</th><th>Khách hàng</th><th>Thành tiền</th><th>Trạng thái</th>
        </tr></thead><tbody>
          {filtered.map((order) => <tr key={order.id}>
            <td><button type="button" aria-pressed={selectedId === order.id}
              onClick={() => setSelectedId(order.id)}>#{order.id}</button></td>
            <td>{order.customerName}<small>{order.phoneNumber}</small></td>
            <td>{money(order.totalPrice)}</td><td>{statusLabels[order.orderStatus] || order.orderStatus}</td>
          </tr>)}
        </tbody></table></div>
        {!filtered.length && <p className="sf-state">Không có đơn hàng phù hợp.</p>}
      </section>
      {selected ? <OrderDetails key={`${selected.id}-${selected.orderStatus}`} order={selected} reload={state.reload} />
        : <aside className="sf-panel">Chọn đơn hàng để xem chi tiết.</aside>}
    </div>}
  </>
}

function OrderDetails({ order, reload }) {
  const operations = useStaffData(`orders/${order.id}/operations`)
  const action = useStaffAction()
  const [next, setNext] = useState('')
  const saveStatus = async (event) => {
    event.preventDefault()
    if (next === 'CANCELLED' && !window.confirm(`Hủy đơn #${order.id} và hoàn lại tồn kho?`)) return
    if (await action.run(() => api.staff.orderStatus(order.id, next))) reload()
  }
  const saveTracking = async (event) => {
    event.preventDefault()
    const value = new FormData(event.currentTarget).get('tracking')
    if (await action.run(() => api.staff.tracking(order.id, value))) operations.reload()
  }
  const saveNote = async (event) => {
    event.preventDefault()
    const form = event.currentTarget
    const value = String(new FormData(form).get('content')).trim()
    if (await action.run(() => api.staff.note(order.id, value))) { form.reset(); operations.reload() }
  }
  return <aside className="sf-panel sf-order-detail">
    <section className="sf-invoice">
      <h2>Hóa đơn đơn hàng #{order.id}</h2>
      <p>Ngày đặt: {dateTime(order.createdAt)}</p>
      <p>{order.customerName} · {order.phoneNumber}</p><p>{order.shippingAddress}</p>
      <p>Trạng thái: {statusLabels[order.orderStatus] || order.orderStatus}</p>
      <div className="sf-table-wrap"><table><thead><tr><th>Sản phẩm</th><th>SL</th><th>Đơn giá</th></tr></thead>
        <tbody>{order.orderDetails.map((item) => <tr key={item.id}>
          <td>{item.productName}</td><td>{item.quantity}</td><td>{money(item.unitPrice)}</td>
        </tr>)}</tbody></table></div>
      {!order.orderDetails.length && <p>Chưa có chi tiết sản phẩm.</p>}
      <p className="sf-total">Tổng tiền: {money(order.totalPrice)}</p>
      <p>Thanh toán: {order.paymentMethod} · {order.paymentStatus}</p>
    </section>
    <button type="button" onClick={() => window.print()}>In hóa đơn</button>
    <ConfirmBankPayment order={order} onConfirmed={reload} />
    <StaffNotice notice={action.notice} />
    {!!nextStatuses[order.orderStatus]?.length && <form onSubmit={saveStatus}>
      <label>Trạng thái tiếp theo<select required value={next} onChange={(event) => setNext(event.target.value)}>
        <option value="">Chọn trạng thái</option>
        {nextStatuses[order.orderStatus].map((value) => <option key={value} value={value}>
          {statusLabels[value]}</option>)}
      </select></label>
      <button type="submit" className="sf-primary" disabled={action.busy}>Cập nhật trạng thái</button>
    </form>}
    <StaffState {...operations} />
    {operations.data && <>
      <form key={operations.data.trackingNumber || 'empty'} onSubmit={saveTracking}>
        <label>Mã vận đơn<input name="tracking" maxLength={100} defaultValue={operations.data.trackingNumber || ''} />
        </label><button type="submit" disabled={action.busy}>Lưu mã vận đơn</button>
      </form>
      <form onSubmit={saveNote}><label>Ghi chú nội bộ<textarea name="content" required maxLength={2000} rows={3} />
      </label><button type="submit" disabled={action.busy}>Thêm ghi chú</button></form>
      <h3>Lịch sử ghi chú</h3>
      {!operations.data.notes.length ? <p>Chưa có ghi chú.</p>
        : <ul className="sf-activity">{operations.data.notes.map((note) => <li key={note.id}>
          <strong>{note.staffName}</strong><time>{dateTime(note.createdAt)}</time>
          <p className="sf-prewrap">{note.content}</p>
        </li>)}</ul>}
    </>}
  </aside>
}
