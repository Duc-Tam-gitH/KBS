import { useEffect, useRef, useState } from 'react'
import { ArrowLeft, Check, Package, ShoppingCart, Star, UserRound } from 'lucide-react'
import { api } from '../../services/api'
import PaymentPanel from './PaymentPanel'
import './orderProduct.css'

// Labels translate database status codes; they do not assign an order's status.
const orderLabels = {
  PENDING: 'Chờ xác nhận',
  CONFIRMED: 'Đã xác nhận',
  SHIPPING: 'Đang giao hàng',
  COMPLETED: 'Hoàn thành',
  CANCELLED: 'Đã hủy',
}
const paymentLabels = { PENDING: 'Chờ thanh toán', PAID: 'Đã thanh toán', CANCELLED: 'Đã hủy thanh toán' }
const paymentMethods = { COD: 'Thanh toán khi nhận hàng', BANKING: 'Chuyển khoản' }
const money = (value) => new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(value)

function OrderImage({ item }) {
  const [failed, setFailed] = useState(false)
  return item.imageUrl && !failed
    ? <img src={item.imageUrl} alt={item.productName} loading="lazy" onError={() => setFailed(true)} />
    : <span className="op-image-empty"><Package size={32} aria-hidden="true" /><span>Chưa có ảnh</span></span>
}

