import { useEffect, useState } from 'react'
import { api } from '../../services/api'
import { dateTime, useStaffAction } from '../Staff/staffHooks'
import './ShopManagement.css'
import { homeFields } from '../../components/homeDefaults'

const fields = ['name', 'slogan', 'description', 'hotline', 'email', 'warehouseAddress',
  'bankName', 'bankBin', 'accountNumber', 'accountName']
const formFrom = data => Object.fromEntries(fields.map(key => [key, data.shop?.[key] ?? data[key] ?? '']))
const changed = () => window.dispatchEvent(new Event('shop-settings-changed'))

export default function ShopManagement() {
  const [form, setForm] = useState(null)
  const [saved, setSaved] = useState(null)
  const [error, setError] = useState('')
  const [retry, setRetry] = useState(0)
  const [tab, setTab] = useState('general')
  const [homeContent, setHomeContent] = useState({})
  const [availabilityConfirm, setAvailabilityConfirm] = useState(false)
  const action = useStaffAction()
  useEffect(() => {
    const controller = new AbortController()
    api.admin.shop(controller.signal).then(data => {
      if (!controller.signal.aborted) {
        setForm(formFrom(data)); setSaved(data.shop); setHomeContent(data.shop.homeContent || {}); setError('')
      }
    }).catch(err => { if (!controller.signal.aborted) setError(err.message) })
    return () => controller.abort()
  }, [retry])
  const input = (key, label, maxLength, type = 'text', required = false) => <label>{label}
    <input name={key} type={type} maxLength={maxLength} required={required} value={form[key]}
      onChange={e => setForm(old => ({ ...old, [key]: e.target.value }))} />
  </label>
  const save = async event => {
    event.preventDefault()
    await action.run(async () => {
      const payload = Object.fromEntries(fields.map(key => [key, form[key].trim() || null]))
      payload.name ||= 'KBS'
      payload.homeContent = homeContent
      const data = await api.admin.saveShop(payload)
      setForm(formFrom(data)); setSaved(data.shop); changed()
    }, 'Đã lưu cấu hình cửa hàng và cập nhật nội dung Home.')
  }
  const toggleAvailability = () => action.run(async () => {
    await api.admin.shopAvailability(saved?.isOpen === false)
    const data = await api.admin.shop(); setSaved(data.shop); setAvailabilityConfirm(false); changed()
  }, saved?.isOpen === false ? 'Đã mở website.' : 'Đã đóng website. Admin vẫn truy cập quản trị được.')
  return <>
    <main className="ad-main sm-layout">
      <header className="ad-heading"><div><h1>Quản lý cửa hàng</h1><p>Nội dung Home cũ và thông tin nhận chuyển khoản.</p></div>
        <button type="button" disabled={!form || action.busy || !!error} onClick={() => setAvailabilityConfirm(true)}>
          {saved?.isOpen === false ? 'Mở website' : 'Đóng website'}</button></header>
      <p role="status">Trạng thái: {saved ? (saved.isOpen === false ? 'Đang đóng' : 'Đang mở') : 'Đang tải…'}</p>
      {availabilityConfirm && <div className="sm-notice"><p>Xác nhận {saved?.isOpen === false ? 'mở' : 'đóng'} website?</p>
        <button type="button" disabled={action.busy} onClick={toggleAvailability}>Xác nhận</button>
        <button type="button" disabled={action.busy} onClick={() => setAvailabilityConfirm(false)}>Hủy</button></div>}
      <nav className="sm-tabs" aria-label="Cấu hình cửa hàng">{[['general', 'Quản lý trang Home'], ['contact', 'Liên hệ & Kho'],
        ['payment', 'Tài khoản ngân hàng']].map(([key, label]) => <button type="button" key={key}
          aria-pressed={tab === key} onClick={() => setTab(key)}>{label}</button>)}</nav>
      {action.notice && <p role={action.notice.error ? 'alert' : 'status'} className="sm-notice">{action.notice.text}</p>}
      {error ? <p role="alert">{error} <button type="button" onClick={() => setRetry(value => value + 1)}>Thử lại</button></p>
        : !form ? <p role="status">Đang tải cấu hình cửa hàng…</p>
          : <form className="sm-form" noValidate onSubmit={save}>
            <fieldset disabled={action.busy}>
              <section hidden={tab !== 'general'}><h2>Quản lý trang Home</h2>
                <p>Giữ nguyên bố cục Home cũ. Các ô bên dưới chứa nội dung gốc; chỉnh sửa rồi lưu để áp dụng.</p>
                <button type="button" onClick={() => {
                  if (window.confirm('Khôi phục toàn bộ nội dung Home gốc? Bấm Lưu thay đổi để áp dụng.')) setHomeContent({})
                }}>Khôi phục nội dung Home gốc</button>
                {[...new Set(homeFields.map(field => field.group))].map(group => <details key={group} className="sm-home-group">
                  <summary>{group}</summary>
                  {homeFields.filter(field => field.group === group).map(field => <label key={field.key}>{field.label}
                    {field.kind === 'image' ? <><input type="url" value={homeContent[field.key] ?? field.defaultValue}
                      onChange={e => setHomeContent(old => ({ ...old, [field.key]: e.target.value }))} />
                      <img className="sm-home-preview" src={homeContent[field.key] ?? field.defaultValue} alt={field.label} /></>
                      : <textarea rows={2} maxLength={5000} value={homeContent[field.key] ?? field.defaultValue}
                        onChange={e => setHomeContent(old => ({ ...old, [field.key]: e.target.value }))} />}
                  </label>)}
                </details>)}
              </section>
              <section hidden={tab !== 'contact'}><h2>Thông tin cửa hàng và liên hệ</h2>
                <p>Thông tin liên hệ nội bộ. Nội dung hiển thị trên Home được chỉnh trong tab Quản lý trang Home.</p>
                {input('name', 'Tên cửa hàng', 150)}
                {input('slogan', 'Slogan', 100)}
                <label>Giới thiệu<textarea name="description" maxLength={2000} rows={5} value={form.description}
                  onChange={e => setForm(old => ({ ...old, description: e.target.value }))} /></label>
                {input('hotline', 'Hotline', 30, 'tel')}{input('email', 'Email liên hệ', 254, 'email')}
                <label>Địa chỉ kho<textarea name="warehouseAddress" maxLength={500} rows={3} value={form.warehouseAddress}
                  onChange={e => setForm(old => ({ ...old, warehouseAddress: e.target.value }))} /></label>
              </section>
              <section hidden={tab !== 'payment'}><h2>Tài khoản nhận chuyển khoản</h2>
                <p>Nhập đầy đủ bốn trường để dùng chuyển khoản/VietQR, hoặc để trống cả bốn để chỉ dùng COD.</p>
                {input('bankName', 'Tên ngân hàng', 100)}{input('bankBin', 'Mã BIN ngân hàng (6 chữ số)', 6)}
                {input('accountNumber', 'Số tài khoản (6–19 chữ số)', 19)}{input('accountName', 'Tên chủ tài khoản', 150)}
                <p className="ad-muted">COD luôn khả dụng. QR hỗ trợ chuyển khoản; nhân viên vẫn phải đối chiếu tiền nhận trước khi xác nhận thanh toán.</p>
              </section>
            </fieldset>
            <button type="submit" className="ad-primary" disabled={action.busy}>{action.busy ? 'Đang lưu…' : 'Lưu thay đổi'}</button>
          </form>}
    </main>
    <aside className="ad-details ad-panel sm-details"><h2>Nội dung đang lưu</h2>
      <dl><dt>Tên cửa hàng</dt><dd>{saved?.name || 'Chưa có thông tin'}</dd>
        <dt>Slogan</dt><dd>{saved?.slogan || '—'}</dd><dt>Hotline</dt><dd>{saved?.hotline || '—'}</dd>
        <dt>Email</dt><dd>{saved?.email || '—'}</dd><dt>Địa chỉ kho</dt><dd>{saved?.warehouseAddress || '—'}</dd>
        <dt>Cập nhật</dt><dd>{dateTime(saved?.updatedAt)}</dd></dl>
      <p>Home cập nhật khi mở trang, quay lại tab hoặc tự làm mới trong vòng 30 giây.</p>
    </aside>
  </>
}
