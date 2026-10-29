import { useState } from 'react'
import { Package, Eye, History, Download, ArrowDownToLine, ArrowUpFromLine } from 'lucide-react'
import { api } from '../../services/api'
import { dateTime, useStaffAction, useStaffData } from '../Staff/staffHooks'
import { stockLabels, stockStatus, filterInventory, stockAdjustment, inventoryCsv } from './inventoryUtils'
import './InventoryManagement.css'

const emptyFilters = { query: '', category: '', status: '', sort: 'asc' }
const modeLabels = { in: 'Nhập kho', out: 'Xuất kho', adjust: 'Điều chỉnh tồn thực tế' }

export function StockForm({ items, mode, busy, onConfirm, onClose }) {
  const [error, setError] = useState('')
  return <form className="iv-form" onSubmit={event => {
    event.preventDefault()
    const form = new FormData(event.currentTarget)
    try {
      const changes = items.map(item => ({ item, payload: stockAdjustment(item, mode,
        Number(form.get(String(item.productId))), String(form.get('reason'))) }))
      setError(''); onConfirm(changes)
    } catch (err) { setError(err.message) }
  }}>
    <h2>{modeLabels[mode]}</h2>
    <p>{mode === 'adjust' ? 'Nhập số tồn thực tế của từng sản phẩm.' : 'Nhập số lượng thay đổi cho từng sản phẩm.'}</p>
    <fieldset disabled={busy}>{items.map(item => <label key={item.productId}>
      #{item.productId} · {item.name} (hiện có {item.stockQuantity})
      <input name={String(item.productId)} type="number" required step="1"
        min={mode === 'adjust' ? 0 : 1} max={mode === 'out' ? item.stockQuantity : 2147483647} />
    </label>)}
      <label>Lý do<textarea name="reason" required maxLength={500} rows={3} /></label>
    </fieldset>
    {error && <p role="alert">{error}</p>}
    <div className="iv-actions"><button type="submit" className="iv-primary" disabled={busy}>Tiếp tục</button>
      <button type="button" disabled={busy} onClick={onClose}>Đóng</button></div>
  </form>
}

