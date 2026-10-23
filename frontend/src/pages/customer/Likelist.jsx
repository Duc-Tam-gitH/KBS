import { useState, useMemo, useEffect, useRef } from 'react'
import './Likelist.css'
import { api } from '../../services/api'
import {
  Heart,
  HeartOff,
  ShoppingBag,
  ShoppingCart,
  Trash2,
  Search,
  ArrowRight,
  ArrowLeft,
  Undo2,
  LayoutGrid,
  List,
  X,
  Package,
} from 'lucide-react'

const money = (n) => new Intl.NumberFormat('vi-VN').format(n || 0) + '₫'

export const normalizeItem = (item) => ({
  id: item.productId,
  name: item.productName,
  brand: item.brandName,
  category: item.categoryName,
  price: Number(item.price) || 0,
  originalPrice: item.originalPrice == null ? null : Number(item.originalPrice),
  isActive: item.isActive,
  inStock: item.isActive === true && item.stockQuantity > 0,
  images: item.images || [],
  createdAt: item.createdAt,
})

export default function Likelist({
  onAddToCart,
  onSelectProduct,
  onNavigate,
  busy: cartBusy = false,
  user = null,
}) {
  const [likes, updateLikes] = useState([])
  const [loading, setLoading] = useState(!!user)
  const [loadError, setLoadError] = useState('')
  const [retry, setRetry] = useState(0)
  const [working, setWorking] = useState(false)
  const lock = useRef(false)
  const busy = cartBusy || working || loading
  const [search, setSearch] = useState('')
  const [category, setCategory] = useState('')
  const [sort, setSort] = useState('recent')
  const [view, setView] = useState('grid')
  const [toast, setToast] = useState(null)
  const [recentlyRemoved, setRecentlyRemoved] = useState(null)
  const [confirmClearOpen, setConfirmClearOpen] = useState(false)
  const filtered = useMemo(() => {
    const query = search.trim().toLocaleLowerCase('vi')
    return likes.filter((p) => (!category || p.category === category)
      && `${p.name} ${p.brand} ${p.category}`.toLocaleLowerCase('vi').includes(query))
      .sort((a, b) => sort === 'price-asc' ? a.price - b.price
        : sort === 'price-desc' ? b.price - a.price
          : sort === 'name' ? a.name.localeCompare(b.name, 'vi')
            : new Date(b.createdAt) - new Date(a.createdAt))
  }, [likes, search, category, sort])

  useEffect(() => {
    let active = true
    updateLikes([])
    setToast(null)
    setRecentlyRemoved(null)
    setLoadError('')
    setLoading(!!user)
    if (user) {
      api.likelist()
        .then((items) => { if (active) updateLikes(items.map(normalizeItem)) })
        .catch((err) => { if (active) setLoadError(err.message || 'Không thể tải danh sách yêu thích.') })
        .finally(() => { if (active) setLoading(false) })
    }
    return () => { active = false }
  }, [user, retry])

  const runAction = async (action) => {
    if (!user || lock.current || busy) return
    lock.current = true
    setWorking(true)
    try {
      await action()
    } catch (err) {
      setToast({ type: 'error', message: err.message || 'Thao tác thất bại. Vui lòng thử lại.' })
    } finally {
      lock.current = false
      setWorking(false)
    }
  }

  const removeItem = async (id) => {
    try {
      await api.removeLikeItem(id)
    } catch (err) {
      if (err.status !== 404) throw err
    }
    updateLikes((prev) => prev.filter((item) => item.id !== id))
  }

  const handleRemove = (product) => runAction(async () => {
    await removeItem(product.id)
    setRecentlyRemoved([product])
    setToast({ message: 'Đã xóa sản phẩm khỏi danh sách yêu thích.', canUndo: true })
  })

  const handleUndo = () => runAction(async () => {
    if (!recentlyRemoved) return
    for (const product of recentlyRemoved) {
      await api.addLikeItem(product.id)
      updateLikes((prev) => [product, ...prev.filter((item) => item.id !== product.id)])
      setRecentlyRemoved((prev) => prev?.filter((item) => item.id !== product.id))
    }
    updateLikes((await api.likelist()).map(normalizeItem))
    setRecentlyRemoved(null)
    setToast({ message: 'Đã khôi phục sản phẩm yêu thích.' })
  })

  const addCart = async (product) => {
    if (!product.inStock) throw new Error('Sản phẩm hiện không thể đặt mua.')
    if (onAddToCart) {
      if (await onAddToCart(product) === false) throw new Error('Chưa thêm được sản phẩm vào giỏ hàng.')
    } else {
      await api.addCartItem(product.id, 1)
    }
  }

  const handleAddCart = (product) => runAction(async () => {
    await addCart(product)
    setToast({ message: 'Đã thêm sản phẩm vào giỏ hàng.' })
  })

  const handleAddAllToCart = () => runAction(async () => {
    let added = 0
    try {
      for (const product of likes.filter((item) => item.inStock)) {
        await addCart(product)
        added++
      }
    } catch (err) {
      throw new Error('Đã thêm ' + added + ' sản phẩm. ' + err.message)
    }
    setToast({ message: 'Đã thêm ' + added + ' sản phẩm vào giỏ hàng.' })
  })

  const handleClearAllConfirmed = () => runAction(async () => {
    setConfirmClearOpen(false)
    setRecentlyRemoved(null)
    for (const product of likes) await removeItem(product.id)
    setRecentlyRemoved(likes)
    setToast({ message: 'Đã xóa toàn bộ danh sách yêu thích.', canUndo: true })
  })

  const handleViewDetail = (product) => runAction(async () => {
    const detail = await api.product(product.id)
    const selected = { ...detail, brand: detail.brandName, category: detail.categoryName,
      connection: detail.connectionType }
    if (onSelectProduct) onSelectProduct(selected)
    else onNavigate?.('detail', selected)
  })

  return (
    <section className="likelist-page">
      <div className="likelist-toolbar">
        <button onClick={() => onNavigate?.('shop')}><ArrowLeft size={18} /> Tiếp tục khám phá</button>
        {!!likes.length && <button disabled={busy} onClick={() => setConfirmClearOpen(true)}>
          <Trash2 size={18} /> Xóa tất cả
        </button>}
      </div>
      <header className="likelist-hero">
        <div>
          <p className="likelist-eyebrow"><Heart size={16} /> BỘ SƯU TẬP CỦA BẠN</p>
          <h1>Những tuyệt tác<br /><em>bạn rung động nhất</em></h1>
          <p>Không gian lưu trữ những sản phẩm bạn đã chọn cho góc làm việc của mình.</p>
        </div>
        <div>
          <dl className="likelist-stats">
            <div><dt>Đã thích</dt><dd>{likes.length}</dd></div>
            <div><dt>Sẵn hàng</dt><dd>{likes.filter((p) => p.inStock).length}</dd></div>
            <div><dt>Ước tính</dt><dd>{money(likes.reduce((sum, p) => sum + p.price, 0))}</dd></div>
          </dl>
          {!!likes.length && <button className="likelist-primary" disabled={busy || !likes.some((p) => p.inStock)}
            onClick={handleAddAllToCart}><ShoppingCart size={18} /> Thêm tất cả vào giỏ hàng</button>}
        </div>
      </header>
      {toast && <div className="likelist-state likelist-toolbar" role={toast.type === 'error' ? 'alert' : 'status'}>
        <p>{toast.message}</p>
        {toast.canUndo && recentlyRemoved?.length > 0 && <button disabled={busy} onClick={handleUndo}>
          <Undo2 size={18} /> Hoàn tác</button>}
        <button aria-label="Đóng thông báo" onClick={() => setToast(null)}><X size={18} /></button>
      </div>}
      {!user ? <div className="likelist-state" role="status">
        <p>Đăng nhập để xem sản phẩm yêu thích của bạn.</p>
        <button onClick={() => onNavigate?.('auth')}>Đăng nhập</button>
      </div> : loading ? <div className="likelist-state" role="status">Đang tải danh sách yêu thích…</div>
        : loadError ? <div className="likelist-state" role="alert"><p>{loadError}</p>
          <button onClick={() => setRetry((n) => n + 1)}>Thử lại</button></div>
          : !likes.length ? <div className="likelist-state">
            <HeartOff size={40} /><h2>Danh sách yêu thích đang trống</h2>
            <p>Bạn chưa lưu sản phẩm nào vào danh sách yêu thích.</p>
            <button onClick={() => onNavigate?.('shop')}>Khám phá bộ sưu tập <ArrowRight size={18} /></button>
          </div> : <>
            <div className="likelist-filters">
              <label className="likelist-search"><Search size={18} />
                <input aria-label="Tìm sản phẩm yêu thích" placeholder="Tìm trong danh sách yêu thích…"
                  value={search} onChange={(e) => setSearch(e.target.value)} /></label>
              <select aria-label="Lọc danh mục" value={category} onChange={(e) => setCategory(e.target.value)}>
                <option value="">Tất cả danh mục</option>
                {[...new Set(likes.map((p) => p.category))].filter(Boolean).map((c) => <option key={c}>{c}</option>)}
              </select>
              <select aria-label="Sắp xếp" value={sort} onChange={(e) => setSort(e.target.value)}>
                <option value="recent">Mới thêm gần đây</option><option value="price-asc">Giá tăng dần</option>
                <option value="price-desc">Giá giảm dần</option><option value="name">Tên A–Z</option>
              </select>
              <button aria-label="Dạng lưới" aria-pressed={view === 'grid'} onClick={() => setView('grid')}>
                <LayoutGrid size={20} /></button>
              <button aria-label="Dạng danh sách" aria-pressed={view === 'list'} onClick={() => setView('list')}>
                <List size={20} /></button>
            </div>
            <p className="likelist-count">Hiển thị {filtered.length} / {likes.length} sản phẩm</p>
            <div className={`likelist-products likelist-products--${view}`}>
              {filtered.map((p) => <article className="likelist-card" key={p.id}>
                <button className="likelist-image" disabled={working || !p.isActive}
                  aria-label={`Xem ${p.name}`} onClick={() => handleViewDetail(p)}>
                  {p.images[0]?.imageUrl ? <img src={p.images[0].imageUrl} alt={p.images[0].altText || p.name} />
                    : <Package size={64} />}
                  <span>{!p.isActive ? 'Ngừng bán' : p.inStock ? 'Còn hàng' : 'Hết hàng'}</span>
                </button>
                <div className="likelist-card-body">
                  <p className="likelist-eyebrow">{p.brand} · {p.category}</p>
                  <button className="likelist-title" disabled={working || !p.isActive}
                    onClick={() => handleViewDetail(p)}><h2>{p.name}</h2></button>
                  <div className="likelist-price"><strong>{money(p.price)}</strong>
                    {p.originalPrice > p.price && <del>{money(p.originalPrice)}</del>}</div>
                  <div className="likelist-toolbar">
                    <button disabled={busy} aria-label={`Bỏ thích ${p.name}`} onClick={() => handleRemove(p)}>
                      <Heart size={20} fill="currentColor" /></button>
                    <button className="likelist-primary" disabled={busy || !p.inStock} onClick={() => handleAddCart(p)}>
                      <ShoppingBag size={18} /> {!p.isActive ? 'Ngừng bán' : p.inStock ? 'Thêm giỏ' : 'Hết hàng'}
                    </button>
                  </div>
                </div>
              </article>)}
            </div>
            {!filtered.length && <div className="likelist-state"><p>Không tìm thấy sản phẩm phù hợp.</p>
              <button onClick={() => { setSearch(''); setCategory('') }}>Xóa bộ lọc</button></div>}
          </>}
      {confirmClearOpen && <div className="likelist-modal" role="dialog" aria-modal="true"
        aria-labelledby="likelist-confirm" onKeyDown={(e) => { if (e.key === 'Escape') setConfirmClearOpen(false) }}>
        <div className="likelist-state"><h2 id="likelist-confirm">Xóa toàn bộ danh sách yêu thích?</h2>
          <p>{likes.length} sản phẩm sẽ được gỡ khỏi danh sách.</p>
          <button autoFocus onClick={() => setConfirmClearOpen(false)}>Giữ lại</button>
          <button disabled={busy} onClick={handleClearAllConfirmed}>Xác nhận xóa</button>
        </div>
      </div>}
    </section>
  )
}