export default function OrderProduct({ user = null, onNavigate, onAdd, busy = false }) {
  const [orders, setOrders] = useState(null)
  const [error, setError] = useState('')
  const [retry, setRetry] = useState(0)
  const [filter, setFilter] = useState('')
  const [paymentFilter, setPaymentFilter] = useState('')
  const [confirmId, setConfirmId] = useState(null)
  const [working, setWorking] = useState(false)
  const [notice, setNotice] = useState(null)
  const lock = useRef(false)
  const mounted = useRef(true)

  useEffect(() => {
    mounted.current = true
    return () => { mounted.current = false }
  }, [])

  useEffect(() => {
    if (!user) return
    let active = true
    api.myOrders()
      .then((items) => { if (active) setOrders(items) })
      .catch((err) => { if (active) setError(err.message || 'Không thể tải đơn hàng.') })
    return () => { active = false }
  }, [user, retry])

  const reload = () => {
    setOrders(null)
    setError('')
    setConfirmId(null)
    setRetry((value) => value + 1)
  }

  const runAction = async (action) => {
    if (lock.current) return
    lock.current = true
    setWorking(true)
    setNotice(null)
    try {
      await action()
    } catch (err) {
      if (mounted.current) setNotice({ error: true, text: err.message || 'Thao tác thất bại. Vui lòng thử lại.' })
    } finally {
      lock.current = false
      if (mounted.current) setWorking(false)
    }
  }

  const confirmReceipt = (id) => runAction(async () => {
    const updated = await api.confirmOrderReceipt(id)
    if (!mounted.current) return
    setOrders((items) => items.map((order) => order.id === updated.id ? updated : order))
    setConfirmId(null)
    setNotice({ text: `Đã xác nhận nhận hàng cho đơn #${updated.id}.` })
  })

  const buyAgain = (item) => runAction(async () => {
    if (busy || !item.productIsActive) return
    const product = await api.product(item.productId)
    if (!mounted.current) return
    if (!product.isActive || product.stockQuantity < item.quantity) {
      throw new Error('Sản phẩm không còn đủ số lượng đã mua. Hãy mở chi tiết để chọn lại.')
    }
    if (item.quantity > 100) throw new Error('Vui lòng mở chi tiết sản phẩm để chọn tối đa 100 sản phẩm mỗi lần.')
    if (!onAdd || await onAdd(product, item.quantity) === false) {
      throw new Error('Chưa thêm được sản phẩm vào giỏ hàng. Vui lòng kiểm tra thông báo và thử lại.')
    }
    if (mounted.current) setNotice({ cart: true, text: 'Đã thêm sản phẩm vào giỏ hàng theo giá hiện tại.' })
  })

  if (!user) {
    return <section className="order-product-page op-state">
      <UserRound size={40} aria-hidden="true" /><h1>Đơn hàng của tôi</h1>
      <p>Đăng nhập để xem đơn hàng của bạn.</p>
      <button type="button" className="op-primary" onClick={() => onNavigate?.('orders')}>Đăng nhập</button>
    </section>
  }

  const statuses = [...new Set((orders || []).map((order) => order.orderStatus))].filter(Boolean)
  const paymentStatuses = [...new Set((orders || []).map((order) => order.paymentStatus))].filter(Boolean)
  const selectedStatus = statuses.includes(filter) ? filter : ''
  const selectedPayment = paymentStatuses.includes(paymentFilter) ? paymentFilter : ''
  const filtered = (orders || []).filter((order) => (!selectedStatus || order.orderStatus === selectedStatus)
    && (!selectedPayment || order.paymentStatus === selectedPayment))

  return (
    <section className="order-product-page">
      <header className="op-heading">
        <div><p className="op-eyebrow">Tài khoản</p><h1>Đơn hàng của tôi</h1></div>
        <button type="button" onClick={() => onNavigate?.('shop')}>
          <ArrowLeft size={18} aria-hidden="true" /> Tiếp tục mua sắm
        </button>
      </header>
      <div className="op-layout">
        <aside className="op-sidebar">
          <div className="op-user">
            <UserRound size={40} aria-hidden="true" />
            <div><strong>{user.fullName || user.username || '—'}</strong>
              <button type="button" onClick={() => onNavigate?.('account')}>Tài khoản của tôi</button>
            </div>
          </div>
          <nav className="op-menu" aria-label="Lọc trạng thái đơn hàng">
            <button type="button" aria-pressed={!selectedStatus} onClick={() => setFilter('')}>
              Tất cả {orders && <span>{orders.length}</span>}
            </button>
            {statuses.map((status) => <button type="button" key={status}
              aria-pressed={selectedStatus === status} onClick={() => setFilter(status)}>
              {orderLabels[status] || status}
              <span>{orders.filter((order) => order.orderStatus === status).length}</span>
            </button>)}
          </nav>
        </aside>
        <div className="op-main">
          <div className="op-toolbar">
            <label htmlFor="op-payment-filter">Thanh toán</label>
            <select id="op-payment-filter" value={selectedPayment} onChange={(e) => setPaymentFilter(e.target.value)}>
              <option value="">Tất cả</option>
              {paymentStatuses.map((status) => <option key={status} value={status}>
                {paymentLabels[status] || status}
              </option>)}
            </select>
            <button type="button" disabled={working || (!orders && !error)} onClick={reload}>Tải lại</button>
          </div>
          {notice && <div className="op-notice" role={notice.error ? 'alert' : 'status'}>
            <p>{notice.text}</p>
            {notice.cart && <button type="button" onClick={() => onNavigate?.('cart')}>Xem giỏ hàng</button>}
          </div>}
          {error ? <div className="op-state" role="alert"><p>{error}</p>
            <button type="button" onClick={reload}>Thử lại</button>
          </div>
            : !orders ? <div className="op-state" role="status">Đang tải đơn hàng…</div>
              : !filtered.length ? <div className="op-state">
                <Package size={48} aria-hidden="true" />
                <h2>{orders.length ? 'Không có đơn hàng phù hợp' : 'Bạn chưa có đơn hàng nào'}</h2>
                {orders.length
                  ? <button type="button" onClick={() => { setFilter(''); setPaymentFilter('') }}>Xóa bộ lọc</button>
                  : <button type="button" className="op-primary" onClick={() => onNavigate?.('shop')}>
                    Khám phá sản phẩm
                  </button>}
              </div>
                : <div className="op-orders">
                  {filtered.map((order) => <article className="op-card" key={order.id}>
                    <header className="op-card-header">
                      <div><h2>Đơn hàng #{order.id}</h2>
                        <time dateTime={order.createdAt}>
                          {new Date(order.createdAt).toLocaleString('vi-VN')}
                        </time>
                      </div>
                      <span className="op-status">{orderLabels[order.orderStatus] || order.orderStatus || '—'}</span>
                    </header>
                    <div className="op-items">
                      {!order.orderDetails?.length ? <p>Chưa có chi tiết sản phẩm.</p>
                        : order.orderDetails.map((item) => <div className="op-item" key={item.id}>
                          <div className="op-item-image"><OrderImage key={item.imageUrl} item={item} /></div>
                          <div className="op-item-description">
                            <button type="button" className="op-product-name" disabled={!item.productIsActive}
                              onClick={() => onNavigate?.('detail', { id: item.productId })}>
                              {item.productName || 'Chưa có tên sản phẩm'}
                            </button>
                            <p>Số lượng: {item.quantity}</p>
                            {!item.productIsActive && <p>Sản phẩm đã ngừng bán</p>}
                            {order.orderStatus === 'COMPLETED' && <div className="op-item-actions">
                              <button type="button" disabled={busy || working || !item.productIsActive}
                                onClick={() => buyAgain(item)}>
                                <ShoppingCart size={16} aria-hidden="true" /> Mua lại
                              </button>
                              <button type="button" disabled={!item.productIsActive}
                                onClick={() => onNavigate?.('detail', { id: item.productId, section: 'reviews' })}>
                                <Star size={16} aria-hidden="true" /> Đánh giá
                              </button>
                            </div>}
                          </div>
                          <div className="op-item-price">
                            <span>Đơn giá lúc mua</span><strong>{money(item.unitPrice)}</strong>
                            <span>{money(item.unitPrice * item.quantity)} / {item.quantity} sản phẩm</span>
                          </div>
                        </div>)}
                    </div>
                    <details className="op-delivery">
                      <summary>Thông tin giao nhận và thanh toán</summary>
                      <dl>
                        <div><dt>Người nhận</dt><dd>{order.customerName || '—'}</dd></div>
                        <div><dt>Số điện thoại</dt><dd>{order.phoneNumber || '—'}</dd></div>
                        <div><dt>Địa chỉ</dt><dd>{order.shippingAddress || '—'}</dd></div>
                        <div><dt>Phương thức</dt>
                          <dd>{paymentMethods[order.paymentMethod] || order.paymentMethod}</dd></div>
                        <div><dt>Thanh toán</dt>
                          <dd>{paymentLabels[order.paymentStatus] || order.paymentStatus}</dd></div>
                      </dl>
                      {order.paymentMethod === 'BANKING' && order.paymentStatus === 'PENDING'
                        && order.orderStatus !== 'CANCELLED' && <OrderPayment order={order} />}
                    </details>
                    <footer className="op-card-footer">
                      <p>Thành tiền: <strong>{money(order.totalPrice)}</strong></p>
                      {order.orderStatus === 'SHIPPING' && (confirmId === order.id
                        ? <div className="op-confirm" role="group" aria-label={`Xác nhận nhận đơn #${order.id}`}>
                          <p>Bạn đã nhận đủ sản phẩm trong đơn hàng này?</p>
                          <button type="button" disabled={working} onClick={() => setConfirmId(null)}>Quay lại</button>
                          <button type="button" className="op-primary" disabled={working}
                            onClick={() => confirmReceipt(order.id)}>
                            {working ? 'Đang xác nhận…' : 'Xác nhận đã nhận hàng'}
                          </button>
                        </div>
                        : <button type="button" className="op-primary" disabled={working}
                          onClick={() => setConfirmId(order.id)}>
                          <Check size={18} aria-hidden="true" /> Đã nhận hàng
                        </button>)}
                    </footer>
                  </article>)}
                </div>}
        </div>
      </div>
    </section>
  )
}

function OrderPayment({ order }) {
  const [open, setOpen] = useState(false)
  return open ? <PaymentPanel orderId={order.id} method={order.paymentMethod} />
    : <button type="button" onClick={() => setOpen(true)}>Tiếp tục thanh toán</button>
}
