import { useRef, useState } from 'react'
import { api } from '../services/api'

export default function ConfirmBankPayment({ order, onConfirmed }) {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const lock = useRef(false)
  if (order.paymentMethod !== 'BANKING' || order.paymentStatus !== 'PENDING' || order.orderStatus === 'CANCELLED') {
    return null
  }
  const confirm = async () => {
    if (lock.current || !window.confirm(`Bạn đã kiểm tra tài khoản ngân hàng và nhận đủ ${
      new Intl.NumberFormat('vi-VN').format(order.totalPrice)}₫ với nội dung KBS${order.id}?`)) return
    lock.current = true; setBusy(true); setError('')
    try { await api.confirmBankPayment(order.id); onConfirmed() }
    catch (err) { setError(err.message) }
    finally { lock.current = false; setBusy(false) }
  }
  return <div>
    {error && <p role="alert">{error}</p>}
    <button type="button" disabled={busy} onClick={confirm}>
      {busy ? 'Đang xác nhận…' : 'Xác nhận đã nhận chuyển khoản'}</button>
  </div>
}
