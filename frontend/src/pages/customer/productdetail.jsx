import { useEffect, useRef, useState } from 'react'
import { ArrowLeft, Heart, Minus, Package, Plus, Share2, ShoppingCart, Star, Store } from 'lucide-react'
import { api } from '../../services/api'
import './productdetail.css'

const money = (value) => new Intl.NumberFormat('vi-VN', {
  style: 'currency', currency: 'VND',
}).format(value)

function ProductImage({ image, name, ...props }) {
  const [failed, setFailed] = useState(false)
  if (!image?.imageUrl || failed) {
    return <span className="pd-placeholder"><Package size={48} aria-hidden="true" />Chưa có ảnh</span>
  }
  return <img src={image.imageUrl} alt={image.altText || name} onError={() => setFailed(true)} {...props} />
}

export default function ProductDetail({
  product: selected, onBack, onAdd, onNavigate, onSelect, user = null, busy = false,
}) {
  const [product, setProduct] = useState(null)
  const [loading, setLoading] = useState(!!selected?.id)
  const [error, setError] = useState('')
  const [retry, setRetry] = useState(0)
  const [imageIndex, setImageIndex] = useState(0)
  const [quantity, setQuantity] = useState(1)
  const [working, setWorking] = useState(false)
  const [message, setMessage] = useState('')
  const [liked, setLiked] = useState(false)
  const [likesLoading, setLikesLoading] = useState(!!user)
  const [likesError, setLikesError] = useState('')
  const [likeRetry, setLikeRetry] = useState(0)
  const [related, setRelated] = useState([])
  const [relatedLoading, setRelatedLoading] = useState(true)
  const [relatedError, setRelatedError] = useState('')
  const [relatedRetry, setRelatedRetry] = useState(0)
  const lock = useRef(false)
  const mounted = useRef(true)

  useEffect(() => {
    if (product && selected?.section === 'reviews') {
      document.getElementById('pd-reviews-title')?.scrollIntoView({ block: 'start' })
    }
  }, [product, selected?.section])

  useEffect(() => {
    mounted.current = true
    return () => { mounted.current = false }
  }, [])

  useEffect(() => {
    let active = true
    if (selected?.id) {
      api.product(selected.id)
        .then((result) => { if (active) setProduct(result) })
        .catch((err) => {
          if (active) setError(err.status === 404
            ? 'Sản phẩm không tồn tại hoặc đã ngừng bán.'
            : err.message || 'Không thể tải chi tiết sản phẩm.')
        })
        .finally(() => { if (active) setLoading(false) })
    }
    return () => { active = false }
  }, [selected?.id, retry])

  useEffect(() => {
    let active = true
    if (user && selected?.id) {
      api.likelist()
        .then((items) => { if (active) setLiked(items.some((item) => item.productId === selected.id)) })
        .catch(() => { if (active) setLikesError('Không thể tải trạng thái yêu thích.') })
        .finally(() => { if (active) setLikesLoading(false) })
    }
    return () => { active = false }
  }, [user, selected?.id, likeRetry])

  useEffect(() => {
    let active = true
    if (product) {
      api.products({ categoryId: product.categoryId, page: 1, pageSize: 5 })
        .then((result) => {
          if (active) setRelated(result.items.filter((item) => item.id !== product.id).slice(0, 4))
        })
        .catch(() => { if (active) setRelatedError('Không thể tải sản phẩm cùng danh mục.') })
        .finally(() => { if (active) setRelatedLoading(false) })
    }
    return () => { active = false }
  }, [product, relatedRetry])

  const runAction = async (action) => {
    if (lock.current) return
    lock.current = true
    setWorking(true)
    setMessage('')
    try {
      await action()
    } catch (err) {
      if (mounted.current) setMessage(err.message || 'Thao tác thất bại. Vui lòng thử lại.')
    } finally {
      lock.current = false
      if (mounted.current) setWorking(false)
    }
  }

  const maxQuantity = Math.min(100, Math.max(0, product?.stockQuantity ?? 0))
  const available = product?.isActive && maxQuantity > 0
  const images = product?.images || []
  const addCart = (buyNow = false) => runAction(async () => {
    if (!available || busy || quantity < 1 || quantity > maxQuantity) return
    if (await onAdd(product, quantity)) {
      if (buyNow) onNavigate?.('cart')
      else if (mounted.current) setMessage('Đã thêm sản phẩm vào giỏ hàng.')
    } else if (mounted.current) {
      setMessage('Chưa thêm được sản phẩm vào giỏ hàng. Vui lòng kiểm tra thông báo và thử lại.')
    }
  })

  const toggleLike = () => runAction(async () => {
    if (!user) { onNavigate?.('auth'); return }
    if (likesLoading || likesError) return
    if (liked) {
      try {
        await api.removeLikeItem(product.id)
      } catch (err) {
        if (err.status !== 404) throw err
      }
    } else {
      await api.addLikeItem(product.id)
    }
    if (mounted.current) setLiked(!liked)
  })

  const share = () => runAction(async () => {
    const url = new URL(window.location.href)
    url.hash = `product=${product.id}`
    await navigator.clipboard.writeText(url.href)
    if (mounted.current) setMessage('Đã sao chép liên kết sản phẩm.')
  })

  return (
    <section className="product-detail" aria-busy={loading}>
      <button type="button" className="pd-back" onClick={onBack}>
        <ArrowLeft size={18} aria-hidden="true" /> Trở lại cửa hàng
      </button>
      {loading ? <p className="pd-panel" role="status">Đang tải chi tiết sản phẩm…</p>
        : error ? <div className="pd-panel" role="alert">
          <p>{error}</p><button type="button" onClick={() => {
            setLoading(true)
            setError('')
            setRetry((value) => value + 1)
          }}>Thử lại</button>
        </div>
          : !product ? <p className="pd-panel">Không tìm thấy sản phẩm.</p>
            : <div className="pd-layout">
              <aside className="pd-shop pd-panel" aria-label="Thông tin cửa hàng">
                <div className="pd-shop-logo"><Store size={32} aria-hidden="true" /></div>
                <h2>KBS</h2>
                <p className="pd-muted">Keyboard Beyond Standard</p>
                <button type="button" onClick={onBack}>Xem cửa hàng</button>
                <dl>
                  <div><dt>Thương hiệu</dt><dd>{product.brandName || 'Chưa cập nhật'}</dd></div>
                  <div><dt>Danh mục</dt><dd>{product.categoryName || 'Chưa cập nhật'}</dd></div>
                  <div><dt>Mã sản phẩm</dt><dd>#{product.id}</dd></div>
                </dl>
              </aside>
              <div className="pd-main">
                <div className="pd-top">
                  <div className="pd-gallery pd-panel">
                    <div className="pd-main-image">
                      <ProductImage key={images[imageIndex]?.imageUrl || 'empty'}
                        image={images[imageIndex]} name={product.name} />
                    </div>
                    {images.length > 1 && <div className="pd-thumbnails" aria-label="Ảnh sản phẩm">
                      {images.map((image, index) => <button type="button" key={image.id ?? index}
                        aria-label={`Xem ảnh ${index + 1}`} aria-pressed={index === imageIndex}
                        onClick={() => setImageIndex(index)}>
                        <ProductImage image={image} name="" loading="lazy" />
                      </button>)}
                    </div>}
                    <div className="pd-social">
                      <button type="button" disabled={working} onClick={share}>
                        <Share2 size={18} aria-hidden="true" /> Chia sẻ
                      </button>
                      <button type="button" aria-pressed={liked}
                        disabled={working || likesLoading || !!likesError} onClick={toggleLike}>
                        <Heart size={18} fill={liked ? 'currentColor' : 'none'} aria-hidden="true" />
                        {liked ? 'Đã yêu thích' : 'Yêu thích'}
                      </button>
                    </div>
                    {likesError && <div role="alert"><p>{likesError}</p>
                      <button type="button" onClick={() => {
                        setLikesLoading(true)
                        setLikesError('')
                        setLikeRetry((value) => value + 1)
                      }}>Thử lại</button>
                    </div>}
                  </div>
                  <div className="pd-info pd-panel">
                    <p className="pd-eyebrow">{product.brandName}</p>
                    <h1>{product.name}</h1>
                    <div className="pd-price">
                      <strong>{money(product.price)}</strong>
                      {product.originalPrice > product.price && <del>{money(product.originalPrice)}</del>}
                    </div>
                    <p className="pd-stock">
                      {available ? `Còn ${product.stockQuantity} sản phẩm` : 'Tạm hết hàng'}
                    </p>
                    <dl>
                      {[
                        ['Layout', product.layout], ['Kết nối', product.connectionType],
                        ['Switch', product.switchType], ['Keycap', product.keycap], ['Màu sắc', product.color],
                        ['Hot-swap', product.isHotSwap ? 'Có' : 'Không'],
                        ['RGB', product.isRGB ? 'Có' : 'Không'],
                      ].map(([label, value]) => <div key={label}>
                        <dt>{label}</dt><dd>{value || 'Chưa cập nhật'}</dd>
                      </div>)}
                    </dl>
                    <div className="pd-quantity" role="group" aria-label="Số lượng">
                      <span>Số lượng</span>
                      <button type="button" aria-label="Giảm số lượng"
                        disabled={!available || working || quantity <= 1}
                        onClick={() => setQuantity((value) => value - 1)}><Minus size={16} /></button>
                      <output aria-label="Số lượng đã chọn">{quantity}</output>
                      <button type="button" aria-label="Tăng số lượng"
                        disabled={!available || working || quantity >= maxQuantity}
                        onClick={() => setQuantity((value) => value + 1)}><Plus size={16} /></button>
                    </div>
                    <div className="pd-actions">
                      <button type="button" disabled={!available || busy || working} onClick={() => addCart()}>
                        <ShoppingCart size={18} aria-hidden="true" /> Thêm vào giỏ
                      </button>
                      <button type="button" className="pd-primary" disabled={!available || busy || working}
                        onClick={() => addCart(true)}>Mua ngay</button>
                    </div>
                    <p className="pd-muted">Mua ngay sẽ thêm sản phẩm và mở giỏ hàng để bạn kiểm tra.</p>
                  </div>
                </div>
                {message && <p className="pd-panel" role="status">{message}</p>}
                <section className="pd-panel pd-description" aria-labelledby="pd-description-title">
                  <h2 id="pd-description-title">Mô tả sản phẩm</h2>
                  <p>{product.description || 'Chưa có mô tả cho sản phẩm này.'}</p>
                </section>
                <ProductReviews key={`${product.id}-${user?.id || 'guest'}`} productId={product.id}
                  user={user} onNavigate={onNavigate} />
                <section className="pd-panel" aria-labelledby="pd-related-title">
                  <h2 id="pd-related-title">Sản phẩm cùng danh mục</h2>
                  {relatedLoading ? <p role="status">Đang tải sản phẩm…</p>
                    : relatedError ? <div role="alert"><p>{relatedError}</p>
                      <button type="button" onClick={() => {
                        setRelatedLoading(true)
                        setRelatedError('')
                        setRelatedRetry((value) => value + 1)
                      }}>Thử lại</button>
                    </div>
                      : !related.length ? <p>Chưa có sản phẩm khác trong danh mục này.</p>
                        : <div className="pd-related">
                          {related.map((item) => <button type="button" className="pd-related-card" key={item.id}
                            onClick={() => onSelect?.(item)}>
                            <div className="pd-related-image">
                              <ProductImage image={item.images?.[0]} name={item.name} loading="lazy" />
                            </div>
                            <span>{item.name}</span><strong>{money(item.price)}</strong>
                          </button>)}
                        </div>}
                </section>
              </div>
            </div>}
    </section>
  )
}

