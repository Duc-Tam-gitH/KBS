import { useState } from 'react'
import { api } from '../../services/api'
import { dateTime, money, useStaffAction, useStaffData } from './staffHooks'
import { StaffNotice, StaffState } from './StaffState'
import './Inventory.css'

export default function Inventory() {
  const state = useStaffData('inventory')
  const [query, setQuery] = useState('')
  const [lowOnly, setLowOnly] = useState(false)
  const [selectedId, setSelectedId] = useState(null)
  const items = state.data || []
  const selected = items.find((item) => item.productId === selectedId)
  const filtered = items.filter((item) => (!lowOnly || item.stockQuantity <= item.lowStockThreshold)
    && `${item.productId} ${item.name}`.toLocaleLowerCase('vi').includes(query.trim().toLocaleLowerCase('vi')))
  return <>
    <header className="sf-heading"><h1>Quản lý kho hàng</h1>
      <button type="button" onClick={state.reload}>Làm mới</button></header>
    <StaffState {...state} />
    {state.data && <div className="sf-split sf-inventory">
      <section className="sf-panel sf-list-panel">
        <div className="sf-toolbar"><input aria-label="Tìm sản phẩm trong kho" placeholder="Mã hoặc tên sản phẩm"
          value={query} onChange={(event) => setQuery(event.target.value)} />
          <label className="sf-checkbox"><input type="checkbox" checked={lowOnly}
            onChange={(event) => setLowOnly(event.target.checked)} /> Chỉ xem tồn thấp</label>
        </div>
        <div className="sf-table-wrap"><table><thead><tr>
          <th>Sản phẩm</th><th>Giá</th><th>Tồn kho</th><th>Ngưỡng</th>
        </tr></thead><tbody>{filtered.map((item) => <tr key={item.productId}>
          <td><button type="button" aria-pressed={selectedId === item.productId}
            onClick={() => setSelectedId(item.productId)}>#{item.productId} · {item.name}</button>
            {!item.isActive && <small>Ngừng bán</small>}</td>
          <td>{money(item.price)}</td><td>{item.stockQuantity}
            {item.stockQuantity <= item.lowStockThreshold && <small className="sf-warning">Cần bổ sung</small>}</td>
          <td>{item.lowStockThreshold}</td>
        </tr>)}</tbody></table></div>
        {!filtered.length && <p className="sf-state">Không có sản phẩm phù hợp.</p>}
      </section>
      {selected ? <InventoryDetails
        key={`${selected.productId}-${selected.stockQuantity}-${selected.lowStockThreshold}`}
        item={selected} reload={state.reload} />
        : <aside className="sf-panel">Chọn sản phẩm để xem tồn kho và lịch sử.</aside>}
    </div>}
  </>
}

function InventoryDetails({ item, reload }) {
  const history = useStaffData(`inventory/${item.productId}/movements`)
  const action = useStaffAction()
  const adjust = async (event) => {
    event.preventDefault()
    const data = new FormData(event.currentTarget)
    const quantity = Number(data.get('quantity'))
    const delta = data.get('direction') === 'out' ? -quantity : quantity
    const ok = await action.run(() => api.staff.adjustStock(item.productId, {
      delta, expectedQuantity: item.stockQuantity, reason: String(data.get('reason')).trim(),
    }))
    if (ok) reload()
  }
  const threshold = async (event) => {
    event.preventDefault()
    const value = Number(new FormData(event.currentTarget).get('threshold'))
    if (await action.run(() => api.staff.threshold(item.productId, value))) reload()
  }
  return <aside className="sf-panel sf-stock-detail">
    <h2>{item.name}</h2><p className="sf-prewrap">{item.description || 'Chưa có mô tả.'}</p>
    <p>Tồn kho: <strong>{item.stockQuantity}</strong></p><p>Cập nhật: {dateTime(item.updatedAt)}</p>
    <StaffNotice notice={action.notice} />
    <form onSubmit={adjust}>
      <h3>Nhập / xuất kho</h3>
      <label>Thao tác<select name="direction"><option value="in">Nhập thêm</option>
        <option value="out">Xuất bớt</option></select></label>
      <label>Số lượng<input name="quantity" type="number" required min="1" max="2147483647" step="1" /></label>
      <label>Lý do<textarea name="reason" required maxLength={500} rows={3} /></label>
      <button className="sf-primary" type="submit" disabled={action.busy}>Lưu biến động kho</button>
    </form>
    <form onSubmit={threshold}><label>Ngưỡng cảnh báo
      <input name="threshold" type="number" min="0" max="2147483647" required defaultValue={item.lowStockThreshold} />
    </label><button type="submit" disabled={action.busy}>Lưu ngưỡng</button></form>
    <h3>Lịch sử biến động</h3><StaffState {...history} />
    {history.data && (!history.data.length ? <p>Chưa có lịch sử biến động.</p>
      : <ul className="sf-activity">{history.data.map((log) => <li key={log.id}>
        <strong>{log.actorName}</strong><time>{dateTime(log.createdAt)}</time>
        <p>{log.quantityBefore} → {log.quantityAfter} ({log.delta > 0 ? '+' : ''}{log.delta})</p>
        <p className="sf-prewrap">{log.reason}</p>{log.orderId && <small>Đơn #{log.orderId}</small>}
      </li>)}</ul>)}
  </aside>
}
