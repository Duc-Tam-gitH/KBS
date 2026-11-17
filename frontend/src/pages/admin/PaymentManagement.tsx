import { useEffect, useState, type FormEvent } from 'react'
import { api } from '../../services/api'
import './ShopManagement.css'

type Bank = { bankName: string; bankBin: string; accountNumber: string; accountName: string; sePayBankAccountId: string | null }
type Payment = { orderId: number; amount: number; accountNumber: string; transactionId: string | null; paidAt: string | null }
type Data = { bank: Bank | null; payments: Payment[]; tokenConfigured: boolean; gatewayEnabled: boolean;
  ipnUrl: string; gatewayPayments: { orderId: number; amount: number; status: string; environment: string }[] }
const empty: Bank = { bankName: '', bankBin: '', accountNumber: '', accountName: '', sePayBankAccountId: null }
const fields = [
  ['bankName', 'Tên ngân hàng', 100], ['bankBin', 'BIN ngân hàng (6 chữ số)', 6],
  ['accountNumber', 'Số tài khoản', 19], ['accountName', 'Chủ tài khoản', 150],
  ['sePayBankAccountId', 'ID tài khoản ngân hàng trong SePay (UUID, không bắt buộc)', 36],
] as const

export default function PaymentManagement() {
  const [data, setData] = useState<Data | null>(null)
  const [form, setForm] = useState<Bank>(empty)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [busy, setBusy] = useState(false)
  const [retry, setRetry] = useState(0)
  useEffect(() => {
    const controller = new AbortController()
    const load = async () => {
      try {
        const result: Data = await api.admin.payments(controller.signal)
        if (!controller.signal.aborted) { setData(result); setForm(result.bank ?? empty); setError('') }
      } catch (err: unknown) {
        if (!controller.signal.aborted) setError(err instanceof Error ? err.message : 'Không tải được thanh toán.')
      }
    }
    void load()
    return () => controller.abort()
  }, [retry])
  const save = async (event: FormEvent) => {
    event.preventDefault()
    if (busy) return
    setBusy(true); setNotice(''); setError('')
    try {
      await api.admin.saveBank({ ...form, sePayBankAccountId: form.sePayBankAccountId?.trim() || null })
      setNotice('Đã lưu ngân hàng. Mã QR đã tạo trước đó vẫn giữ tài khoản nhận cũ.')
      setRetry(value => value + 1)
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Không lưu được ngân hàng.')
    } finally { setBusy(false) }
  }
  return <>
    <main className="ad-main sm-layout">
      <header className="ad-heading"><h1>Thanh toán</h1>
        <button type="button" disabled={busy} onClick={() => setRetry(value => value + 1)}>Làm mới</button></header>
      {error && <p role="alert">{error}</p>}
      {notice && <p role="status">{notice}</p>}
      {!data ? <p role="status">Đang tải cấu hình thanh toán…</p> : <>
        <form className="sm-form" onSubmit={save}>
          <fieldset disabled={busy}><legend>Tài khoản nhận chuyển khoản</legend>
            {fields.map(([key, label, maxLength]) => <label key={key}>{label}
              <input name={key} value={form[key] ?? ''} maxLength={maxLength}
                required={key !== 'sePayBankAccountId'}
                pattern={key === 'bankBin' ? '[0-9]{6}' : key === 'accountNumber' ? '[0-9]{6,19}' : undefined}
                onChange={event => setForm(old => ({ ...old, [key]: event.target.value }))} />
            </label>)}
          </fieldset>
          <button className="ad-primary" disabled={busy}>{busy ? 'Đang lưu…' : 'Lưu ngân hàng'}</button>
        </form>
        <h2>Lịch sử mã thanh toán (50 đơn gần nhất)</h2>
        {data.gatewayPayments.map(payment => <p key={`gateway-${payment.orderId}`}>
          SePay {payment.environment} · Đơn #{payment.orderId} · {payment.amount.toLocaleString('vi-VN')} ₫ · {payment.status}
        </p>)}
        {!data.payments.length ? <p>Chưa có mã thanh toán.</p> : <ul>{data.payments.map(payment =>
          <li key={payment.orderId}>Đơn #{payment.orderId} · {payment.amount.toLocaleString('vi-VN')} ₫ · {payment.accountNumber}
            <p>{payment.transactionId ? `Đã đối soát: ${payment.transactionId}` : 'Chưa có giao dịch đối soát tự động'}</p>
          </li>)}</ul>}
      </>}
    </main>
    <aside className="ad-details ad-panel"><h2>Đối soát tự động</h2>
      {data?.gatewayEnabled ? <>
        <p>SePay Payment Gateway Sandbox đang bật. Thanh toán mô phỏng, không chuyển tiền thật.</p>
        <p>URL IPN:</p><code style={{ overflowWrap: 'anywhere' }}>{data.ipnUrl}</code>
        <p>Ngân hàng nhận tiền trên trang thanh toán được quản lý tại SePay. Thông tin ngân hàng trong form dành cho QR trực tiếp.</p>
      </> : <>
      <p>{data?.tokenConfigured ? 'Backend đã có API token SePay.' : 'Backend chưa cấu hình API token SePay.'}</p>
      <p>Liên kết ngân hàng với SePay, sau đó nhập đúng ID tài khoản tương ứng. Backend đọc giao dịch mỗi 30 giây khi đang chạy, kể cả trên localhost.</p>
      <p>Đã cấu hình không đồng nghĩa đã kết nối thành công. Chỉ giao dịch được đối soát mới được ghi nhận đã thanh toán.</p>
      </>}
      <p>Đơn từ 10.000.000 ₫ trở lên luôn cần người duyệt, kể cả khi đã nhận đủ tiền.</p>
    </aside>
  </>
}
