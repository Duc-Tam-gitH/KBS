import { useEffect, useRef, useState } from 'react'
import { ChevronLeft, ChevronRight, Package, Pencil, Plus, Power, RefreshCw, Search, X } from 'lucide-react'
import { api } from '../../services/api'
import ProductForm from './ProductForm'
import { filterProducts } from './filterProducts'
import './ProductManagement.css'

const money = value => new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(value)
const statusLabel = active => active ? 'Đang bán' : 'Ngừng bán'

export default function ProductManagement() {
  const [data, setData] = useState(null)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [revision, setRevision] = useState(0)
  const [query, setQuery] = useState('')
  const [category, setCategory] = useState('')
  const [status, setStatus] = useState('')
  const [page, setPage] = useState(1)
  const [editor, setEditor] = useState(null)
  const [changing, setChanging] = useState(null)
  const [busy, setBusy] = useState(false)
  const [actionError, setActionError] = useState('')
  const lock = useRef(false)
  const mounted = useRef(false)

  useEffect(() => {
    mounted.current = true
    return () => { mounted.current = false }
  }, [])
  useEffect(() => {
    const controller = new AbortController()
    Promise.all([api.admin.products(controller.signal), api.admin.categories(controller.signal), api.brands()])
      .then(([products, categories, brands]) => {
        if (!controller.signal.aborted) setData({ products, categories, brands })
      })
      .catch(err => { if (!controller.signal.aborted) setError(err.message) })
    return () => controller.abort()
  }, [revision])

  const reload = () => { setData(null); setError(''); setNotice(''); setRevision(value => value + 1) }
  const clearFilters = () => { setQuery(''); setCategory(''); setStatus(''); setPage(1) }
  const openEditor = product => { setNotice(''); setEditor({ product }) }
  const save = async (product, payload) => {
    if (lock.current) return
    lock.current = true
    setBusy(true)
    try {
      const saved = product
        ? await api.admin.updateProduct(product.id, payload)
        : await api.admin.createProduct(payload)
      if (!mounted.current) return
      setData(current => ({ ...current, products: product
        ? current.products.map(item => item.id === saved.id ? saved : item)
        : [saved, ...current.products] }))
      setEditor(null)
      setChanging(null)
      setNotice('Đã lưu sản phẩm.')
    } finally {
      lock.current = false
      if (mounted.current) setBusy(false)
    }
  }
  const changeStatus = async () => {
    setActionError('')
    try { await save(changing, { ...changing, isActive: !changing.isActive }) }
    catch (err) { if (mounted.current) setActionError(err.message) }
  }
  const products = data?.products || []
  const filtered = filterProducts(products, query, category, status)
  const pageCount = Math.max(1, Math.ceil(filtered.length / 10))
  const currentPage = Math.min(page, pageCount)
  const offset = (currentPage - 1) * 10
  const hasFilters = Boolean(query || category || status)

  return <main className="pm-page">
    <header className="pm-heading">
      <div><p className="ad-eyebrow">DANH MỤC / SẢN PHẨM</p><h1>Sản phẩm</h1>
        {data && <p className="pm-summary">{products.length} sản phẩm
          <span aria-hidden="true"> / </span>
          <span>{products.filter(item => item.stockQuantity === 0).length} hết hàng</span></p>}
      </div>
      <button type="button" className="pm-icon" onClick={reload} disabled={!data && !error}
        aria-label="Làm mới sản phẩm" title="Làm mới"><RefreshCw size={17} /></button>
    </header>
    {notice && <p className="pm-notice" role="status">{notice}</p>}
    <div className="pm-toolbar">
      <label className="pm-search"><Search size={18} aria-hidden="true" />
        <input type="search" aria-label="Tìm sản phẩm" placeholder="Tìm theo tên hoặc ID sản phẩm"
          value={query} onChange={event => { setQuery(event.target.value); setPage(1) }} /></label>
      <select aria-label="Lọc theo danh mục" value={category}
        onChange={event => { setCategory(event.target.value); setPage(1) }}>
        <option value="">Tất cả danh mục</option>
        {data?.categories.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}
      </select>
      <div className="pm-segments" role="group" aria-label="Lọc theo trạng thái">
        {[['', 'Tất cả'], ['active', 'Đang bán'], ['inactive', 'Ngừng bán']].map(([value, label]) =>
          <button key={value} type="button" aria-pressed={status === value}
            onClick={() => { setStatus(value); setPage(1) }}>{label}</button>)}
      </div>
      {hasFilters && <button type="button" className="pm-icon" onClick={clearFilters}
        aria-label="Xóa bộ lọc" title="Xóa bộ lọc"><X size={16} /></button>}
      <button type="button" className="ad-primary pm-add" onClick={() => openEditor(null)} disabled={!data}>
        <Plus size={17} aria-hidden="true" />Thêm sản phẩm</button>
    </div>
    <section className="pm-table-panel" aria-label="Danh sách sản phẩm" aria-busy={!data && !error}>
      {error ? <div className="pm-empty" role="alert"><Package size={32} aria-hidden="true" />
        <h2>Không thể tải sản phẩm</h2><p>{error}</p><button onClick={reload}>Thử lại</button></div>
        : !data ? <p className="pm-empty" role="status">Đang tải sản phẩm…</p>
          : <>
            <ProductTable products={filtered.slice(offset, offset + 10)} hasFilters={hasFilters}
              onEdit={openEditor} onCreate={() => openEditor(null)} onClear={clearFilters}
              onStatus={product => { setActionError(''); setChanging(product) }} />
            {filtered.length > 0 && <footer className="pm-pagination">
              <p>Hiển thị {offset + 1}–{Math.min(offset + 10, filtered.length)} / {filtered.length} sản phẩm</p>
              <div><button type="button" className="pm-icon" aria-label="Trang trước" disabled={currentPage === 1}
                onClick={() => setPage(currentPage - 1)}><ChevronLeft size={17} /></button>
              <span>{currentPage} / {pageCount}</span>
              <button type="button" className="pm-icon" aria-label="Trang sau" disabled={currentPage === pageCount}
                onClick={() => setPage(currentPage + 1)}><ChevronRight size={17} /></button></div>
            </footer>}
          </>}
    </section>
    {editor && <ProductDialog className="pm-drawer" titleId="pm-form-title" busy={busy}
      onClose={() => setEditor(null)}>
      <ProductForm product={editor.product} categories={data.categories} brands={data.brands} busy={busy}
        onSave={payload => save(editor.product, payload)} onClose={() => setEditor(null)} />
    </ProductDialog>}
    {changing && <ProductDialog className="pm-confirm" titleId="pm-confirm-title" busy={busy}
      onClose={() => setChanging(null)}>
      <Power className="pm-confirm-icon" size={28} aria-hidden="true" />
      <h2 id="pm-confirm-title">{changing.isActive ? 'Ngừng bán sản phẩm?' : 'Mở bán sản phẩm?'}</h2>
      <p><strong>{changing.name}</strong> sẽ {changing.isActive ? 'được ẩn khỏi' : 'hiển thị trên'} cửa hàng.</p>
      {actionError && <p role="alert">{actionError}</p>}
      <div className="pm-form-actions"><button type="button" disabled={busy}
        onClick={() => setChanging(null)}>Hủy</button>
      <button type="button" className="ad-primary" disabled={busy} onClick={changeStatus}>
        {busy ? 'Đang lưu…' : 'Xác nhận'}</button></div>
    </ProductDialog>}
  </main>
}

