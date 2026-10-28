export function filterProducts(products, query, category, status) {
  const term = query.trim().toLocaleLowerCase('vi')
  return products.filter(item =>
    (item.name.toLocaleLowerCase('vi').includes(term) || String(item.id).includes(term.replace(/^#/, '')))
    && (!category || String(item.categoryId) === category)
    && (!status || item.isActive === (status === 'active')))
}
