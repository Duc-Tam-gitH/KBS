import { useEffect, useRef, useState } from 'react'
import { api } from '../../services/api'
import './CategoryManagement.css'

const dateLabel = value => value ? new Date(value).toLocaleDateString('vi-VN') : '--'
const statusLabel = active => active ? 'Hoạt động' : 'Đang ẩn'

export default function CategoryManagement() {
  const [categories, setCategories] = useState(null)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [retry, setRetry] = useState(0)
  const [query, setQuery] = useState('')
  const [status, setStatus] = useState('')
  const [page, setPage] = useState(1)
  const [editor, setEditor] = useState(null)
  const [busy, setBusy] = useState(false)
  const lock = useRef(false)
  const mounted = useRef(false)

  useEffect(() => {
    mounted.current = true
    return () => { mounted.current = false }
  }, [])
  useEffect(() => {
    const controller = new AbortController()
    api.admin.categories(controller.signal)
      .then(data => { if (!controller.signal.aborted) setCategories(data) })
      .catch(err => { if (!controller.signal.aborted) setError(err.message) })
    return () => controller.abort()
  }, [retry])

  const reload = () => {
    setCategories(null); setError(''); setNotice(''); setEditor(null); setRetry(value => value + 1)
  }
  const openEditor = category => {
    if (lock.current) return
    setNotice('')
    setEditor({ category })
  }
  const save = async payload => {
    if (lock.current) return
    lock.current = true
    setBusy(true)
    setNotice('')
    try {
      const saved = editor.category
        ? await api.admin.updateCategory(editor.category.id, payload)
        : await api.admin.createCategory(payload)
      if (!mounted.current) return
      setCategories(items => [...items.filter(item => item.id !== saved.id), saved].sort((a, b) => a.id - b.id))
      setEditor(null)
      setNotice('Đã lưu danh mục.')
    } finally {
      lock.current = false
      if (mounted.current) setBusy(false)
    }
  }
  const remove = async category => {
    if (lock.current || !window.confirm('Xóa danh mục "' + category.name + '"? Thao tác này không thể hoàn tác.')) return
    lock.current = true
    setBusy(true)
    setNotice('')
    try {
      await api.admin.deleteCategory(category.id)
      if (!mounted.current) return
      setCategories(items => items.filter(item => item.id !== category.id))
      if (editor?.category?.id === category.id) setEditor(null)
      setNotice('Đã xóa danh mục.')
    } catch (err) {
      if (mounted.current) setNotice('Không thể xóa: ' + err.message)
    } finally {
      lock.current = false
      if (mounted.current) setBusy(false)
    }
  }
  const filtered = (categories || []).filter(item =>
    item.name.toLocaleLowerCase('vi').includes(query.trim().toLocaleLowerCase('vi'))
    && (status === '' || item.isActive === (status === 'active')))
  const pageCount = Math.max(1, Math.ceil(filtered.length / 10))
  const currentPage = Math.min(page, pageCount)
  const offset = (currentPage - 1) * 10
  const visible = filtered.slice(offset, offset + 10)

  const exportCsv = () => {
    const cell = value => {
      const text = String(value ?? '')
      const safe = /^[\s]*[=+@-]/.test(text) ? "'" + text : text
      return '"' + safe.replaceAll('"', '""') + '"'
    }
    const rows = [['ID', 'Tên danh mục', 'Mô tả', 'Số sản phẩm', 'Trạng thái', 'Ngày tạo'],
      ...filtered.map(item => [item.id, item.name, item.description, item.productCount,
        statusLabel(item.isActive), dateLabel(item.createdAt)])]
    const blob = new Blob(['\uFEFF' + rows.map(row => row.map(cell).join(',')).join('\r\n')],
      { type: 'text/csv;charset=utf-8;' })
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = 'danh-muc.csv'
    link.click()
    setTimeout(() => URL.revokeObjectURL(url), 1000)
  }

  return <>
    <div className="ad-main cm-page">
      <header className="ad-heading">
        <div><p className="ad-eyebrow">DANH MỤC SẢN PHẨM</p><h1>Quản lý danh mục</h1></div>
        <button type="button" onClick={reload} disabled={busy || (!categories && !error)}>Làm mới</button>
      </header>
      {notice && <p className="ad-panel" role="status">{notice}</p>}
      {error ? <div className="ad-panel" role="alert"><p>{error}</p>
        <button type="button" onClick={reload}>Thử lại</button></div>
        : !categories ? <p className="ad-panel" role="status">Đang tải danh mục…</p> : <>
          <section className="cm-stats" aria-label="Thống kê danh mục">
            {[['Tổng danh mục', categories.length],
              ['Hoạt động', categories.filter(item => item.isActive).length],
              ['Đang ẩn', categories.filter(item => !item.isActive).length]].map(([label, value]) =>
              <article className="ad-panel" key={label}><span>{label}</span><strong>{value}</strong></article>)}
          </section>
          <section className="ad-panel">
            <div className="cm-toolbar">
              <label>Tìm danh mục<input type="search" value={query} placeholder="Nhập tên danh mục"
                onChange={event => { setQuery(event.target.value); setPage(1) }} /></label>
              <label>Trạng thái<select value={status}
                onChange={event => { setStatus(event.target.value); setPage(1) }}>
                <option value="">Tất cả</option><option value="active">Hoạt động</option>
                <option value="hidden">Đang ẩn</option>
              </select></label>
              <div className="cm-actions">
                <button type="button" onClick={exportCsv} disabled={!filtered.length || busy}>Xuất CSV (Excel)</button>
                <button type="button" className="ad-primary" onClick={() => openEditor(null)}
                  disabled={busy}>+ Thêm danh mục</button>
              </div>
            </div>
            {!filtered.length ? <p className="ad-empty">
              {categories.length ? 'Không có danh mục phù hợp.' : 'Chưa có danh mục.'}</p>
              : <div className="ad-table-wrap"><table>
                <thead><tr><th>ID</th><th>Tên danh mục</th><th>Số SP</th><th>Trạng thái</th>
                  <th>Ngày tạo</th><th>Thao tác</th></tr></thead>
                <tbody>{visible.map(item => <tr key={item.id}>
                  <td>#{item.id}</td><td><button type="button" disabled={busy}
                    aria-pressed={editor?.category?.id === item.id}
                    onClick={() => openEditor(item)}>{item.name}</button></td>
                  <td>{item.productCount}</td><td><span className="cm-status">{statusLabel(item.isActive)}</span></td>
                  <td>{dateLabel(item.createdAt)}</td>
                  <td><div className="cm-actions">
                    <button type="button" onClick={() => openEditor(item)} disabled={busy}>Sửa</button>
                    <button type="button" onClick={() => remove(item)} disabled={busy || item.productCount > 0}
                      title={item.productCount > 0 ? 'Danh mục đang có sản phẩm nên không thể xóa' : undefined}>Xóa</button>
                  </div></td>
                </tr>)}</tbody>
              </table></div>}
            <div className="cm-pagination">
              <p>{filtered.length ? offset + 1 : 0}–{Math.min(offset + 10, filtered.length)} / {filtered.length} danh mục</p>
              <div className="cm-actions">
                <button type="button" disabled={currentPage === 1}
                  onClick={() => setPage(currentPage - 1)}>Trước</button>
                <span>Trang {currentPage} / {pageCount}</span>
                <button type="button" disabled={currentPage === pageCount}
                  onClick={() => setPage(currentPage + 1)}>Sau</button>
              </div>
            </div>
          </section>
        </>}
    </div>
    <aside className="ad-details ad-panel cm-details" aria-label="Chi tiết danh mục">
      <p className="ad-eyebrow">CHI TIẾT DANH MỤC</p>
      {editor ? <CategoryForm key={editor.category?.id ?? 'new'} category={editor.category}
        busy={busy} onSave={save} onCancel={() => setEditor(null)} />
        : <p className="ad-empty">Chọn danh mục để xem, chỉnh sửa hoặc thêm danh mục mới.</p>}
    </aside>
  </>
}

