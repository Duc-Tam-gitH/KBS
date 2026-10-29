import { useEffect, useRef, useState } from 'react'
import { Eye, Pencil, LockKeyhole, UnlockKeyhole, Trash2, Users, Search, Plus } from 'lucide-react'
import { api } from '../../services/api'
import { dateTime } from '../Staff/staffHooks'
import './AccountManagement.css'

const blank = { search: '', role: '', active: '', days: '', sort: 'newest' }

export function filterAccounts(users, filters, now = Date.now()) {
  const query = filters.search.trim().toLocaleLowerCase('vi')
  return users.filter(item => (!query || [item.fullName, item.username, item.email, item.phoneNumber]
    .some(value => value?.toLocaleLowerCase('vi').includes(query)))
    && (!filters.role || String(item.roleId) === filters.role)
    && (!filters.active || String(item.isActive) === filters.active)
    && (!filters.days || new Date(item.createdAt).getTime() >= (filters.days === 'today'
      ? new Date(now).setHours(0, 0, 0, 0) : now - Number(filters.days) * 86400000)))
    .sort((a, b) => (filters.sort === 'name'
      ? (a.fullName || a.username).localeCompare(b.fullName || b.username, 'vi')
      : (new Date(a.createdAt) - new Date(b.createdAt)) * (filters.sort === 'oldest' ? 1 : -1)) || a.id - b.id)
}

export function AccountTable({ items, selected = [], currentId, busy, onSelect, onOpen, onAction }) {
  if (!items.length) return <p className="um-empty">Không tìm thấy tài khoản phù hợp.</p>
  const all = items.every(item => selected.includes(item.id))
  return <div className="um-table-wrap"><table className="um-table">
    <thead><tr><th><input type="checkbox" aria-label="Chọn tất cả trên trang" checked={all}
      disabled={busy} onChange={() => onSelect(all ? [] : items.map(item => item.id))} /></th>
      <th>Người dùng</th><th>Liên hệ</th><th>Vai trò</th><th>Trạng thái</th><th>Ngày tạo</th><th>Hành động</th>
    </tr></thead>
    <tbody>{items.map(item => <tr key={item.id} className={selected.includes(item.id) ? 'um-selected' : ''}>
      <td><input type="checkbox" aria-label={`Chọn ${item.username}`} checked={selected.includes(item.id)}
        disabled={busy} onChange={() => onSelect(selected.includes(item.id)
          ? selected.filter(id => id !== item.id) : [...selected, item.id])} /></td>
      <td><div className="um-user"><span className="um-avatar" aria-hidden="true">
        {(item.fullName || item.username).slice(0, 1).toLocaleUpperCase('vi')}</span>
        <div><strong>{item.fullName || item.username}</strong><small>@{item.username} · #{item.id}</small></div>
      </div></td>
      <td>{item.email}<small>{item.phoneNumber || '—'}</small></td><td>{item.roleName}</td>
      <td><span className="um-badge">{item.isActive ? 'Hoạt động' : 'Bị khóa'}</span></td>
      <td>{dateTime(item.createdAt)}</td>
      <td><div className="um-actions">
        <button type="button" aria-label={`Xem ${item.username}`} disabled={busy} onClick={() => onOpen(item, 'view')}>
          <Eye size={17} /></button>
        <button type="button" aria-label={`Sửa ${item.username}`} disabled={busy} onClick={() => onOpen(item, 'edit')}>
          <Pencil size={17} /></button>
        <button type="button" aria-label={`${item.isActive ? 'Khóa' : 'Mở khóa'} ${item.username}`}
          disabled={busy || item.id === currentId} onClick={() => onAction(item.isActive ? 'lock' : 'unlock', [item])}>
          {item.isActive ? <LockKeyhole size={17} /> : <UnlockKeyhole size={17} />}</button>
        <button type="button" aria-label={`Xóa ${item.username}`} disabled={busy || item.id === currentId}
          onClick={() => onAction('delete', [item])}><Trash2 size={17} /></button>
      </div></td>
    </tr>)}</tbody>
  </table></div>
}

