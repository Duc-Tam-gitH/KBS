import { useEffect, useState } from 'react'
import { Search as SearchIcon, SlidersHorizontal, Package, ShoppingBag, ArrowLeft, ArrowRight } from 'lucide-react'
import { api } from '../../services/api'
import './Search.css'

const emptyFilters = { search: '', categoryId: '', brandId: '', minPrice: '', maxPrice: '' }
const money = (value) => new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(value)

export default function SearchPage({ onSelect, onAdd, busy = false }) {
  const [draft, setDraft] = useState(emptyFilters)
  const [filters, setFilters] = useState(emptyFilters)
  const [page, setPage] = useState(1)
  const [result, setResult] = useState({ items: [], totalItems: 0, totalPages: 0 })
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [validation, setValidation] = useState('')
  const [categories, setCategories] = useState([])
  const [brands, setBrands] = useState([])
  const [filterError, setFilterError] = useState('')
  const [retry, setRetry] = useState(0)
  const [filtersOpen, setFiltersOpen] = useState(false)

  useEffect(() => {
    let active = true
    setFilterError('')
    Promise.all([api.categories(), api.brands()])
      .then(([cats, brandList]) => {
        if (!active) return
        setCategories(cats.filter((x) => x.isActive !== false))
        setBrands(brandList.filter((x) => x.isActive !== false))
      })
      .catch(() => { if (active) setFilterError('Chưa tải được danh mục và thương hiệu.') })
    return () => { active = false }
  }, [retry])

  useEffect(() => {
    let active = true
    setLoading(true)
    setError('')
    const params = Object.fromEntries(Object.entries(filters).filter(([, value]) => value !== ''))
    api.products({ ...params, page, pageSize: 12 })
      .then((response) => { if (active) setResult(response) })
      .catch((err) => { if (active) setError(err.message || 'Không thể tải sản phẩm.') })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [filters, page, retry])

  const change = (event) => setDraft((prev) => ({ ...prev, [event.target.name]: event.target.value }))
  const apply = (event) => {
    event.preventDefault()
    if (draft.minPrice !== '' && draft.maxPrice !== '' && Number(draft.minPrice) > Number(draft.maxPrice)) {
      setValidation('Giá tối đa phải lớn hơn hoặc bằng giá tối thiểu.')
      return
    }
    setValidation('')
    setLoading(true)
    setPage(1)
    setFilters({ ...draft, search: draft.search.trim() })
    setFiltersOpen(false)
  }
  const reset = () => {
    setDraft(emptyFilters)
    setFilters({ ...emptyFilters })
    setPage(1)
    setLoading(true)
    setValidation('')
  }
  const selectProduct = (p) => onSelect?.({ ...p, brand: p.brandName, category: p.categoryName,
    connection: p.connectionType })

  return (
    <section className="search-page">
      <header className="search-heading"><p>KBS / KHÁM PHÁ</p><h1>Tìm lựa chọn của bạn</h1>
        <p>Từ cảm giác gõ đến phong cách riêng — tìm sản phẩm phù hợp với bạn.</p></header>
      <form className="search-layout" onSubmit={apply}>
        <aside className="search-sidebar" aria-label="Bộ lọc sản phẩm">
          <button type="button" className="search-filter-toggle" aria-expanded={filtersOpen}
            aria-controls="search-filter-fields" onClick={() => setFiltersOpen((value) => !value)}>
            <SlidersHorizontal size={18} /> Bộ lọc sản phẩm
          </button>
          <div id="search-filter-fields" className={`search-filter-fields${filtersOpen ? ' is-open' : ''}`}>
            <h2><SlidersHorizontal size={18} /> Bộ lọc</h2>
            <label htmlFor="search-category">Danh mục</label>
            <select id="search-category" name="categoryId" value={draft.categoryId} onChange={change}>
              <option value="">Tất cả danh mục</option>
              {categories.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
            </select>
            <label htmlFor="search-brand">Thương hiệu</label>
            <select id="search-brand" name="brandId" value={draft.brandId} onChange={change}>
              <option value="">Tất cả thương hiệu</option>
              {brands.map((b) => <option key={b.id} value={b.id}>{b.name}</option>)}
            </select>
            {filterError && <p role="alert">{filterError} <button type="button"
              onClick={() => setRetry((n) => n + 1)}>Thử lại</button></p>}
            <fieldset><legend>Khoảng giá (₫)</legend>
              <label htmlFor="search-min">Từ</label>
              <input id="search-min" type="number" min="0" step="any" name="minPrice"
                placeholder="Không giới hạn" value={draft.minPrice} onChange={change} />
              <label htmlFor="search-max">Đến</label>
              <input id="search-max" type="number" min="0" step="any" name="maxPrice"
                placeholder="Không giới hạn" value={draft.maxPrice} onChange={change} />
            </fieldset>
            <button className="search-primary" type="submit">Áp dụng bộ lọc</button>
            <button className="search-reset" type="button" onClick={reset}>Xóa tất cả bộ lọc</button>
          </div>
        </aside>
        <div className="search-main">
          <div className="search-input-bar" role="search">
            <SearchIcon size={22} aria-hidden="true" />
            <input name="search" aria-label="Tìm theo tên sản phẩm" placeholder="Bạn đang tìm sản phẩm nào?"
              value={draft.search} onChange={change} />
            <button className="search-primary" type="submit">Tìm kiếm</button>
          </div>
          {validation && <p className="search-message" role="alert">{validation}</p>}
          <div className="search-results" aria-busy={loading}>
            {loading ? <p className="search-message" role="status">Đang tìm sản phẩm…</p>
              : error ? <div className="search-message" role="alert"><p>{error}</p>
                <button type="button" onClick={() => setRetry((n) => n + 1)}>Thử lại</button></div>
                : <>
                  <p className="search-result-count" role="status">{result.totalItems} sản phẩm
                    {filters.search && <> cho “{filters.search}”</>}</p>
                  {result.items.length ? <div className="search-product-grid">
                    {result.items.map((p) => <article className="search-product-card" key={p.id}>
                      <button type="button" className="search-product-image" aria-label={`Xem ${p.name}`}
                        onClick={() => selectProduct(p)}>
                        {p.images?.[0]?.imageUrl ? <img loading="lazy" src={p.images[0].imageUrl}
                          alt={p.images[0].altText || p.name} /> : <Package size={56} aria-hidden="true" />}
                      </button>
                      <div className="search-product-info"><p>{p.brandName} · {p.categoryName}</p>
                        <button type="button" className="search-product-title" onClick={() => selectProduct(p)}>
                          <h2>{p.name}</h2></button>
                        <div className="search-product-price"><strong>{money(p.price)}</strong>
                          {p.originalPrice > p.price && <del>{money(p.originalPrice)}</del>}</div>
                        <button type="button" className="search-add" disabled={busy} onClick={() => onAdd?.(p)}>
                          <ShoppingBag size={17} /> Thêm vào giỏ</button>
                      </div>
                    </article>)}
                  </div> : <div className="search-message"><Package size={40} aria-hidden="true" />
                    <h2>Chưa tìm thấy sản phẩm phù hợp</h2><p>Thử từ khóa khác hoặc mở rộng bộ lọc.</p>
                    <button type="button" onClick={reset}>Xem tất cả sản phẩm</button></div>}
                  {result.totalPages > 1 && <nav className="search-pagination" aria-label="Phân trang kết quả">
                    <button type="button" disabled={page <= 1} onClick={() => { setLoading(true); setPage(page - 1) }}>
                      <ArrowLeft size={16} /> Trước</button>
                    <span>Trang {page} / {result.totalPages}</span>
                    <button type="button" disabled={page >= result.totalPages}
                      onClick={() => { setLoading(true); setPage(page + 1) }}>Sau <ArrowRight size={16} /></button>
                  </nav>}
                </>}
          </div>
        </div>
      </form>
    </section>
  )
}
