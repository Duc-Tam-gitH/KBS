import { useEffect, useRef, useState } from 'react'
import { api } from '../../services/api'
import './Checkout.css'

const money = value => new Intl.NumberFormat('vi-VN').format(value) + '₫'

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
    api.paymentStatus(orderId, controller.signal)
      .then(async status => {
        if (controller.signal.aborted) return
        setPayment(status)
        if (method === 'BANKING' && status.paymentStatus === 'PENDING' && status.orderStatus !== 'CANCELLED') {
          const details = await api.createPayment(orderId)
          if (!controller.signal.aborted) setPayment(details)
        }
      })
      .catch(err => { if (!controller.signal.aborted) setError(err.message) })
    return () => { mounted.current = false; controller.abort() }
  }, [orderId, method])

  const run = async create => {
    if (lock.current) return
    lock.current = true; setBusy(true); setError('')
    try {
      const next = create ? await api.createPayment(orderId) : await api.paymentStatus(orderId)
      if (mounted.current) {
        setPayment(previous => ({ ...previous, ...next,
          ...(create ? {} : { qrImageUrl: previous?.qrImageUrl,
            bankName: previous?.bankName, accountName: previous?.accountName,
            accountNumber: previous?.accountNumber, transferContent: previous?.transferContent }) }))
        if (create) setImageFailed(false)
      }
    } catch (err) { if (mounted.current) setError(err.message) }
    finally { lock.current = false; if (mounted.current) setBusy(false) }
  }

  return <section className="co-payment-panel" aria-label={`Thanh toán đơn ${orderId}`}>
    <h3>Chuyển khoản VietQR</h3>
    {error && <p role="alert" className="co-error">{error}</p>}
    {!payment && !error && <p role="status">Đang tải thông tin thanh toán…</p>}
    {payment?.paymentStatus === 'PAID' ? <p role="status">Đã xác nhận thanh toán {money(payment.amount)}.</p>
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
            <p>Chuyển đúng số tiền và nội dung. Nhân viên sẽ xác nhận sau khi đối chiếu tài khoản ngân hàng.</p>
          </>}
        </>}
    {payment?.paymentStatus !== 'PAID' && payment?.orderStatus !== 'CANCELLED' && <div className="co-payment-actions">
      <button type="button" className="co-primary" disabled={busy} onClick={() => run(true)}>
        {busy ? 'Đang xử lý…' : 'Tải lại mã QR'}</button>
      <button type="button" className="co-text-button" disabled={busy} onClick={() => run(false)}>
        Kiểm tra trạng thái</button>
    </div>}
  </section>
}