export function AccountForm({ account, roles, currentId, busy, onSave, onClose }) {
  const [form, setForm] = useState(() => ({ username: account?.username || '', email: account?.email || '',
    fullName: account?.fullName || '', phoneNumber: account?.phoneNumber || '',
    roleId: account?.roleId || roles.find(role => role.name === 'Customer')?.id || '',
    isActive: account?.isActive ?? true, password: '', confirmPassword: '' }))
  const change = event => setForm(value => ({ ...value, [event.target.name]: event.target.type === 'checkbox'
    ? event.target.checked : event.target.value }))
  return <form className="um-form" onSubmit={event => {
    event.preventDefault()
    const { password, confirmPassword, ...fields } = form
    onSave({ ...fields, phoneNumber: fields.phoneNumber.trim() || null, roleId: Number(fields.roleId),
      ...(!account ? { password, confirmPassword } : {}) })
  }}>
    <h2>{account ? `Sửa tài khoản #${account.id}` : 'Thêm người dùng'}</h2>
    <fieldset disabled={busy}>
      {[['username', 'Tên đăng nhập', 'text', 100], ['email', 'Email', 'email', 254],
        ['fullName', 'Họ tên', 'text', 200], ['phoneNumber', 'Số điện thoại', 'tel', 30]].map(([name, label, type, max]) =>
        <label key={name}>{label}<input name={name} type={type} value={form[name]} onChange={change}
          maxLength={max} required={name === 'username' || name === 'email'} /></label>)}
      {!account && <>
        <label>Mật khẩu<input name="password" type="password" autoComplete="new-password" required
          minLength={8} maxLength={128} value={form.password} onChange={change} /></label>
        <small>Ít nhất 8 ký tự, gồm chữ và số.</small>
        <label>Xác nhận mật khẩu<input name="confirmPassword" type="password" autoComplete="new-password" required
          minLength={8} maxLength={128} value={form.confirmPassword} onChange={change} /></label>
      </>}
      <label>Vai trò<select name="roleId" required value={form.roleId} onChange={change}
        disabled={!!account && account.id === currentId}><option value="">Chọn vai trò</option>
        {roles.map(role => <option key={role.id} value={role.id}>{role.name}</option>)}
      </select></label>
      <label className="um-check"><input name="isActive" type="checkbox" checked={form.isActive}
        disabled={!!account && account.id === currentId} onChange={change} />Đang hoạt động</label>
    </fieldset>
    <div className="um-actions"><button type="submit" className="um-primary" disabled={busy}>
      {busy ? 'Đang lưu…' : 'Lưu'}</button><button type="button" disabled={busy} onClick={onClose}>Đóng</button></div>
  </form>
}

