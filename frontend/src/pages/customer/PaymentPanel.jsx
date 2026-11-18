import { useEffect, useRef, useState } from 'react'
import { CheckCircle2 } from 'lucide-react'
import { api } from '../../services/api'
import './Checkout.css'

const money = value => new Intl.NumberFormat('vi-VN').format(value) + '₫'

export const shouldPollPayment = payment => payment.orderStatus !== 'CANCELLED'
  && (payment.paymentStatus === 'PENDING'
    || (payment.paymentStatus === 'PAID' && payment.orderStatus === 'PENDING'))

export function PaymentSuccess({ payment }) {
  if (payment?.paymentStatus !== 'PAID') return null
  const orderMessage = {
    CONFIRMED: 'Đơn hàng đã được xác nhận.', SHIPPING: 'Đơn hàng đang được giao.',
    COMPLETED: 'Đơn hàng đã hoàn tất.', CANCELLED: 'Đơn đã hủy. Vui lòng liên hệ shop để kiểm tra khoản thanh toán.',
  }[payment.orderStatus]
  return <div className="co-payment-success" role="status" aria-live="polite" aria-atomic="true">
    <CheckCircle2 size={28} aria-hidden="true" />
    <div><h4>Thanh toán thành công</h4>
      <p>Đã nhận {money(payment.amount)} cho đơn #{payment.orderId}. Bạn không cần chuyển khoản lại.</p>
      <p>{orderMessage || (payment.amount >= 10000000
        ? 'Đơn từ 10.000.000 ₫ cần nhân viên duyệt sau khi nhận tiền.'
        : 'Đang chờ xác nhận đơn hàng. Trạng thái sẽ tự cập nhật.')}</p>
    </div>
  </div>
}

export default function PaymentPanel({ orderId, method }) {
  const [payment, setPayment] = useState(null)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const [imageFailed, setImageFailed] = useState(false)
  const lock = useRef(false)
  const mounted = useRef(false)
  useEffect(() => {
    mounted.current = true
    const controller = new AbortController()
    let timer
    setPayment(null); setError(''); setImageFailed(false)
    const refresh = async (initial = false) => {
      let pending = true
      try {
        let status = await api.paymentStatus(orderId, controller.signal)
        if (initial && method === 'BANKING' && status.paymentStatus === 'PENDING'
          && status.orderStatus !== 'CANCELLED') status = await api.createPayment(orderId, controller.signal)
        pending = shouldPollPayment(status)
        if (!controller.signal.aborted) {
          setPayment(previous => initial ? status : { ...previous, orderId: status.orderId,
            paymentStatus: status.paymentStatus, orderStatus: status.orderStatus, amount: status.amount })
          setError('')
        }
      } catch (err) {
        if (!controller.signal.aborted) setError(err.message)
      } finally {
        if (!controller.signal.aborted && pending) timer = setTimeout(() => void refresh(), 5000)
      }
    }
    void refresh(true)
    return () => { mounted.current = false; controller.abort(); clearTimeout(timer) }
  }, [orderId, method])

  const run = async create => {
    if (lock.current) return
    lock.current = true; setBusy(true); setError('')
    try {
      const next = create ? await api.createPayment(orderId) : await api.paymentStatus(orderId)
      if (mounted.current) {
        setPayment(previous => create ? next : { ...previous, orderId: next.orderId,
          paymentStatus: next.paymentStatus, orderStatus: next.orderStatus, amount: next.amount })
        if (create) setImageFailed(false)
      }
    } catch (err) { if (mounted.current) setError(err.message) }
    finally { lock.current = false; if (mounted.current) setBusy(false) }
  }

  return <section className="co-payment-panel" aria-label={`Thanh toán đơn ${orderId}`}>
    <h3>Thanh toán chuyển khoản</h3>
    {error && <p role="alert" className="co-error">{error}</p>}
    {!payment && !error && <p role="status">Đang tải thông tin thanh toán…</p>}
    {payment?.paymentStatus === 'PAID' ? <PaymentSuccess payment={payment} />
      : payment?.paymentStatus === 'REVIEW' ? <p role="status">Giao dịch cần nhân viên kiểm tra. Vui lòng không thanh toán lại.</p>
      : payment?.orderStatus === 'CANCELLED' ? <p>Đơn đã hủy. Vui lòng không chuyển tiền cho đơn này.</p>
        : payment && <>
          <p>Chờ thanh toán: <strong>{money(payment.amount)}</strong></p>
          {method === 'BANKING' && payment.qrImageUrl && <>
            {!imageFailed ? <img className="co-bank-qr" src={payment.qrImageUrl} alt={`QR chuyển khoản đơn ${orderId}`}
              referrerPolicy="no-referrer" onError={() => setImageFailed(true)} />
              : <p role="alert">Không tải được ảnh QR. Bạn có thể chuyển khoản theo thông tin bên dưới.</p>}
            <dl><div><dt>Ngân hàng</dt><dd>{payment.bankName}</dd></div>
              <div><dt>Số tài khoản</dt><dd>{payment.accountNumber}</dd></div>
              <div><dt>Chủ tài khoản</dt><dd>{payment.accountName}</dd></div>
              <div><dt>Nội dung</dt><dd>{payment.transferContent}</dd></div></dl>
            <p>Chuyển đúng số tiền và nội dung. Trạng thái tự cập nhật sau khi đối soát ngân hàng hoặc nhân viên xác nhận.</p>
            <p>Quét mã bằng ứng dụng ngân hàng để chuyển tiền thật vào tài khoản trên.</p>
          </>}
        </>}
    {payment?.paymentStatus === 'PENDING' && payment?.orderStatus !== 'CANCELLED' && <div className="co-payment-actions">
      <button type="button" className="co-primary" disabled={busy} onClick={() => run(true)}>
        {busy ? 'Đang xử lý…' : 'Tải lại thanh toán'}</button>
      <button type="button" className="co-text-button" disabled={busy} onClick={() => run(false)}>
        Kiểm tra trạng thái</button>
    </div>}
  </section>
}
