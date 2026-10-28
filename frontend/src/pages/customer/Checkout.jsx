import { useEffect, useRef, useState } from 'react'
import { ArrowLeft, ArrowRight, Banknote, Building2, Check, CheckCircle2, Package, Truck } from 'lucide-react'
import { api, ApiError } from '../../services/api'
import ShippingAddress from './ShippingAddress'
import PaymentPanel from './PaymentPanel'
import './Checkout.css'

const money = value => new Intl.NumberFormat('vi-VN').format(value ?? 0) + '₫'

export default function Checkout({ cart, user, busy = false, cartError = '', onPlaced, onDone, onBack, onShop }) {
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState('')
  const [order, setOrder] = useState(null)
  const [payment, setPayment] = useState('COD')
  const [region, setRegion] = useState('')
  const [options, setOptions] = useState(null)
  const [optionsError, setOptionsError] = useState('')
  const lock = useRef(false)
  const total = cart.reduce((sum, item) => sum + item.price * item.quantity, 0)
  const count = cart.reduce((sum, item) => sum + item.quantity, 0)
  useEffect(() => {
    const controller = new AbortController()
    api.paymentOptions(controller.signal)
      .then(data => { if (!controller.signal.aborted) setOptions(data) })
      .catch(() => {
        if (!controller.signal.aborted) setOptionsError('Chưa tải được phương thức trực tuyến. Bạn vẫn có thể chọn COD.')
      })
    return () => controller.abort()
  }, [])

  const submit = async event => {
    event.preventDefault()
    if (lock.current || busy || cartError || !cart.length) return
    const form = new FormData(event.currentTarget)
    const customerName = form.get('name').trim()
    const street = form.get('address').trim()
    const shippingAddress = `${street}, ${region}`
    if (!customerName || !street || !region || shippingAddress.length > 500) {
      setError('Vui lòng nhập tên, địa chỉ cụ thể và chọn đủ tỉnh, huyện, xã; địa chỉ tối đa 500 ký tự.')
      return
    }
    if (payment === 'BANKING' && !options?.bankAvailable) {
      setError('Phương thức thanh toán chưa được cấu hình. Vui lòng chọn COD.')
      return
    }
    lock.current = true
    setSubmitting(true)
    setError('')
    let saved
    try {
      saved = await api.createOrder({
        customerName,
        phoneNumber: form.get('phone').trim(),
        shippingAddress,
        paymentMethod: payment,
        orderItems: cart.map(item => ({ productId: item.id, quantity: item.quantity })),
      })
    } catch (err) {
      lock.current = false
      setSubmitting(false)
      setError(err instanceof ApiError ? err.message : 'Không thể tạo đơn hàng. Vui lòng thử lại.')
      return
    }
    setOrder(saved)
    setSubmitting(false)
    onPlaced()
  }

  return <section className="kbs-checkout">
    <nav className="co-progress" aria-label="Tiến trình đặt hàng">
      {['Giỏ hàng', 'Thanh toán', 'Hoàn tất'].map((label, index) => <span key={label}
        className={index < (order ? 2 : 1) ? 'is-complete' : ''}
        aria-current={index === (order ? 2 : 1) ? 'step' : undefined}>
        <i>{index < (order ? 2 : 1) ? <Check size={13} aria-hidden="true" /> : index + 1}</i>{label}
      </span>)}
    </nav>
    {order ? <div className="co-success co-panel" role="status">
      <CheckCircle2 size={48} className="co-success-icon" aria-hidden="true" />
      <p className="co-eyebrow">CẢM ƠN BẠN ĐÃ CHỌN KBS</p>
      <h1>Đặt hàng thành công</h1>
      <p>Đơn hàng <strong>#{order.id}</strong> đã được ghi nhận.</p>
      <dl><div><dt>Người nhận</dt><dd>{order.customerName}</dd></div>
        <div><dt>Địa chỉ</dt><dd>{order.shippingAddress}</dd></div>
        <div><dt>Thanh toán</dt><dd>{order.paymentMethod === 'COD' ? 'Khi nhận hàng'
          : 'Chuyển khoản'}</dd></div>
        <div><dt>Tổng đơn hàng</dt><dd>{money(order.totalPrice)}</dd></div></dl>
      {order.paymentMethod === 'COD' ? <p>Bạn thanh toán khi nhận hàng.</p>
        : <PaymentPanel orderId={order.id} method={order.paymentMethod} />}
      <button type="button" className="co-primary" onClick={onDone}>Xem đơn hàng<ArrowRight size={17} /></button>
      {onShop && <button type="button" className="co-text-button" onClick={onShop}>Tiếp tục mua sắm</button>}
    </div> : <>
      <header className="co-heading">
        {onBack && <button type="button" className="co-back" onClick={onBack} disabled={submitting}>
          <ArrowLeft size={16} aria-hidden="true" />Quay lại giỏ hàng</button>}
        <h1>Thanh toán</h1>
        <p>Điền thông tin giao hàng và chọn cách thanh toán phù hợp với bạn.</p>
      </header>
      {busy ? <div className="co-panel co-empty" role="status">Đang tải giỏ hàng…</div>
        : cartError ? <div className="co-panel co-empty" role="alert"><h2>Chưa thể thanh toán</h2>
          <p>{cartError}</p>{onBack && <button type="button" className="co-primary" onClick={onBack}>
            Quay lại giỏ hàng</button>}</div>
          : !cart.length ? <div className="co-panel co-empty"><Package size={40} aria-hidden="true" />
            <h2>Không có sản phẩm để thanh toán</h2><p>Quay lại cửa hàng để thêm sản phẩm.</p>
            {onShop && <button type="button" className="co-primary" onClick={onShop}>Khám phá sản phẩm</button>}</div>
            : <form className="co-layout" onSubmit={submit}>
              <div className="co-panel co-sections">
                <section className="co-section" aria-labelledby="co-shipping">
                  <h2 id="co-shipping"><span className="co-step">1</span>Thông tin giao hàng</h2>
                  <fieldset disabled={submitting} className="co-fields">
                    <label>Họ và tên<input name="name" autoComplete="name" required maxLength={100}
                      defaultValue={user?.fullName || ''} placeholder="Nhập tên người nhận" /></label>
                    <label>Số điện thoại<input name="phone" type="tel" autoComplete="tel" required
                      pattern="[0-9+ \(\)\-]{8,20}" maxLength={20} defaultValue={user?.phoneNumber || ''}
                      placeholder="Nhập số điện thoại liên hệ" /></label>
                    {user?.email && <div className="co-account-email co-wide"><span>Email tài khoản</span>
                      <p>{user.email}</p></div>}
                    <ShippingAddress onChange={setRegion} disabled={submitting} />
                  </fieldset>
                </section>
                <section className="co-section" aria-labelledby="co-delivery">
                  <h2 id="co-delivery"><span className="co-step">2</span>Giao nhận</h2>
                  <div className="co-delivery"><Truck size={23} aria-hidden="true" /><div>
                    <strong>Giao đến địa chỉ của bạn</strong>
                    <p>Kiểm tra thông tin người nhận trước khi đặt hàng. Theo dõi tiến trình giao hàng trong đơn hàng.</p>
                  </div></div>
                </section>
                <section className="co-section" aria-labelledby="co-payment">
                  <h2 id="co-payment"><span className="co-step">3</span>Phương thức thanh toán</h2>
                  <p className="co-section-description">Chọn phương thức thanh toán cho đơn hàng của bạn.</p>
                  {optionsError && <p role="alert" className="co-error">{optionsError}</p>}
                  <fieldset disabled={submitting} className="co-payment-options" aria-label="Phương thức thanh toán">
                    {[['COD', 'Thanh toán khi nhận hàng (COD)', 'Thanh toán khi đơn hàng được giao đến bạn.', Banknote],
                      ['BANKING', 'Chuyển khoản ngân hàng', 'Quét VietQR với số tiền và nội dung đơn hàng điền sẵn.',
                        Building2]
                    ].map(([value, title, description, Icon]) =>
                      <div key={value} className={`co-payment-option ${payment === value ? 'is-selected' : ''}`}>
                        <label><input type="radio" name="payment" value={value} checked={payment === value}
                          disabled={value === 'BANKING' && !options?.bankAvailable}
                          onChange={() => setPayment(value)} /><span><strong>{title}</strong><small>{description}</small></span>
                        <Icon size={21} aria-hidden="true" /></label>
                        {payment === value && <div className="co-payment-detail">
                          <span>{value === 'COD' ? 'Số tiền thanh toán khi nhận hàng' : 'Giá trị đơn hàng'}</span>
                          <strong>{money(total)}</strong>
                          {value === 'BANKING' && <p>Đặt hàng chưa đồng nghĩa với đã thanh toán.</p>}
                        </div>}
                      </div>)}
                  </fieldset>
                </section>
              </div>
              <aside className="co-summary co-panel" aria-labelledby="co-summary-title">
                <header><h2 id="co-summary-title">Đơn hàng</h2><span>{count} sản phẩm</span></header>
                <ul className="co-items">{cart.map(item => <li key={item.id}>
                  <div className="co-product-image">
                    {item.imageUrl ? <img src={item.imageUrl} alt={item.name} />
                      : <Package size={25} aria-label="Chưa có ảnh sản phẩm" />}
                    <span>{item.quantity}</span>
                  </div>
                  <div className="co-product-info"><strong>{item.name}</strong>
                    {(item.color || item.layout) && <p>{[item.color, item.layout].filter(Boolean).join(' · ')}</p>}
                    <p>{money(item.price)} × {item.quantity}</p></div>
                  <strong className="co-line-price">{money(item.price * item.quantity)}</strong>
                </li>)}</ul>
                <div className="co-totals"><div><span>Tạm tính</span><strong>{money(total)}</strong></div>
                  <div className="co-grand-total"><span>Tổng cộng</span><strong>{money(total)}</strong></div>
                  <p>{payment === 'COD' ? 'Thanh toán khi nhận hàng.'
                    : 'Quét mã VietQR sau khi đặt đơn.'}</p>
                </div>
                <div className="co-summary-action">
                  {error && <p className="co-error" role="alert">{error}</p>}
                  <button type="submit" className="co-primary" disabled={submitting}>
                    {submitting ? 'Đang xử lý…' : 'Đặt hàng'}<ArrowRight size={18} aria-hidden="true" /></button>
                  <p>Vui lòng kiểm tra sản phẩm và địa chỉ trước khi xác nhận.</p>
                </div>
              </aside>
            </form>}
    </>}
  </section>
}