export function CategoryForm({ category, busy, onSave, onCancel }) {
  const [name, setName] = useState(category?.name || '')
  const [description, setDescription] = useState(category?.description || '')
  const [isActive, setIsActive] = useState(category?.isActive ?? true)
  const [error, setError] = useState('')
  const submit = async event => {
    event.preventDefault()
    if (busy) return
    if (!name.trim()) { setError('Tên danh mục không được để trống.'); return }
    setError('')
    try {
      await onSave({ name: name.trim(), description: description.trim() || null, isActive })
    } catch (err) { setError(err.message || 'Không thể lưu danh mục.') }
  }
  return <form className="cm-form" onSubmit={submit}>
    <h2>{category ? 'Chỉnh sửa danh mục' : 'Thêm danh mục'}</h2>
    {category && <dl><dt>ID</dt><dd>#{category.id}</dd>
      <dt>Số sản phẩm</dt><dd>{category.productCount}</dd>
      <dt>Ngày tạo</dt><dd>{dateLabel(category.createdAt)}</dd></dl>}
    <label>Tên danh mục<input name="name" required value={name} disabled={busy}
      onChange={event => setName(event.target.value)} /></label>
    <label>Mô tả<textarea name="description" rows={4} value={description} disabled={busy}
      onChange={event => setDescription(event.target.value)} /></label>
    <label className="cm-checkbox"><input name="isActive" type="checkbox" checked={isActive} disabled={busy}
      onChange={event => setIsActive(event.target.checked)} />Hoạt động</label>
    {error && <p role="alert">{error}</p>}
    <div className="cm-actions"><button className="ad-primary" type="submit" disabled={busy}>
      {busy ? 'Đang xử lý…' : 'Lưu danh mục'}</button>
      <button type="button" onClick={onCancel} disabled={busy}>Đóng</button></div>
  </form>
}
