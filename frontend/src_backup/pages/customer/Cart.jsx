const money = (n) => new Intl.NumberFormat('vi-VN').format(n || 0) + '₫'

export default function Cart({ cart, busy, onUpdate, onRemove, onCheckout, onNavigate }) {
  const total = cart.reduce((sum, item) => sum + item.price * item.quantity, 0)
  return (
    <section className="page-wrap">
      <h1>Những món bạn đã chọn</h1>
      {busy && <p role="status">Đang cập nhật giỏ hàng…</p>}
      <button className="back" onClick={() => onNavigate('shop')}>← Tiếp tục mua sắm</button>
      <div className="cart-layout">
        <div className="glass-card cart-list">
          {cart.length ? cart.map((item) => (
            <div className="cart-row" key={item.id}>
              <div><h3>{item.name}</h3><p>{money(item.price)}</p></div>
              <div className="stepper">
                <button disabled={busy || item.quantity <= 1}
                  onClick={() => onUpdate(item.id, item.quantity - 1)}
                  aria-label={'Giảm số lượng ' + item.name}>−</button>
                <span>{item.quantity}</span>
                <button disabled={busy || item.quantity >= 100}
                  onClick={() => onUpdate(item.id, item.quantity + 1)}
                  aria-label={'Tăng số lượng ' + item.name}>+</button>
              </div>
              <button disabled={busy} onClick={() => onRemove(item.id)} aria-label={'Xóa ' + item.name}>×</button>
            </div>
          )) : <p>Giỏ hàng đang trống.</p>}
        </div>
        <aside className="glass-card summary">
          <p>Tạm tính</p><strong>{money(total)}</strong>
          <small>Phí vận chuyển sẽ được tính ở bước tiếp theo.</small>
          <button className="ink-button" disabled={busy || !cart.length} onClick={onCheckout}>
            Tiếp tục thanh toán
          </button>
        </aside>
      </div>
    </section>
  )
}
