import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { createElement } from 'react'
import { renderToString } from 'react-dom/server'
import { createServer } from 'vite'

const root = fileURLToPath(new URL('../', import.meta.url))
const server = await createServer({ root, server: { middlewareMode: true }, appType: 'custom' })
try {
  const { default: Page, AccountTable, AccountForm, filterAccounts } =
    await server.ssrLoadModule('/src/pages/admin/AccountManagement.jsx')
  assert.match(renderToString(createElement(Page)), /Đang tải tài khoản/)
  assert.match(renderToString(createElement(AccountTable, { items: [] })), /Không tìm thấy/)
  const empty = { search: '', role: '', active: '', days: '', sort: 'newest' }
  // MOCK DATA — isolated test records, never shipped to the UI.
  const records = [
    { id: 1, username: 'alpha', email: 'alpha@example.com', roleId: 2, roleName: 'Customer',
      isActive: true, createdAt: '2026-01-01T00:00:00Z' },
    { id: 2, username: 'beta', email: 'beta@example.com', phoneNumber: '0900000001', roleId: 3,
      roleName: 'Staff', isActive: false, createdAt: '2026-01-02T00:00:00Z' },
  ]
  assert.deepEqual(filterAccounts(records, empty).map(x => x.id), [2, 1])
  assert.deepEqual(filterAccounts(records, { ...empty, search: ' ALPHA ' }).map(x => x.id), [1])
  assert.deepEqual(filterAccounts(records, { ...empty, search: '090000' }).map(x => x.id), [2])
  assert.deepEqual(filterAccounts(records, { ...empty, role: '3', active: 'false' }).map(x => x.id), [2])
  assert.deepEqual(filterAccounts(records, { ...empty, role: '3', active: 'true' }), [])
  assert.deepEqual(filterAccounts(records, { ...empty, sort: 'oldest' }).map(x => x.id), [1, 2])
  assert.deepEqual(filterAccounts(records, { ...empty, days: '7' }, Date.parse('2026-01-09T00:00:00Z'))
    .map(x => x.id), [2])
  const markup = renderToString(createElement(AccountTable, { items: records, currentId: 1 }))
  assert.match(markup, /aria-label="Khóa alpha" disabled/)
  assert.match(markup, /aria-label="Xóa alpha" disabled/)
  assert.match(markup, /beta@example.com/)
  const roles = [{ id: 2, name: 'Customer' }]
  const create = renderToString(createElement(AccountForm, { roles }))
  assert.match(create, /name="password"/)
  assert.match(create, /name="confirmPassword"/)
  const edit = renderToString(createElement(AccountForm, { roles, account: records[0], currentId: 1 }))
  assert.doesNotMatch(edit, /name="password"/)
  assert.match(edit, /name="roleId"[^>]*disabled/)
  assert.doesNotMatch(readFileSync(new URL('../src/pages/admin/AccountManagement.css', import.meta.url), 'utf8'),
    /#[0-9a-f]{3,8}\b|rgba?\(/i)
  const { api } = await server.ssrLoadModule('/src/services/api.js')
  const originalFetch = globalThis.fetch
  const requests = []
  try {
    globalThis.fetch = async (url, options) => { requests.push({ url, ...options }); return Response.json({}) }
    await api.admin.users(); assert.ok(requests.at(-1).url.endsWith('/api/users'))
    await api.admin.userRoles(); assert.ok(requests.at(-1).url.endsWith('/api/users/roles'))
    await api.admin.createUser({ username: 'test' }); assert.equal(requests.at(-1).method, 'POST')
    await api.admin.saveUser(2, { fullName: 'Changed' }); assert.equal(requests.at(-1).method, 'PUT')
    await api.admin.userStatus(2, false); assert.equal(requests.at(-1).body, '{"isActive":false}')
    await api.admin.deleteUser(2); assert.equal(requests.at(-1).method, 'DELETE')
  } finally { globalThis.fetch = originalFetch }
  console.log('PASS: Account rendering, filters, permissions UI, forms, API requests and palette.')
} finally { await server.close() }
