import { useState } from 'react'
import { X } from 'lucide-react'

export default function ProductForm({ product, categories, brands, busy, onSave, onClose }) {
  const [error, setError] = useState('')
  const submit = async event => {
    event.preventDefault()
    if (busy) return
    const values = new FormData(event.currentTarget)
    const name = values.get('name').trim()
    if (name.length < 2) { setError('Tên sản phẩm cần ít nhất 2 ký tự.'); return }
    const payload = {
      ...product,
      name,
      price: Number(values.get('price')),
      originalPrice: values.get('originalPrice') === '' ? null : Number(values.get('originalPrice')),
      categoryId: Number(values.get('categoryId')),
      brandId: Number(values.get('brandId')),
      description: values.get('description').trim() || null,
      isActive: product ? values.get('isActive') === 'on' : true,
      isHotSwap: values.get('isHotSwap') === 'on',
      isRGB: values.get('isRGB') === 'on',
      images: product?.images || [],
    }
    for (const key of ['switchType', 'layout', 'connectionType', 'keycap', 'color']) {
      payload[key] = values.get(key).trim() || null
    }
    setError('')
    try { await onSave(payload) }
    catch (err) { setError(err.message || 'Không thể lưu sản phẩm.') }
  }
  return <>
    <header className="pm-form-heading"><div>
      <p className="ad-eyebrow">{product ? `SẢN PHẨM #${product.id}` : 'DANH MỤC SẢN PHẨM'}</p>
      <h2 id="pm-form-title">{product ? 'Sửa sản phẩm' : 'Thêm sản phẩm'}</h2>
    </div><button type="button" className="pm-icon" aria-label="Đóng form" onClick={onClose} disabled={busy}>
      <X size={18} /></button></header>
    <form className="pm-form" onSubmit={submit}>
      <fieldset disabled={busy} className="pm-form-body">
        <label>Tên sản phẩm<input name="name" required minLength={2} maxLength={200}
          defaultValue={product?.name || ''} autoFocus /></label>
        <div className="pm-form-grid">
          <label>Danh mục<select name="categoryId" required defaultValue={product?.categoryId || ''}>
            <option value="" disabled>Chọn danh mục</option>
            {categories.map(item => <option value={item.id} key={item.id}>{item.name}</option>)}
          </select></label>
          <label>Thương hiệu<select name="brandId" required defaultValue={product?.brandId || ''}>
            <option value="" disabled>Chọn thương hiệu</option>
            {product && !brands.some(item => item.id === product.brandId) &&
              <option value={product.brandId} disabled>{product.brandName || '--'} (không khả dụng)</option>}
            {brands.map(item => <option value={item.id} key={item.id}>{item.name}</option>)}
          </select></label>
        </div>
        {(!categories.length || !brands.length) && <p className="pm-help">
          Cần có danh mục và thương hiệu để lưu sản phẩm.</p>}
        <div className="pm-form-grid">
          <label>Giá bán (₫)<input name="price" type="number" required min="0" max="999999999" step="0.01"
            defaultValue={product?.price ?? ''} /></label>
          <label>Giá gốc (₫)<input name="originalPrice" type="number" min="0" step="0.01"
            defaultValue={product?.originalPrice ?? ''} /></label>
        </div>
        <div className="pm-stock-info"><span>Tồn kho</span>
          <strong>{product ? product.stockQuantity.toLocaleString('vi-VN') : 'Khởi tạo khi lưu'}</strong>
          <p>Số lượng được cập nhật qua nghiệp vụ nhập / xuất kho.</p></div>
        <label>Mô tả<textarea name="description" rows={3} defaultValue={product?.description || ''} /></label>
        <details className="pm-specs"><summary>Thông số kỹ thuật</summary>
          <div className="pm-form-grid">
            {[['switchType', 'Switch'], ['layout', 'Layout'], ['connectionType', 'Kết nối'],
              ['keycap', 'Keycap'], ['color', 'Màu sắc']].map(([key, label]) =>
              <label key={key}>{label}<input name={key} defaultValue={product?.[key] || ''} /></label>)}
          </div>
          <div className="pm-checks">
            <label><input name="isHotSwap" type="checkbox" defaultChecked={product?.isHotSwap} />Hot swap</label>
            <label><input name="isRGB" type="checkbox" defaultChecked={product?.isRGB} />RGB</label>
          </div>
        </details>
        {product && <label className="pm-active-check">
          <input name="isActive" type="checkbox" defaultChecked={product.isActive} />Đang bán</label>}
        {error && <p role="alert">{error}</p>}
      </fieldset>
      <footer className="pm-form-actions"><button type="button" disabled={busy} onClick={onClose}>Hủy</button>
        <button type="submit" className="ad-primary" disabled={busy || !categories.length || !brands.length}>
          {busy ? 'Đang lưu…' : product ? 'Lưu thay đổi' : 'Thêm sản phẩm'}</button></footer>
    </form>
  </>
}
