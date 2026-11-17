import { useEffect, useRef, useState } from 'react'
import { api } from '../../services/api'
import { dateTime } from '../Staff/staffHooks'
import './ReviewModeration.css'

const statuses = { FLAGGED: 'Cần kiểm tra', PENDING: 'Chưa quét', APPROVED: 'Đã duyệt', HIDDEN: 'Đã ẩn' }

export default function ReviewModeration() {
  const [status, setStatus] = useState('FLAGGED')
  const [page, setPage] = useState(1)
  const [data, setData] = useState(null)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [busy, setBusy] = useState(false)
  const [retry, setRetry] = useState(0)
  const lock = useRef(false)
  useEffect(() => {
    const controller = new AbortController()
    setData(null); setError('')
    api.admin.reviews(status, page, controller.signal).then(result => {
      if (!controller.signal.aborted) {
        if (page > 1 && !result.items.length) setPage(Math.max(1, result.totalPages))
        else setData(result)
      }
    }).catch(err => { if (!controller.signal.aborted) setError(err.message) })
    return () => controller.abort()
  }, [status, page, retry])
  const moderate = async (id, next) => {
    if (lock.current) return
    lock.current = true; setBusy(true); setNotice(''); setError('')
    try {
      await api.admin.moderateReview(id, next)
      setNotice(next === 'HIDDEN' ? 'Đã ẩn đánh giá khỏi trang sản phẩm và thống kê sao.' : 'Đã duyệt hiển thị đánh giá.')
      setRetry(value => value + 1)
    } catch (err) { setError(err.message || 'Không thể duyệt đánh giá.') }
    finally { lock.current = false; setBusy(false) }
  }
  return <main className="ad-main review-moderation">
    <header className="ad-heading"><div><h1>Duyệt đánh giá</h1>
      <p>AI chỉ gắn cờ nghi vấn. Nhân viên quyết định giữ hoặc ẩn; nội dung gốc được giữ nguyên.</p></div>
      <button type="button" disabled={busy} onClick={() => setRetry(x => x + 1)}>Làm mới</button></header>
    <label>Trạng thái <select value={status} disabled={busy}
      onChange={e => { setStatus(e.target.value); setPage(1); setNotice('') }}>
      <option value="">Tất cả</option>
      {Object.entries(statuses).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
    </select></label>
    {error && <p role="alert">{error}</p>}
    {notice && <p role="status">{notice}</p>}
    {!data ? !error && <p role="status">Đang tải đánh giá…</p>
      : !data.items.length ? <p>Không có đánh giá phù hợp.</p>
        : <div className="review-table"><table>
          <thead><tr><th>Sản phẩm / Khách hàng</th><th>Đánh giá gốc</th><th>Kiểm tra</th><th>Thao tác</th></tr></thead>
          <tbody>{data.items.map(item => <tr key={item.id}>
            <td><strong>{item.productName}</strong><p>{item.displayName}</p><small>#{item.id} · {dateTime(item.createdAt)}</small></td>
            <td><span>{item.rating}/5 sao</span><p className="review-comment">{item.comment}</p></td>
            <td><strong>{statuses[item.status] || item.status}</strong>
              <p>Từ khóa nghi vấn: {item.reason || 'Không có'}</p><small>{dateTime(item.checkedAt)}</small></td>
            <td><div className="review-actions">
              <button type="button" disabled={busy || item.status === 'APPROVED'}
                onClick={() => moderate(item.id, 'APPROVED')}>Giữ đánh giá</button>
              <button type="button" disabled={busy || item.status === 'HIDDEN'}
                onClick={() => moderate(item.id, 'HIDDEN')}>Ẩn đánh giá</button>
            </div></td>
          </tr>)}</tbody>
        </table></div>}
    {data && <nav className="review-actions" aria-label="Phân trang đánh giá">
      <button type="button" disabled={busy || page <= 1} onClick={() => setPage(x => x - 1)}>Trước</button>
      <span>Trang {page}/{Math.max(1, data.totalPages)} · {data.totalItems} đánh giá</span>
      <button type="button" disabled={busy || page >= data.totalPages} onClick={() => setPage(x => x + 1)}>Sau</button>
    </nav>}
  </main>
}
