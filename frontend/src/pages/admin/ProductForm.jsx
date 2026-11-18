import { useEffect, useRef, useState } from 'react'
import { X } from 'lucide-react'

export default function ProductForm({ product, categories, brands, busy, onSave, onClose }) {
  const [error, setError] = useState('')
  const [images, setImages] = useState(() => [...(product?.images || [])]
    .sort((a, b) => a.sortOrder - b.sortOrder).map(image => ({ ...image, preview: image.imageUrl })))
  const previews = useRef(new Set())
  useEffect(() => {
    const urls = previews.current
    return () => { urls.forEach(url => URL.revokeObjectURL(url)); urls.clear() }
  }, [])
  const chooseImages = event => {
    const files = [...event.target.files]
    event.target.value = ''
    if (!files.length) return
    if (images.length + files.length > 10) { setError('Tối đa 10 ảnh cho một sản phẩm.'); return }
    if (files.some(file => !['image/jpeg', 'image/png', 'image/webp'].includes(file.type)
      || file.size === 0 || file.size > 5 * 1024 * 1024)) {
      setError('Chọn ảnh JPG, PNG hoặc WebP, mỗi ảnh không vượt quá 5 MB.'); return
    }
    setImages(current => [...current, ...files.map(file => {
      const preview = URL.createObjectURL(file)
      previews.current.add(preview)
      return { file, preview }
    })])
    setError('')
  }
  const removeImage = index => {
    const image = images[index]
    if (image.file) { URL.revokeObjectURL(image.preview); previews.current.delete(image.preview) }
    setImages(current => current.filter((_, position) => position !== index))
  }
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
      images: images.filter(image => !image.file).map((image, sortOrder) => ({
        imageUrl: image.imageUrl, altText: image.altText || name, sortOrder,
      })),
    }
    for (const key of ['switchType', 'sound', 'layout', 'connectionType', 'keycap', 'color']) {
      payload[key] = values.get(key).trim() || null
    }
    setError('')
    try { await onSave(payload, images.filter(image => image.file).map(image => image.file)) }
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
        <section className="pm-images" aria-label="Ảnh sản phẩm">
          <label>Thêm ảnh sản phẩm<input type="file" accept="image/jpeg,image/png,image/webp" multiple
            disabled={images.length >= 10} onChange={chooseImages} aria-describedby="pm-image-help" /></label>
          <p id="pm-image-help" className="pm-help">JPG, PNG, WebP · tối đa 5 MB/ảnh, 10 ảnh.
            Ảnh đầu tiên là ảnh đại diện.</p>
          {!images.length ? <p className="pm-help">Chưa chọn ảnh.</p>
            : <ol className="pm-image-list">{images.map((image, index) => <li key={image.preview}>
              <img src={image.preview} alt={image.altText || `Ảnh sản phẩm ${index + 1}`} />
              <span>{index === 0 ? 'Ảnh đại diện' : `Ảnh ${index + 1}`}</span>
              <button type="button" onClick={() => removeImage(index)} aria-label={`Bỏ ảnh ${index + 1}`}>
                <X size={16} aria-hidden="true" />Bỏ ảnh</button>
            </li>)}</ol>}
        </section>
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
        <label>Âm thanh<input name="sound" maxLength={50} defaultValue={product?.sound || ''}
          list="product-sounds" /></label>
        <datalist id="product-sounds"><option value="Thocky" /><option value="Clacky" /></datalist>
        <p className="pm-help">Nhập Thocky cho âm trầm; để trống nếu chưa xác định. Gợi ý Linear dùng trường Switch.</p>
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
