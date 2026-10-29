export const stockLabels = { available: 'Còn hàng', low: 'Sắp hết', empty: 'Hết hàng' }
export const stockStatus = item => item.stockQuantity === 0 ? 'empty'
  : item.stockQuantity <= item.lowStockThreshold ? 'low' : 'available'

export function filterInventory(items, { query = '', category = '', status = '', sort = 'asc' }) {
  const search = query.trim().toLocaleLowerCase('vi')
  return items.filter(item => (!search || (search.startsWith('#') ? String(item.productId) === search.slice(1)
    : String(item.productId).includes(search) || item.name.toLocaleLowerCase('vi').includes(search)))
    && (!category || String(item.categoryId) === category) && (!status || stockStatus(item) === status))
    .sort((a, b) => (sort === 'name' ? a.name.localeCompare(b.name, 'vi')
      : sort === 'updated' ? new Date(b.updatedAt) - new Date(a.updatedAt)
        : (a.stockQuantity - b.stockQuantity) * (sort === 'desc' ? -1 : 1)) || a.productId - b.productId)
}

export function stockAdjustment(item, mode, quantity, reason) {
  if (!Number.isInteger(quantity) || quantity < (mode === 'adjust' ? 0 : 1) || quantity > 2147483647)
    throw new Error('Số lượng phải là số nguyên hợp lệ.')
  if (!reason.trim() || reason.trim().length > 500) throw new Error('Nhập lý do từ 1 đến 500 ký tự.')
  const delta = mode === 'adjust' ? quantity - item.stockQuantity : mode === 'out' ? -quantity : quantity
  if (!delta) throw new Error(`Sản phẩm #${item.productId}: số lượng không thay đổi.`)
  if (item.stockQuantity + delta < 0 || item.stockQuantity + delta > 2147483647)
    throw new Error(`Sản phẩm #${item.productId}: số lượng sau cập nhật không hợp lệ.`)
  return { delta, expectedQuantity: item.stockQuantity, reason: reason.trim() }
}

export function inventoryCsv(items) {
  const cell = value => {
    let text = String(value ?? '')
    if (/^[\s]*[=+@-]|^[\t\r\n]/.test(text)) text = "'" + text
    return '"' + text.replaceAll('"', '""') + '"'
  }
  return '\uFEFF' + [
    ['Mã sản phẩm', 'Tên sản phẩm', 'Danh mục', 'Tồn kho', 'Đã đặt (đơn đang xử lý)', 'Ngưỡng', 'Trạng thái', 'Cập nhật'],
    ...items.map(item => [item.productId, item.name, item.categoryName, item.stockQuantity, item.orderedQuantity,
      item.lowStockThreshold, stockLabels[stockStatus(item)], item.updatedAt]),
  ].map(row => row.map(cell).join(',')).join('\r\n')
}