function ProductReviews({ productId, user, onNavigate }) {
  const [result, setResult] = useState(null)
  const [rating, setRating] = useState('')
  const [page, setPage] = useState(1)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [retry, setRetry] = useState(0)
  const [submitting, setSubmitting] = useState(false)
  const [message, setMessage] = useState('')
  const [submitted, setSubmitted] = useState(false)
  const lock = useRef(false)

  useEffect(() => {
    let active = true
    api.productReviews(productId, { page, ...(rating ? { rating } : {}) })
      .then((response) => { if (active) setResult(response) })
      .catch((err) => { if (active) setError(err.message || 'Không thể tải đánh giá.') })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [productId, rating, page, retry])

  const submit = async (event) => {
    event.preventDefault()
    if (lock.current) return
    const form = event.currentTarget
    const data = new FormData(form)
    const comment = String(data.get('comment')).trim()
    if (!comment) { setMessage('Vui lòng nhập nhận xét.'); return }
    lock.current = true
    setSubmitting(true)
    setMessage('')
    try {
      await api.addProductReview(productId, { rating: Number(data.get('rating')), comment })
      setSubmitted(true)
      setMessage('Đã gửi đánh giá của bạn.')
      setPage(1)
      setRating('')
      setLoading(true)
      setError('')
      setRetry((value) => value + 1)
    } catch (err) {
      setMessage(err.message || 'Không thể gửi đánh giá.')
    } finally {
      lock.current = false
      setSubmitting(false)
    }
  }

  return (
    <section className="pd-panel pd-reviews" aria-labelledby="pd-reviews-title">
      <h2 id="pd-reviews-title">Đánh giá sản phẩm</h2>
      {result && <div className="pd-review-overview">
        <p><Star size={22} aria-hidden="true" />
          <strong>{result.totalReviews ? result.averageRating.toFixed(1) : '—'}</strong>
          / 5 · {result.totalReviews} đánh giá
        </p>
        <div className="pd-review-filters" role="group" aria-label="Lọc đánh giá">
          {['', '5', '4', '3', '2', '1'].map((value) => <button type="button" key={value}
            aria-pressed={rating === value} onClick={() => {
              if (rating === value && page === 1) return
              setLoading(true)
              setError('')
              setRating(value)
              setPage(1)
            }}>
            {value ? `${value} sao (${result.ratingCounts[value] || 0})` : 'Tất cả'}
          </button>)}
        </div>
      </div>}
      {loading ? <p role="status">Đang tải đánh giá…</p>
        : error ? <div role="alert"><p>{error}</p>
          <button type="button" onClick={() => {
            setLoading(true)
            setError('')
            setRetry((value) => value + 1)
          }}>Thử lại</button>
        </div>
          : <>
            {!result?.reviews.items.length ? <p>Chưa có đánh giá phù hợp.</p>
              : result.reviews.items.map((review) => <article className="pd-review" key={review.id}>
                <h3>{review.displayName}</h3>
                <p className="pd-review-rating"><Star size={16} aria-hidden="true" />{review.rating} / 5</p>
                <time className="pd-muted" dateTime={review.createdAt}>
                  {new Date(review.createdAt).toLocaleDateString('vi-VN')}
                </time>
                <p className="pd-review-comment">{review.comment}</p>
              </article>)}
            {result?.reviews.totalPages > 1 && <nav className="pd-pagination" aria-label="Phân trang đánh giá">
              <button type="button" disabled={page <= 1}
                onClick={() => { setLoading(true); setPage(page - 1) }}>Trước</button>
              <span>Trang {page} / {result.reviews.totalPages}</span>
              <button type="button" disabled={page >= result.reviews.totalPages}
                onClick={() => { setLoading(true); setPage(page + 1) }}>Sau</button>
            </nav>}
          </>}
      {!user ? <button type="button" onClick={() => onNavigate?.('auth')}>Đăng nhập để đánh giá</button>
        : !submitted && <form className="pd-review-form" onSubmit={submit}>
          <h3>Chia sẻ trải nghiệm của bạn</h3>
          <p className="pd-muted">Mỗi khách được đánh giá một lần sau khi đơn hàng đã hoàn tất.</p>
          <label htmlFor="pd-review-rating">Điểm đánh giá</label>
          <select id="pd-review-rating" name="rating" defaultValue="5" disabled={submitting}>
            {[5, 4, 3, 2, 1].map((value) => <option key={value} value={value}>{value} sao</option>)}
          </select>
          <label htmlFor="pd-review-comment">Nhận xét</label>
          <textarea id="pd-review-comment" name="comment" required maxLength={2000} rows={4}
            disabled={submitting} placeholder="Cảm nhận của bạn về sản phẩm" />
          <button className="pd-primary" disabled={submitting} type="submit">
            {submitting ? 'Đang gửi…' : 'Gửi đánh giá'}
          </button>
        </form>}
      {message && <p role="status">{message}</p>}
    </section>
  )
}
