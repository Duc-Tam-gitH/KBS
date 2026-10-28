import { useEffect, useState } from 'react'
import { api } from '../../services/api'
import ConfirmBankPayment from '../../components/ConfirmBankPayment'
import { dateTime, money, statusLabels } from '../Staff/staffHooks'
import './Dashboard.css'

const tabs = [['overview', 'Tổng quan'], ['revenue', 'Doanh thu'], ['orders', 'Đơn hàng']]
const metrics = [
  ['revenue', 'Doanh thu đã thanh toán', 'Tổng giá trị các đơn đã thanh toán, toàn thời gian.'],
  ['totalOrders', 'Tổng đơn hàng', 'Tất cả đơn hàng đã được ghi nhận.'],
  ['pendingOrders', 'Đơn đang chờ', 'Các đơn có trạng thái PENDING.'],
  ['totalCustomers', 'Khách hàng', 'Số tài khoản có vai trò Customer.'],
  ['lowStockProducts', 'Sản phẩm sắp hết', 'Số sản phẩm có tồn kho nhỏ hơn hoặc bằng ngưỡng cảnh báo.'],
]
const amount = (key, value) => value == null ? '--' : key === 'revenue' ? money(value) : value.toLocaleString('vi-VN')
const dayLabel = value => new Date(value + 'T00:00:00Z').toLocaleDateString('vi-VN', { timeZone: 'UTC' })

export default function Dashboard({ user }) {
  if (user?.roleName !== 'Admin') return <p role="alert">Không có quyền truy cập.</p>
  return <DashboardWorkspace key={user.id} />
}

function DashboardWorkspace() {
  const [tab, setTab] = useState('overview')
  const [retry, setRetry] = useState(0)
  const [result, setResult] = useState(null)
  const [selection, setSelection] = useState(null)
  const resource = tab === 'orders' ? 'orders' : 'dashboard'
  const requestKey = resource + ':' + retry
  const current = result?.key === requestKey ? result : null

  useEffect(() => {
    const controller = new AbortController()
    api.admin[resource](controller.signal)
      .then(data => {
        if (!controller.signal.aborted) setResult({ key: requestKey, data })
      })
      .catch(error => {
        if (!controller.signal.aborted) setResult({ key: requestKey, error: error.message })
      })
    return () => controller.abort()
  }, [resource, requestKey])

  const changeTab = next => { setTab(next); setSelection(null) }
  const reload = () => { setSelection(null); setRetry(value => value + 1) }

  return <>
    <div className="ad-main">
      <header className="ad-heading">
        <div><p className="ad-eyebrow">BẢNG ĐIỀU KHIỂN</p><h1>{tabs.find(([key]) => key === tab)[1]}</h1></div>
        <button type="button" className="ad-primary" onClick={reload} disabled={!current}>Làm mới</button>
      </header>
      <nav className="ad-dashboard-tabs" aria-label="Dữ liệu Dashboard">
        {tabs.map(([key, label]) => <button type="button" key={key}
          aria-pressed={tab === key} onClick={() => changeTab(key)}>{label}</button>)}
      </nav>
      {!current ? <p className="ad-panel" role="status">Đang tổng hợp số liệu…</p>
        : current.error ? <div className="ad-panel" role="alert">
          <h2>Không thể tải dữ liệu</h2><p>{current.error}</p>
          <button type="button" onClick={reload}>Thử lại</button>
        </div>
          : <DashboardContent tab={tab} data={current.data} selection={selection} onSelect={setSelection} />}
    </div>
    <aside className="ad-details ad-panel" aria-label="Chi tiết dữ liệu" aria-live="polite">
      <p className="ad-eyebrow">CHI TIẾT DỮ LIỆU</p>
      <DashboardDetails selection={current?.error ? null : selection} onPaymentConfirmed={reload} />
    </aside>
  </>
}

