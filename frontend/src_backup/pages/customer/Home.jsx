import '../page.css'
import './Home.css'

const money = (value) => new Intl.NumberFormat('vi-VN').format(value || 0) + '₫'
const ProductImage = ({ product }) => product?.images?.[0]?.imageUrl
  ? <img src={product.images[0].imageUrl} alt={product.images[0].altText || product.name} />
  : <span>{product?.name || 'Bộ sưu tập KBS'}</span>

export default function Home({ onNavigate, products = [], loading, error, onAdd, busy }) {
  const featured = products[0]
  const viewFeatured = () => featured ? onNavigate('detail', featured) : onNavigate('shop')
  return (
    <div className="home-wrapper">
      <section className="hero-section-v2">
        <div className="hero-bg-text">KBS</div>
        <div className="hero-content">
          <h1 className="hero-title">Âm thanh<br />tĩnh lặng.</h1>
          <p className="hero-subtitle">
            Khám phá bộ sưu tập bàn phím cơ được thiết kế tinh giản,
            tối ưu cho trải nghiệm gõ và không gian làm việc của bạn.
          </p>
          <button onClick={() => onNavigate('shop')} className="btn-primary">Khám phá ngay</button>
        </div>
        <div className="hero-visual">
          <div className="main-image-placeholder">
            <ProductImage product={featured} />
            <button className="floating-btn" onClick={viewFeatured} aria-label="Xem sản phẩm nổi bật">↗</button>
          </div>
          {featured && <div className="floating-thumb">
            <div className="thumb-img"><ProductImage product={featured} /></div>
            <div className="thumb-info">
              <span className="thumb-name">{featured.name}</span>
              <span className="thumb-price">{money(featured.price)}</span>
            </div>
          </div>}
        </div>
      </section>
      <section className="feature-section">
        <div className="feature-text">
          <span className="section-tag">// FEATURED</span>
          <h2 className="feature-title">Không chỉ là gõ.<br />Mà là cảm giác.</h2>
          <p className="feature-desc">Tìm chiếc bàn phím phù hợp với nhịp làm việc và phong cách của bạn.</p>
          <button onClick={viewFeatured} className="btn-outline">Xem chi tiết</button>
        </div>
        <div className="feature-image-large"><ProductImage product={products[1] || featured} /></div>
        <div className="feature-thumbnails">
          {products.slice(0, 4).map((product) => (
            <button className="mini-thumb" key={product.id} onClick={() => onNavigate('detail', product)}
              aria-label={'Xem ' + product.name}><ProductImage product={product} /></button>
          ))}
        </div>
      </section>
      <div className="page-container">
        <div className="shop-header">
          <h2 className="section-heading">Bộ sưu tập nổi bật</h2>
          <button onClick={() => onNavigate('shop')} className="btn-text">Xem tất cả →</button>
        </div>
        {loading && <p role="status">Đang tải sản phẩm…</p>}
        {error && <p role="alert">{error}</p>}
        {!loading && !error && !products.length && <p>Chưa có sản phẩm.</p>}
        <div className="showcase-grid">
          {products.slice(0, 4).map((product) => (
            <article className="showcase-card product-card-v2" key={product.id}>
              <button className="card-img-placeholder" onClick={() => onNavigate('detail', product)}
                aria-label={'Xem ' + product.name}><ProductImage product={product} /></button>
              <div className="card-content">
                <h3>{product.name}</h3>
                <p>{product.layout} / {product.switchType || product.connection}</p>
                <div className="card-bottom">
                  <span className="price">{money(product.price)}</span>
                  <button className="icon-btn" disabled={busy} onClick={() => onAdd(product)}
                    aria-label={'Thêm ' + product.name + ' vào giỏ'}>+</button>
                </div>
              </div>
            </article>
          ))}
        </div>
      </div>
    </div>
  )
}