export default function AccountManagement({ user }) {
  const [data, setData] = useState(null)
  const [error, setError] = useState('')
  const [retry, setRetry] = useState(0)
  const [filters, setFilters] = useState(blank)
  const [showFilters, setShowFilters] = useState(false)
  const [page, setPage] = useState(1)
  const [size, setSize] = useState(10)
  const [selected, setSelected] = useState([])
  const [panel, setPanel] = useState(null)
  const [confirmation, setConfirmation] = useState(null)
  const [notice, setNotice] = useState('')
  const [busy, setBusy] = useState(false)
  const locked = useRef(false)
  const mounted = useRef(true)
  useEffect(() => { mounted.current = true; return () => { mounted.current = false } }, [])
  useEffect(() => {
    const controller = new AbortController()
    Promise.all([api.admin.users(controller.signal), api.admin.userRoles(controller.signal)])
      .then(([users, roles]) => { if (!controller.signal.aborted) { setData({ users, roles }); setError('') } })
      .catch(err => { if (!controller.signal.aborted) setError(err.message) })
    return () => controller.abort()
  }, [retry])
  const reload = () => { setSelected([]); setRetry(value => value + 1) }
  const changeFilter = (name, value) => { setFilters(old => ({ ...old, [name]: value })); setPage(1); setSelected([]) }
  const filtered = filterAccounts(data?.users || [], filters)
  const pages = Math.max(1, Math.ceil(filtered.length / size))
  const currentPage = Math.min(page, pages)
  const items = filtered.slice((currentPage - 1) * size, currentPage * size)
  const run = async operation => {
    if (locked.current) return
    locked.current = true; setBusy(true); setNotice('')
    try { await operation() } catch (err) { if (mounted.current) setNotice(err.message || 'Thao tác thất bại.') }
    finally { locked.current = false; if (mounted.current) setBusy(false) }
  }
  const save = payload => run(async () => {
    const updated = panel.account ? await api.admin.saveUser(panel.account.id, payload) : await api.admin.createUser(payload)
    if (!mounted.current) return
    setNotice('Đã lưu tài khoản.'); setPanel({ account: updated, mode: 'view' }); reload()
  })
  const act = () => run(async () => {
    const { kind, accounts } = confirmation
    const failures = []; let successes = 0
    for (const account of accounts) {
      try {
        if (kind === 'delete') await api.admin.deleteUser(account.id)
        else await api.admin.userStatus(account.id, kind === 'unlock')
        successes++
      } catch (err) { failures.push(`${account.username}: ${err.message}`) }
    }
    if (!mounted.current) return
    setConfirmation(null); setPanel(null); reload()
    setNotice(`Đã xử lý ${successes}/${accounts.length} tài khoản.${failures.length ? ' ' + failures.join(' · ') : ''}`)
  })
  const requestAction = (kind, accounts) => setConfirmation({ kind, accounts })
  const chips = Object.entries(filters).filter(([key, value]) => value && key !== 'sort')
  return <>
    <main className="ad-main um-layout">
      <header className="ad-heading"><div><h1><Users size={24} /> Quản lý người dùng</h1>
        <p>Tổng cộng: <strong>{data?.users.length ?? '—'}</strong> tài khoản</p></div>
        <button type="button" className="um-primary" disabled={busy || !data || !!error}
          onClick={() => { setPanel({ mode: 'create' }); setNotice('') }}><Plus size={17} /> Thêm người dùng</button>
      </header>
      <div className="um-toolbar"><label className="um-search"><Search size={18} />
        <input aria-label="Tìm tài khoản" placeholder="Tìm theo tên, email, số điện thoại…"
          value={filters.search} onChange={e => changeFilter('search', e.target.value)} /></label>
        <button type="button" aria-expanded={showFilters} onClick={() => setShowFilters(value => !value)}>Bộ lọc</button>
        <button type="button" disabled={busy} onClick={reload}>Tải lại</button></div>
      {showFilters && <div className="um-filter-grid">
        <label>Vai trò<select value={filters.role} onChange={e => changeFilter('role', e.target.value)}>
          <option value="">Tất cả</option>{data?.roles.map(role => <option key={role.id} value={role.id}>{role.name}</option>)}
        </select></label>
        <label>Trạng thái<select value={filters.active} onChange={e => changeFilter('active', e.target.value)}>
          <option value="">Tất cả</option><option value="true">Hoạt động</option><option value="false">Bị khóa</option>
        </select></label>
        <label>Ngày tạo<select value={filters.days} onChange={e => changeFilter('days', e.target.value)}>
          <option value="">Tất cả</option><option value="today">Hôm nay</option>
          <option value="7">7 ngày qua</option><option value="30">30 ngày qua</option>
        </select></label>
        <label>Sắp xếp<select value={filters.sort} onChange={e => changeFilter('sort', e.target.value)}>
          <option value="newest">Mới nhất</option><option value="oldest">Cũ nhất</option><option value="name">Tên A → Z</option>
        </select></label>
      </div>}
      {!!chips.length && <div className="um-actions">{chips.map(([key, value]) => <button key={key} type="button"
        onClick={() => changeFilter(key, '')}>{key === 'role' ? data?.roles.find(role => String(role.id) === value)?.name
          : key === 'active' ? (value === 'true' ? 'Hoạt động' : 'Bị khóa')
            : key === 'days' ? (value === 'today' ? 'Hôm nay' : `${value} ngày qua`) : value} ×</button>)}
        <button type="button" onClick={() => { setFilters(blank); setPage(1); setSelected([]) }}>Xóa bộ lọc</button></div>}
      {notice && <p role="status" className="um-notice">{notice}</p>}
      {confirmation && <section className="um-notice" aria-label="Xác nhận thao tác">
        <p>{confirmation.kind === 'delete' ? 'Xóa vĩnh viễn' : confirmation.kind === 'lock' ? 'Khóa' : 'Mở khóa'}
          {' '}{confirmation.accounts.length} tài khoản: {confirmation.accounts.map(item => item.username).join(', ')}?</p>
        {confirmation.kind === 'delete' && <p>Chỉ xóa tài khoản không có dữ liệu liên quan. Không thể hoàn tác.</p>}
        <div className="um-actions"><button type="button" disabled={busy} onClick={act}>Xác nhận</button>
          <button type="button" disabled={busy} onClick={() => setConfirmation(null)}>Hủy</button></div>
      </section>}
      {!!selected.length && <div className="um-toolbar"><span>Đã chọn {selected.length} tài khoản</span>
        {[['unlock', 'Mở khóa'], ['lock', 'Khóa'], ['delete', 'Xóa']].map(([kind, label]) => <button key={kind}
          type="button" disabled={busy || !!error} onClick={() => requestAction(kind,
            data.users.filter(item => selected.includes(item.id)))}>{label}</button>)}</div>}
      {error ? <p role="alert">{error} <button type="button" onClick={reload}>Thử lại</button></p>
        : !data ? <p role="status">Đang tải tài khoản…</p>
          : !data.users.length ? <p className="um-empty">Chưa có tài khoản.</p>
            : <AccountTable items={items} selected={selected} currentId={user?.id} busy={busy}
              onSelect={setSelected} onOpen={(account, mode) => setPanel({ account, mode })} onAction={requestAction} />}
      <div className="um-pagination"><span>{filtered.length ? (currentPage - 1) * size + 1 : 0}–
        {Math.min(currentPage * size, filtered.length)} / {filtered.length} kết quả</span>
        <button type="button" disabled={currentPage === 1 || busy} onClick={() => { setPage(currentPage - 1); setSelected([]) }}>Trước</button>
        <span>Trang {currentPage}/{pages}</span>
        <button type="button" disabled={currentPage === pages || busy} onClick={() => { setPage(currentPage + 1); setSelected([]) }}>Sau</button>
        <select aria-label="Số tài khoản mỗi trang" value={size} onChange={e => { setSize(Number(e.target.value)); setPage(1); setSelected([]) }}>
          {[5, 10, 20, 50].map(value => <option key={value} value={value}>{value} / trang</option>)}
        </select></div>
    </main>
    <aside className="ad-details ad-panel um-details" aria-label="Chi tiết tài khoản">
      {!panel ? <p>Chọn tài khoản để xem chi tiết hoặc thêm người dùng.</p>
        : panel.mode === 'view' ? <><h2>{panel.account.fullName || panel.account.username}</h2><dl>
          {[['ID', panel.account.id], ['Tên đăng nhập', panel.account.username], ['Email', panel.account.email],
            ['Điện thoại', panel.account.phoneNumber], ['Vai trò', panel.account.roleName],
            ['Trạng thái', panel.account.isActive ? 'Hoạt động' : 'Bị khóa'], ['Ngày tạo', dateTime(panel.account.createdAt)]]
            .map(([label, value]) => <div key={label}><dt>{label}</dt><dd>{value || '—'}</dd></div>)}
        </dl><button type="button" disabled={busy} onClick={() => setPanel(null)}>Đóng</button></>
          : <AccountForm key={`${panel.mode}:${panel.account?.id || 'new'}`} account={panel.account}
            roles={data?.roles || []} currentId={user?.id} busy={busy || !!error} onSave={save} onClose={() => setPanel(null)} />}
    </aside>
  </>
}
