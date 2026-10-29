import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { createElement } from 'react'
import { renderToString } from 'react-dom/server'
import { createServer } from 'vite'
import { filterInventory, stockStatus, stockAdjustment, inventoryCsv } from '../src/pages/admin/inventoryUtils.js'

// MOCK DATA — isolated test input, never used by the application UI.
const items = [
  { productId: 1, name: '=SUM(1,2)', categoryId: 2, categoryName: 'Keys', stockQuantity: 0,
    lowStockThreshold: 5, orderedQuantity: 3, updatedAt: '2026-01-01T00:00:00Z' },
  { productId: 2, name: 'Beta', categoryId: 3, categoryName: 'Switches', stockQuantity: 4,
    lowStockThreshold: 4, orderedQuantity: 0, updatedAt: '2026-01-02T00:00:00Z' },
  { productId: 3, name: 'Alpha', categoryId: 3, categoryName: 'Switches', stockQuantity: 5,
    lowStockThreshold: 4, orderedQuantity: 1, updatedAt: '2026-01-03T00:00:00Z' },
]
assert.deepEqual(items.map(stockStatus), ['empty', 'low', 'available'])
assert.equal(stockStatus({ stockQuantity: 0, lowStockThreshold: 0 }), 'empty')
assert.deepEqual(filterInventory(items, { query: ' #2 ' }).map(x => x.productId), [2])
assert.deepEqual(filterInventory(items, { category: '3', status: 'low' }).map(x => x.productId), [2])
assert.deepEqual(filterInventory(items, { query: 'ALPHA', sort: 'name' }).map(x => x.productId), [3])
assert.deepEqual(filterInventory(items, { sort: 'desc' }).map(x => x.productId), [3, 2, 1])
assert.deepEqual(filterInventory(items, { sort: 'updated' }).map(x => x.productId), [3, 2, 1])
assert.deepEqual(stockAdjustment(items[1], 'in', 3, ' Count '), { delta: 3, expectedQuantity: 4, reason: 'Count' })
assert.equal(stockAdjustment(items[1], 'out', 4, 'Count').delta, -4)
assert.equal(stockAdjustment(items[1], 'adjust', 0, 'Count').delta, -4)
assert.throws(() => stockAdjustment(items[1], 'out', 5, 'Count'))
assert.throws(() => stockAdjustment(items[1], 'in', 1.5, 'Count'))
assert.throws(() => stockAdjustment(items[1], 'adjust', 4, 'Count'))
assert.throws(() => stockAdjustment(items[1], 'in', 1, ' '))
assert.throws(() => stockAdjustment(items[1], 'in', 2147483647, 'Count'))
const csv = inventoryCsv(items)
assert.ok(csv.startsWith('\uFEFF'))
assert.ok(csv.includes('"\'=SUM(1,2)"'))
assert.equal(csv.split('\r\n').length, 4)
assert.equal(items[1].stockQuantity, 4)
const root = fileURLToPath(new URL('../', import.meta.url))
const server = await createServer({ root, server: { middlewareMode: true }, appType: 'custom' })
try {
  const { default: Page, InventoryTable, StockForm } = await server.ssrLoadModule('/src/pages/admin/InventoryManagement.jsx')
  assert.match(renderToString(createElement(Page)), /Đang tải tồn kho/)
  assert.match(renderToString(createElement(InventoryTable, { items: [] })), /Không có sản phẩm phù hợp/)
  const table = renderToString(createElement(InventoryTable, { items }))
  assert.match(table, /Mã sản phẩm/)
  assert.match(table, /aria-label="Xuất =SUM\(1,2\)" disabled/)
  assert.doesNotMatch(table, /Giá nhập|SKU|Kho HCM/)
  const form = renderToString(createElement(StockForm, { items, mode: 'adjust' }))
  assert.match(form, /min="0"/)
  assert.match(form, /name="reason" required/)
  assert.doesNotMatch(readFileSync(new URL('../src/pages/admin/InventoryManagement.css', import.meta.url), 'utf8'),
    /#[0-9a-f]{3,8}\b|rgba?\(/i)
  console.log('PASS: Inventory render, filters, stock boundaries, adjustment validation, CSV and palette.')
} finally { await server.close() }
