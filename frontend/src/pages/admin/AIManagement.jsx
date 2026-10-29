import { useEffect, useState } from 'react'
import { Bot, Activity, Bell, ChevronRight } from 'lucide-react'
import { api } from '../../services/api'
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
  const [selectedId, setSelectedId] = useState(null)
  const [filter, setFilter] = useState('')
  const [beforeId, setBeforeId] = useState(null)
  const [events, setEvents] = useState(null)
  const [error, setError] = useState('')
  const [retry, setRetry] = useState(0)
  const action = useStaffAction()
  const eventKey = `${filter}:${beforeId}`
  const page = events?.key === eventKey ? events.data : null
  const selected = modules?.find(item => item.id === selectedId)

  useEffect(() => {
    const controller = new AbortController()
    let timer
    const refresh = async () => {
      try {
        const [items, activity] = await Promise.all([
          api.admin.aiModules(controller.signal), api.admin.aiEvents(filter, beforeId, controller.signal),
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
  }, [filter, beforeId, eventKey, retry])

  const select = (id) => { setSelectedId(id); setFilter(id); setBeforeId(null) }
  const toggle = async (item) => {
    if (readOnly) return
    const message = item.isEnabled ? 'Đã tắt xử lý tự động.'
      : 'Đã bật cấu hình AI. Đơn hàng chỉ được xác nhận sau khi worker xử lý thành công.'
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
                Worker chưa kết nối hoặc đã dừng. Đơn hàng vẫn chờ xác nhận; cần khởi động worker AI.
              </p>}
              <div className="ai-module-bottom">
                <span className="ai-connection"><Activity size={14} aria-hidden="true" />
                  {connections[item.connectionStatus] || item.connectionStatus}</span>
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
        <div className="ai-filters">
          <button type="button" aria-pressed={!filter} onClick={() => { setFilter(''); setBeforeId(null) }}>
            ALL AI
          </button>
          {selected && <button type="button" aria-pressed={filter === selected.id}
            onClick={() => { setFilter(selected.id); setBeforeId(null) }}>{selected.name}</button>}
        </div>
        {!page ? <p role="status">Đang tải thông báo…</p>
          : !page.items.length ? <div className="ai-empty"><Bell size={28} aria-hidden="true" />
            <p>Chưa có thông báo.</p><span className="ad-muted">Kết quả hoạt động sẽ xuất hiện tại đây.</span></div>
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
