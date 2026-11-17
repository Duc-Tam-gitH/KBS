import { useEffect, useState, useSyncExternalStore } from 'react'
import { Package } from 'lucide-react'
import { api, authStore } from '../services/api'
import './Recommendations.css'

const money = value => new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(value)

export default function Recommendations({ onSelect }) {
  const user = useSyncExternalStore(authStore.subscribe, () => authStore.user, () => authStore.user)
  const [data, setData] = useState(null)
  const [error, setError] = useState('')
  const [retry, setRetry] = useState(0)
  useEffect(() => {
    const controller = new AbortController()
    let timer
    setData(null); setError('')
    const refresh = async () => {
      try {
        const result = await api.recommendations(controller.signal)
        if (!controller.signal.aborted) { setData(result); setError('') }
      } catch (err) {
        if (!controller.signal.aborted) setError(err.message || 'Không tải được gợi ý sản phẩm.')
      } finally {
        if (!controller.signal.aborted) timer = setTimeout(refresh, 10000)
      }
    }
    refresh()
    return () => { controller.abort(); clearTimeout(timer) }
  }, [user, retry])
  return <section className="home-recommendations" aria-labelledby="recommendations-title">
    <header><p>GỢI Ý THEO SỞ THÍCH</p><h2 id="recommendations-title">Tìm chất phím dành cho bạn</h2>
      <p>{data?.mode === 'PERSONALIZED' ? 'Sản phẩm tương đồng với những món bạn đã mua gần đây.'
        : 'Khám phá sản phẩm dành cho bạn.'}</p></header>
    {error ? <p role="alert">{error} <button type="button" onClick={() => setRetry(x => x + 1)}>Thử lại</button></p>
      : !data ? <p role="status">Đang tải gợi ý…</p>
        : data.status === 'DISABLED' ? <p>Gợi ý sản phẩm hiện chưa được bật.</p>
          : data.status === 'WAITING' ? <p role="status">Đang chờ cập nhật gợi ý sản phẩm.</p>
            : !data.items.length ? <p>Chưa có sản phẩm còn hàng phù hợp để gợi ý.</p>
              : <div className="recommendation-grid">{data.items.map(product =>
                <button type="button" className="recommendation-card" key={product.id}
                  onClick={() => onSelect(product)}>
                  <RecommendationImage product={product} />
                  <strong>{product.name}</strong><span>{money(product.price)}</span>
                  <small>{[product.switchType, product.sound, product.layout].filter(Boolean).join(' · ')}</small>
                </button>)}</div>}
  </section>
}

function RecommendationImage({ product }) {
  const [failed, setFailed] = useState(false)
  const url = product.images?.[0]?.imageUrl
  useEffect(() => setFailed(false), [url])
  return url && !failed ? <img src={url} alt={product.name} loading="lazy" onError={() => setFailed(true)} />
    : <span className="recommendation-image"><Package aria-hidden="true" />Chưa có ảnh</span>
}
