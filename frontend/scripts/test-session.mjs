import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'

const source = readFileSync(new URL('../src/services/api.js', import.meta.url), 'utf8')
  .replace('import.meta.env.VITE_API_URL', '"http://localhost:5231"')
let loadId = 0
const load = () => import(`data:text/javascript;base64,${Buffer.from(source + `\n// ${loadId++}`).toString('base64')}`)
const values = new Map()
globalThis.sessionStorage = {
  getItem: key => values.get(key) ?? null,
  setItem: (key, value) => values.set(key, value),
  removeItem: key => values.delete(key),
}
const token = exp => `header.${Buffer.from(JSON.stringify({ exp })).toString('base64url')}.signature`
const session = { accessToken: token(Date.now() / 1000 + 3600), user: { id: 1, roleName: 'Admin' } }
let { authStore, api } = await load()
authStore.setSession(session)
;({ authStore, api } = await load()) // New module instance simulates reload.
assert.deepEqual(authStore.user, session.user)
assert.equal(authStore.token, session.accessToken)
let captured
globalThis.fetch = async (url, options) => {
  captured = { url: new URL(url), options }
  return Response.json({ items: [], nextBeforeId: null })
}
const likesController = new AbortController()
await api.likelist(likesController.signal)
assert.equal(captured.options.signal, likesController.signal)
likesController.abort()
assert.equal(captured.options.signal.aborted, true)
await api.admin.aiEvents('ai_auto', 42, undefined, {
  kind: 'CONFIRMED', from: '2026-10-30T08:00', to: '2026-10-30T09:00',
})
assert.equal(captured.options.headers.get('Authorization'), `Bearer ${session.accessToken}`)
assert.equal(captured.url.searchParams.get('kind'), 'CONFIRMED')
assert.equal(captured.url.searchParams.get('moduleId'), 'ai_auto')
assert.equal(captured.url.searchParams.get('beforeId'), '42')
assert.equal(captured.url.searchParams.get('from'), new Date('2026-10-30T08:00').toISOString())
assert.equal(captured.url.searchParams.get('to'), new Date('2026-10-30T09:00').toISOString())
authStore.clear()
assert.equal((await load()).authStore.user, null)
authStore.setSession({ ...session, accessToken: token(Date.now() / 1000 - 60) })
assert.equal((await load()).authStore.token, null)
assert.equal(values.size, 0)
values.set('kbs.auth.session', '{broken')
assert.equal((await load()).authStore.user, null)
authStore.setSession(session)
globalThis.fetch = async () => Response.json({ message: 'Expired' }, { status: 401 })
await assert.rejects(api.myOrders())
assert.equal(values.size, 0)
globalThis.sessionStorage = { getItem() { throw new Error('Blocked') }, setItem() { throw new Error('Blocked') }, removeItem() { throw new Error('Blocked') } }
;({ authStore } = await load())
authStore.setSession(session)
assert.equal(authStore.token, session.accessToken)
authStore.clear()
console.log('PASS: reload, logout, expired/corrupt session, 401, blocked storage, and AI filter query.')