function StockDetails({ item, reload }) {
  const history = useStaffData(`inventory/${item.productId}/movements`)
  const action = useStaffAction()
  return <>
    <h2>#{item.productId} · {item.name}</h2>
    <dl><dt>Danh mục</dt><dd>{item.categoryName}</dd><dt>Tồn khả dụng</dt><dd>{item.stockQuantity}</dd>
      <dt>Đã đặt trong đơn đang xử lý</dt><dd>{item.orderedQuantity}</dd>
      <dt>Trạng thái sản phẩm</dt><dd>{item.isActive ? 'Đang bán' : 'Ngừng bán'}</dd>
      <dt>Cập nhật tồn</dt><dd>{dateTime(item.updatedAt)}</dd></dl>
    {action.notice && <p role={action.notice.error ? 'alert' : 'status'}>{action.notice.text}</p>}
    <form className="iv-form" onSubmit={async event => {
      event.preventDefault()
      const value = Number(new FormData(event.currentTarget).get('threshold'))
      if (await action.run(() => api.staff.threshold(item.productId, value))) reload()
    }}>
      <label>Ngưỡng cảnh báo<input name="threshold" type="number" min="0" max="2147483647" step="1"
        defaultValue={item.lowStockThreshold} required /></label>
      <button type="submit" disabled={action.busy}>Lưu ngưỡng</button>
    </form>
    <h3>Lịch sử nhập / xuất</h3>
    {history.error ? <p role="alert">{history.error} <button type="button" onClick={history.reload}>Thử lại</button></p>
      : history.loading ? <p role="status">Đang tải lịch sử…</p>
        : !history.data.length ? <p>Chưa có lịch sử biến động.</p>
          : <ul className="iv-history">{history.data.map(log => <li key={log.id}>
            <strong>{log.actorName}</strong><time dateTime={log.createdAt}>{dateTime(log.createdAt)}</time>
            <p>{log.quantityBefore} → {log.quantityAfter} ({log.delta > 0 ? '+' : ''}{log.delta})</p>
            <p>{log.reason}</p>{log.orderId && <small>Đơn #{log.orderId}</small>}
          </li>)}</ul>}
  </>
}

export function InventoryTable({ items, selected = [], busy, onSelect, onView, onOperation }) {
  if (!items.length) return <p className="iv-empty">Không có sản phẩm phù hợp.</p>
  return <div className="iv-table-wrap"><table><thead><tr>
    <th><input type="checkbox" aria-label="Chọn tất cả trên trang" disabled={busy}
      checked={items.every(item => selected.includes(item.productId))}
      onChange={event => onSelect(event.target.checked ? items.map(item => item.productId) : [])} /></th>
    <th>Sản phẩm</th><th>Mã sản phẩm</th><th>Tồn kho</th><th>Đã đặt</th><th>Trạng thái</th><th>Cập nhật</th><th>Hành động</th>
  </tr></thead><tbody>{items.map(item => <tr key={item.productId}>
    <td><input type="checkbox" aria-label={`Chọn ${item.name}`} disabled={busy} checked={selected.includes(item.productId)}
      onChange={() => onSelect(selected.includes(item.productId) ? selected.filter(id => id !== item.productId)
        : [...selected, item.productId])} /></td>
    <td><strong>{item.name}</strong><small>{item.categoryName}</small>{!item.isActive && <small>Ngừng bán</small>}</td>
    <td>#{item.productId}</td><td><strong>{item.stockQuantity}</strong></td><td>{item.orderedQuantity}</td>
    <td><span className={`iv-status iv-status-${stockStatus(item)}`}>{stockLabels[stockStatus(item)]}</span></td>
    <td>{dateTime(item.updatedAt)}</td><td><div className="iv-actions">
      <button type="button" aria-label={`Chi tiết ${item.name}`} disabled={busy} onClick={() => onView(item.productId)}><Eye size={16} /></button>
      <button type="button" aria-label={`Nhập ${item.name}`} disabled={busy} onClick={() => onOperation('in', [item])}><ArrowDownToLine size={16} /></button>
      <button type="button" aria-label={`Xuất ${item.name}`} disabled={busy || !item.stockQuantity}
        onClick={() => onOperation('out', [item])}><ArrowUpFromLine size={16} /></button>
      <button type="button" aria-label={`Lịch sử ${item.name}`} disabled={busy} onClick={() => onView(item.productId)}><History size={16} /></button>
    </div></td>
  </tr>)}</tbody></table></div>
}

export default function InventoryManagement() {
  const state = useStaffData('inventory')
  const action = useStaffAction()
  const [filters, setFilters] = useState(emptyFilters)
  const [showFilters, setShowFilters] = useState(false)
  const [page, setPage] = useState(1)
  const [size, setSize] = useState(10)
  const [selected, setSelected] = useState([])
  const [detailId, setDetailId] = useState(null)
  const [operation, setOperation] = useState(null)
  const [confirmation, setConfirmation] = useState(null)
  const [report, setReport] = useState(null)
  const items = state.data || []
  const filtered = filterInventory(items, filters)
  const categories = [...new Map(items.map(item => [item.categoryId, item.categoryName])).entries()]
  const totalPages = Math.max(1, Math.ceil(filtered.length / size))
  const currentPage = Math.min(page, totalPages)
  const visible = filtered.slice((currentPage - 1) * size, currentPage * size)
  const detail = items.find(item => item.productId === detailId)
  const updateFilter = (key, value) => { setFilters(old => ({ ...old, [key]: value })); setPage(1); setSelected([]) }
  const reload = () => { setSelected([]); state.reload() }
  const openOperation = (mode, products) => {
    setOperation({ mode, items: products }); setConfirmation(null); setReport(null); setDetailId(null)
  }
  const exportCsv = () => {
    const url = URL.createObjectURL(new Blob([inventoryCsv(filtered)], { type: 'text/csv;charset=utf-8' }))
    const link = document.createElement('a'); link.href = url; link.download = 'ton-kho.csv'
    document.body.appendChild(link); link.click(); link.remove(); setTimeout(() => URL.revokeObjectURL(url), 1000)
  }
  const commit = async () => {
    const failures = []; let succeeded = 0
    const ok = await action.run(async () => {
      for (const change of confirmation) {
        try { await api.staff.adjustStock(change.item.productId, change.payload); succeeded++ }
        catch (err) { failures.push(`#${change.item.productId}: ${err.message}`) }
      }
    }, null)
    if (ok) {
      setReport({ succeeded, total: confirmation.length, failures }); setConfirmation(null); setOperation(null); reload()
    }
  }
  const controlsDisabled = action.busy || state.loading || !!state.error
  return <>
    <main className="ad-main iv-layout">
      <header className="ad-heading"><div><h1><Package size={24} /> Quản lý kho</h1><p>Theo dõi tồn kho và lịch sử nhập xuất</p></div>
        <div className="iv-actions">{['in', 'out', 'adjust'].map(mode => <button key={mode} type="button"
          disabled={controlsDisabled || !selected.length}
          onClick={() => openOperation(mode, items.filter(item => selected.includes(item.productId)))}>{modeLabels[mode]}</button>)}</div>
      </header>
      <div className="iv-stats">{[['', 'Tổng sản phẩm'], ...Object.entries(stockLabels)].map(([status, label]) =>
        <button type="button" key={status} aria-pressed={filters.status === status} onClick={() => updateFilter('status', status)}>
          <span>{label}</span><strong>{state.data ? items.filter(item => !status || stockStatus(item) === status).length : '—'}</strong>
        </button>)}</div>
      <p className="iv-muted">Tồn kho là số lượng khả dụng đã trừ khi đặt hàng. “Đã đặt” thuộc đơn chờ xác nhận,
        đã xác nhận hoặc đang giao. Sắp hết: tồn lớn hơn 0 và không vượt ngưỡng từng sản phẩm.</p>
      <div className="iv-toolbar"><input aria-label="Tìm sản phẩm" placeholder="Tên hoặc mã sản phẩm"
        value={filters.query} onChange={e => updateFilter('query', e.target.value)} />
        <button type="button" aria-expanded={showFilters} onClick={() => setShowFilters(value => !value)}>Bộ lọc</button>
        <button type="button" disabled={controlsDisabled || !filtered.length} onClick={exportCsv}><Download size={16} /> Xuất CSV (Excel)</button>
        <button type="button" disabled={action.busy} onClick={() => { setOperation(null); setConfirmation(null); reload() }}>Tải lại</button>
      </div>
      {showFilters && <div className="iv-filters"><label>Danh mục<select value={filters.category}
        onChange={e => updateFilter('category', e.target.value)}><option value="">Tất cả</option>
        {categories.map(([id, name]) => <option key={id} value={id}>{name}</option>)}</select></label>
        <label>Trạng thái tồn<select value={filters.status} onChange={e => updateFilter('status', e.target.value)}>
          <option value="">Tất cả</option>{Object.entries(stockLabels).map(([key, name]) => <option key={key} value={key}>{name}</option>)}
        </select></label><label>Sắp xếp<select value={filters.sort} onChange={e => updateFilter('sort', e.target.value)}>
          <option value="asc">Tồn tăng dần</option><option value="desc">Tồn giảm dần</option>
          <option value="name">Tên A → Z</option><option value="updated">Mới cập nhật</option></select></label>
      </div>}
      {(filters.query || filters.category || filters.status) && <div className="iv-actions">
        {filters.status && <button type="button" onClick={() => updateFilter('status', '')}>{stockLabels[filters.status]} ×</button>}
        {filters.category && <button type="button" onClick={() => updateFilter('category', '')}>{categories.find(([id]) => String(id) === filters.category)?.[1]} ×</button>}
        <button type="button" onClick={() => { setFilters(emptyFilters); setSelected([]); setPage(1) }}>Xóa bộ lọc</button>
      </div>}
      <p className="iv-muted">{selected.length ? `Đã chọn ${selected.length} sản phẩm.` : 'Chọn sản phẩm để nhập, xuất hoặc điều chỉnh hàng loạt.'}</p>
      {report && <div className="iv-notice" role={report.failures.length ? 'alert' : 'status'}>
        <p>Đã cập nhật {report.succeeded}/{report.total} sản phẩm.</p>
        {report.failures.map(message => <p key={message}>{message}</p>)}
      </div>}
      {action.notice && <p role="alert">{action.notice.text}</p>}
      {state.error ? <p role="alert">{state.error}</p> : state.loading ? <p role="status">Đang tải tồn kho…</p>
        : !items.length ? <p className="iv-empty">Chưa có sản phẩm trong kho.</p>
          : <InventoryTable items={visible} selected={selected} busy={action.busy} onSelect={setSelected}
            onView={id => { setDetailId(id); setOperation(null); setConfirmation(null) }} onOperation={openOperation} />}
      <div className="iv-pagination"><span>{filtered.length ? (currentPage - 1) * size + 1 : 0}–{Math.min(currentPage * size, filtered.length)} / {filtered.length}</span>
        <button type="button" disabled={currentPage === 1 || action.busy} onClick={() => { setPage(currentPage - 1); setSelected([]) }}>Trước</button>
        <span>Trang {currentPage}/{totalPages}</span><button type="button" disabled={currentPage === totalPages || action.busy}
          onClick={() => { setPage(currentPage + 1); setSelected([]) }}>Sau</button>
        <select aria-label="Số sản phẩm mỗi trang" value={size} onChange={e => { setSize(Number(e.target.value)); setPage(1); setSelected([]) }}>
          {[5, 10, 20, 50].map(value => <option key={value} value={value}>{value} / trang</option>)}
        </select></div>
    </main>
    <aside className="ad-details ad-panel iv-details" aria-label="Chi tiết kho">
      {confirmation ? <section><h2>Xác nhận cập nhật kho</h2>
        {confirmation.map(({ item, payload }) => <p key={item.productId}>#{item.productId} · {item.name}: {payload.expectedQuantity} → {payload.expectedQuantity + payload.delta}</p>)}
        <p>Lý do: {confirmation[0]?.payload.reason}</p><p>Mỗi sản phẩm được xử lý riêng; kết quả sẽ nêu các mục thất bại.</p>
        <div className="iv-actions"><button type="button" className="iv-primary" disabled={controlsDisabled} onClick={commit}>
          {action.busy ? 'Đang lưu…' : 'Xác nhận'}</button><button type="button" disabled={action.busy} onClick={() => setConfirmation(null)}>Quay lại</button></div>
      </section> : operation ? <StockForm key={`${operation.mode}:${operation.items.map(item => item.productId).join(',')}`}
        items={operation.items} mode={operation.mode} busy={controlsDisabled} onConfirm={setConfirmation} onClose={() => setOperation(null)} />
        : detail ? <StockDetails key={`${detail.productId}:${detail.updatedAt}:${detail.lowStockThreshold}`} item={detail} reload={reload} />
          : <p>Chọn sản phẩm để xem chi tiết, lịch sử hoặc cập nhật tồn kho.</p>}
    </aside>
  </>
}