export function DashboardContent({ tab, data, selection, onSelect }) {
  if (!data) return <p className="ad-panel">Chưa có dữ liệu.</p>
  if (tab === 'orders') return <section className="ad-panel">
    <h2>Danh sách đơn hàng</h2>
    <p className="ad-muted">Chọn mã đơn để xem thông tin bên phải.</p>
    {!data.length ? <p className="ad-empty">Chưa có đơn hàng.</p> : <div className="ad-table-wrap">
      <table><thead><tr><th>Mã đơn</th><th>Người nhận</th><th>Tổng tiền</th><th>Trạng thái</th></tr></thead>
        <tbody>{data.map(order => <tr key={order.id}>
          <td><button type="button" aria-pressed={selection?.kind === 'order' && selection.value.id === order.id}
            onClick={() => onSelect({ kind: 'order', value: order })}>#{order.id}</button></td>
          <td>{order.customerName || '--'}</td><td>{money(order.totalPrice)}</td>
          <td>{statusLabels[order.orderStatus] || order.orderStatus || '--'}</td>
        </tr>)}</tbody>
      </table>
    </div>}
  </section>
  const days = data.revenueLast7Days || []
  const maxRevenue = Math.max(1, ...days.map(day => day.revenue))
  return <>
    {tab === 'overview' && <div className="ad-metrics">
      {metrics.map(([key, label, description]) => <button type="button" className="ad-panel ad-metric" key={key}
        aria-pressed={selection?.kind === 'metric' && selection.key === key}
        onClick={() => onSelect({ kind: 'metric', key, label, description, value: data[key] })}>
        <span>{label}</span><strong>{amount(key, data[key])}</strong><small>Xem chi tiết →</small>
      </button>)}
    </div>}
    <section className="ad-panel">
      <h2>Doanh thu 7 ngày gần nhất</h2>
      <p className="ad-muted">Đơn đã thanh toán, nhóm theo ngày đặt (UTC). Chọn ngày để xem chi tiết.</p>
      {!days.length ? <p className="ad-empty">Chưa có dữ liệu trong 7 ngày gần nhất.</p>
        : <div className="ad-chart">{days.map(day => <button type="button" className="ad-chart-row" key={day.date}
          aria-pressed={selection?.kind === 'day' && selection.value.date === day.date}
          onClick={() => onSelect({ kind: 'day', value: day })}>
          <span>{dayLabel(day.date)}</span>
          <span className="ad-bar-track" aria-hidden="true">
            <span className="ad-bar" style={{ width: Math.max(0, day.revenue / maxRevenue * 100) + '%' }} />
          </span>
          <strong>{money(day.revenue)}</strong>
        </button>)}</div>}
    </section>
  </>
}

export function DashboardDetails({ selection, onPaymentConfirmed }) {
  if (!selection) return <p className="ad-empty">Chọn một chỉ số, ngày hoặc đơn hàng để xem chi tiết.</p>
  if (selection.kind === 'metric') return <>
    <h2>{selection.label}</h2><p className="ad-detail-value">{amount(selection.key, selection.value)}</p>
    <p className="ad-muted">{selection.description}</p>
  </>
  if (selection.kind === 'day') return <>
    <h2>{dayLabel(selection.value.date)}</h2>
    <dl><dt>Doanh thu đã thanh toán</dt><dd>{money(selection.value.revenue)}</dd>
      <dt>Số đơn được tạo</dt><dd>{selection.value.orders}</dd></dl>
    <p className="ad-muted">Ngày thống kê theo UTC; số đơn bao gồm mọi trạng thái thanh toán.</p>
  </>
  const order = selection.value
  return <>
    <h2>Đơn hàng #{order.id}</h2>
    <dl>
      <dt>Người nhận</dt><dd>{order.customerName || '--'}</dd>
      <dt>Điện thoại</dt><dd>{order.phoneNumber || '--'}</dd>
      <dt>Địa chỉ</dt><dd>{order.shippingAddress || '--'}</dd>
      <dt>Ngày đặt</dt><dd>{dateTime(order.createdAt)}</dd>
      <dt>Trạng thái</dt><dd>{statusLabels[order.orderStatus] || order.orderStatus || '--'}</dd>
      <dt>Thanh toán</dt><dd>{order.paymentMethod || '--'} · {order.paymentStatus || '--'}</dd>
    </dl>
    <h3>Sản phẩm</h3>
    {!order.orderDetails?.length ? <p className="ad-empty">Chưa có chi tiết sản phẩm.</p>
      : <ul className="ad-items">{order.orderDetails.map(item => <li key={item.id}>
        <strong>{item.productName || '--'}</strong><span>{item.quantity} × {money(item.unitPrice)}</span>
      </li>)}</ul>}
    <div className="ad-total"><span>Tổng tiền</span><strong>{money(order.totalPrice)}</strong></div>
    <ConfirmBankPayment order={order} onConfirmed={onPaymentConfirmed} />
  </>
}