export function ProductTable({ products, hasFilters, onEdit, onStatus, onCreate, onClear }) {
  if (!products.length) return <div className="pm-empty">
    <span className="pm-empty-icon"><Package size={26} aria-hidden="true" /></span>
    <h2>{hasFilters ? 'Không tìm thấy sản phẩm' : 'Chưa có sản phẩm nào'}</h2>
    <p>{hasFilters ? 'Thử từ khóa khác hoặc bỏ bớt bộ lọc.' : 'Thêm sản phẩm đầu tiên để bắt đầu quản lý.'}</p>
    <button type="button" className={hasFilters ? '' : 'ad-primary'} onClick={hasFilters ? onClear : onCreate}>
      {hasFilters ? 'Xóa bộ lọc' : 'Thêm sản phẩm'}</button>
  </div>
  return <div className="pm-table-scroll"><table>
    <thead><tr>{['Sản phẩm', 'Danh mục', 'Giá bán', 'Tồn kho', 'Trạng thái', 'Ngày tạo', 'Thao tác'].map(label =>
      <th scope="col" key={label}>{label}</th>)}</tr></thead>
    <tbody>{products.map(item => <tr key={item.id}>
      <td><button type="button" className="pm-product-name" onClick={() => onEdit(item)}>{item.name}</button>
        <span className="pm-product-id">#{item.id} · {item.brandName || '--'}</span></td>
      <td>{item.categoryName || '--'}</td><td className="pm-number">{money(item.price)}</td>
      <td className={`pm-number ${item.stockQuantity === 0 ? 'pm-out-of-stock' : ''}`}>
        {item.stockQuantity === 0 ? 'Hết hàng' : item.stockQuantity.toLocaleString('vi-VN')}</td>
      <td><span className={`pm-status ${item.isActive ? 'is-active' : ''}`}>
        <i aria-hidden="true" />{statusLabel(item.isActive)}</span></td>
      <td className="pm-date">{item.createdAt ? new Date(item.createdAt).toLocaleDateString('vi-VN') : '--'}</td>
      <td><div className="pm-row-actions">
        <button type="button" className="pm-icon" title="Sửa sản phẩm" aria-label={`Sửa ${item.name}`}
          onClick={() => onEdit(item)}><Pencil size={16} /></button>
        <button type="button" className="pm-icon" title={item.isActive ? 'Ngừng bán' : 'Mở bán'}
          aria-label={`${item.isActive ? 'Ngừng bán' : 'Mở bán'} ${item.name}`}
          onClick={() => onStatus(item)}><Power size={16} /></button>
      </div></td>
    </tr>)}</tbody>
  </table></div>
}

function ProductDialog({ children, className, titleId, busy, onClose }) {
  const dialog = useRef(null)
  useEffect(() => {
    const element = dialog.current
    element.showModal()
    return () => element.close()
  }, [])
  return <dialog ref={dialog} className={`pm-dialog ${className}`} aria-labelledby={titleId}
    onCancel={event => { event.preventDefault(); if (!busy) onClose() }}>
    {children}
  </dialog>
}
