import { useEffect, useState } from 'react'
import { Bot, Activity, Bell, ChevronRight } from 'lucide-react'
import { api } from '../../services/api'
import { useSessionView } from '../../services/useSessionView'
import { dateTime, money, useStaffAction } from '../Staff/staffHooks'
import './AIManagement.css'

const connections = {
  NOT_INTEGRATED: 'Chưa tích hợp', NEVER_CONNECTED: 'Chưa kết nối', ONLINE: 'Đã kết nối', OFFLINE: 'Mất kết nối',
}
const kinds = {
  ENABLED: 'Đã bật', DISABLED: 'Đã tắt', CONFIRMED: 'Đã xác nhận', MANUAL_REVIEW: 'Cần staff xác nhận', ERROR: 'Lỗi',
}

export default function AIManagement({ readOnly = false }) {
  const [modules, setModules] = useState(null)
  const [selectedId, setSelectedId] = useSessionView('ai.selected', null, value => typeof value === 'string')
  const [applied, setApplied] = useSessionView('ai.filters', { filter: '', kind: '', from: '', to: '' },
    value => value && ['filter', 'kind', 'from', 'to'].every(key => typeof value[key] === 'string')
      && (!value.from || Number.isFinite(Date.parse(value.from)))
      && (!value.to || Number.isFinite(Date.parse(value.to)))
      && !(value.from && value.to && value.from > value.to))
  const [filter, setFilter] = useState(applied.filter)
  const [kind, setKind] = useState(applied.kind)
  const [from, setFrom] = useState(applied.from)
  const [to, setTo] = useState(applied.to)
  const [beforeId, setBeforeId] = useSessionView('ai.beforeId', null,
    value => Number.isSafeInteger(value) && value > 0)
  const [events, setEvents] = useState(null)
  const [error, setError] = useState('')
  const [retry, setRetry] = useState(0)
  const action = useStaffAction()
  const eventKey = JSON.stringify([applied, beforeId])
  const invalidRange = Boolean(from && to && from > to)
  const page = events?.key === eventKey ? events.data : null
  const selected = modules?.find(item => item.id === selectedId)

  useEffect(() => {
    const controller = new AbortController()
    let timer
    const refresh = async () => {
      try {
        const [items, activity] = await Promise.all([
          api.admin.aiModules(controller.signal),
          api.admin.aiEvents(applied.filter, beforeId, controller.signal, applied),
        ])
        if (controller.signal.aborted) return
        setModules(items)
        setEvents({ key: eventKey, data: activity })
        setError('')
      } catch (err) {
        if (!controller.signal.aborted) setError(err.message || 'Không thể tải dữ liệu AI.')
      } finally {
        if (!controller.signal.aborted) timer = setTimeout(refresh, 5000)
      }
    }
    refresh()
    return () => { controller.abort(); clearTimeout(timer) }
  }, [applied, beforeId, eventKey, retry])

  const select = (id) => { setSelectedId(id); setFilter(id) }
  const toggle = async (item) => {
    if (readOnly) return
    const message = item.isEnabled ? 'Đã tắt xử lý tự động.'
      : 'Đã bật AI. Backend sẽ khởi động worker; trạng thái kết nối sẽ tự cập nhật.'
    if (await action.run(() => api.admin.aiEnabled(item.id, !item.isEnabled), message)) {
      setRetry(value => value + 1)
    }
  }

  return <>
    <main className="ad-main ai-management">
      <header className="ad-heading">
        <div><p className="ad-eyebrow">KBS / AUTOMATION</p><h1>{readOnly ? 'Theo dõi AI' : 'Quản lý AI'}</h1></div>
        <button type="button" onClick={() => setRetry(value => value + 1)}>Làm mới</button>
      </header>
      <p className="ad-muted">{readOnly
        ? 'Chế độ chỉ đọc: theo dõi trạng thái và thông báo AI. Chỉ Admin có quyền bật/tắt AI.'
        : 'Quản lý từng module và theo dõi kết quả xử lý trong cùng một nơi.'}</p>
      {error && <p role="alert">{error} Dữ liệu hiển thị có thể chưa được cập nhật.</p>}
      {action.notice && <p role={action.notice.error ? 'alert' : 'status'}>{action.notice.text}</p>}
      {!modules ? <p role="status">Đang tải danh sách AI…</p>
        : !modules.length ? <p>Chưa có module AI.</p>
          : <div className="ai-modules" aria-label="Danh sách AI">
            {modules.map(item => <article className={`ai-module${selectedId === item.id ? ' is-selected' : ''}`}
              key={item.id}>
              <button type="button" className="ai-select" aria-pressed={selectedId === item.id}
                onClick={() => select(item.id)}>
                <Bot size={24} aria-hidden="true" />
                <span><strong>{item.name}</strong><small>{item.sourceFile}</small></span>
                <ChevronRight size={18} aria-hidden="true" />
              </button>
              <p>{item.description}</p>
              {item.isEnabled && item.connectionStatus !== 'ONLINE' && <p role="alert">
                Đang chờ worker kết nối. Backend tự khởi động worker khi bật AI.
                Nếu chờ lâu, kiểm tra mục Thông báo hoạt động và log backend.
              </p>}
              <div className="ai-module-bottom">
                <span className="ai-connection"><Activity size={14} aria-hidden="true" />
                  {!item.isEnabled && item.isIntegrated ? 'Đã tắt'
                    : connections[item.connectionStatus] || item.connectionStatus}</span>
                {readOnly ? <span>{item.isEnabled ? 'Đang bật' : 'Đang tắt'}</span>
                  : <button type="button" role="switch" aria-checked={item.isEnabled}
                  aria-label={`Bật/tắt ${item.name}`} className="ai-switch"
                  disabled={!item.isIntegrated || action.busy || !!error}
                  title={!item.isIntegrated ? 'Module chưa được tích hợp' : undefined}
                  onClick={() => toggle(item)}>
                  <span aria-hidden="true" />{item.isEnabled ? 'Bật' : 'Tắt'}
                </button>}
              </div>
            </article>)}
          </div>}

      <section className="ai-events" aria-label="Thông báo AI">
        <header className="ad-heading"><h2><Bell size={19} aria-hidden="true" /> Thông báo hoạt động</h2>
          <span className="ad-muted">Tự cập nhật mỗi 5 giây</span></header>
        <form className="ai-filters ai-event-filters" onSubmit={e => {
          e.preventDefault()
          if (invalidRange) return
          setApplied({ filter, kind, from, to }); setBeforeId(null)
        }}>
          <label>Tên AI<select value={filter} onChange={e => setFilter(e.target.value)}>
            <option value="">Tất cả AI</option>
            {modules?.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}
          </select></label>
          <label>Trạng thái<select value={kind} onChange={e => setKind(e.target.value)}>
            <option value="">Tất cả trạng thái</option>
            {Object.entries(kinds).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
          </select></label>
          <label>Từ lúc<input type="datetime-local" value={from} max={to || undefined}
            onChange={e => setFrom(e.target.value)} /></label>
          <label>Đến lúc<input type="datetime-local" value={to} min={from || undefined}
            onChange={e => setTo(e.target.value)} /></label>
          <button type="submit" disabled={invalidRange}>Áp dụng lọc</button>
          <button type="button" onClick={() => {
            setFilter(''); setKind(''); setFrom(''); setTo('')
          }}>Xóa bộ lọc</button>
        </form>
        <p className="ad-muted">Chọn điều kiện rồi bấm Áp dụng lọc. Thông báo tự cập nhật theo bộ lọc đã áp dụng.</p>
        {invalidRange && <p role="alert">Thời gian bắt đầu phải trước hoặc bằng thời gian kết thúc.</p>}
        {!page ? <p role="status">Đang tải thông báo…</p>
          : !page.items.length ? <div className="ai-empty"><Bell size={28} aria-hidden="true" />
            <p>Không có thông báo phù hợp.</p><span className="ad-muted">Thử thay đổi hoặc xóa bộ lọc.</span></div>
            : <div className="ai-event-table"><table>
              <thead><tr><th>Thời gian</th><th>AI</th><th>Nội dung</th><th>Kết quả</th></tr></thead>
              <tbody>{page.items.map(event => <tr key={event.id}>
                <td><time dateTime={event.createdAt}>{dateTime(event.createdAt)}</time></td>
                <td>{event.moduleName}</td><td>{event.message}</td>
                <td>{kinds[event.kind] || event.kind}</td>
              </tr>)}</tbody>
            </table></div>}
        <div className="ai-filters">
          {beforeId && <button type="button" onClick={() => setBeforeId(null)}>Về thông báo mới nhất</button>}
          {page?.nextBeforeId && <button type="button" onClick={() => setBeforeId(page.nextBeforeId)}>
            Thông báo cũ hơn
          </button>}
        </div>
      </section>
    </main>
    <aside className="ad-details ad-panel ai-details" aria-label="Thông tin AI được chọn">
      <p className="ad-eyebrow">THÔNG TIN MODULE</p>
      {!selected ? <div className="ai-empty"><Bot size={40} aria-hidden="true" />
        <h2>Chọn một AI</h2><p className="ad-muted">Chọn module để xem thông tin và thông báo riêng.</p></div>
        : <><Bot size={40} aria-hidden="true" /><h2>{selected.name}</h2><p>{selected.description}</p>
          <dl>
            <dt>File module</dt><dd>{selected.sourceFile}</dd>
            <dt>Xử lý tự động</dt><dd>{selected.isEnabled ? 'Đang bật' : 'Đang tắt'}</dd>
            <dt>Kết nối</dt><dd>{connections[selected.connectionStatus] || selected.connectionStatus}</dd>
            <dt>Liên lạc gần nhất</dt><dd>{dateTime(selected.lastHeartbeatAt)}</dd>
            <dt>Cập nhật cấu hình</dt><dd>{dateTime(selected.updatedAt)}</dd>
          </dl>
          {selected.approvalLimit != null && <div className="ai-rule">
            <h3>Quy tắc xác nhận</h3><p>Đơn chờ dưới <strong>{money(selected.approvalLimit)}</strong>: tự xác nhận.</p>
            <p>Từ mức này trở lên: chờ staff xác nhận.</p>
          </div>}
          {!selected.isIntegrated && <p className="ad-muted">Module chưa được tích hợp.</p>}
          {selected.isIntegrated && <p className="ad-muted">
            Bật module cho phép worker xử lý đơn đang chờ. Khi worker mất kết nối, staff vẫn xử lý thủ công được.
          </p>}
        </>}
    </aside>
  </>
}
