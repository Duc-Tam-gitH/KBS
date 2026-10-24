import { useEffect, useRef, useState } from 'react'
import { api } from '../../services/api'
import { dateTime, useStaffAction } from './staffHooks'
import { StaffNotice } from './StaffState'
import './Staff.css'
import './SupportChat.css'

export default function SupportChat({ user, initialConversationId = null }) {
  const [conversations, setConversations] = useState(null)
  const [error, setError] = useState('')
  const [version, setVersion] = useState(0)
  const [selectedId, setSelectedId] = useState(initialConversationId)
  const [query, setQuery] = useState('')
  const [unreadOnly, setUnreadOnly] = useState(false)
  const action = useStaffAction()
  const isCustomer = user?.roleName === 'Customer'
  useEffect(() => {
    let active = true
    let timer
    const poll = async () => {
      try {
        if (!document.hidden) {
          const data = await api.chat.conversations()
          if (active) { setConversations(data); setError('') }
        }
      } catch (err) {
        if (active) setError(err.message || 'Không thể tải hội thoại.')
      } finally {
        if (active) timer = setTimeout(poll, 5000)
      }
    }
    poll()
    return () => { active = false; clearTimeout(timer) }
  }, [version])

  const choose = (conversation) => action.run(async () => {
    if (!isCustomer && !conversation.staffId) await api.chat.claim(conversation.id)
    setSelectedId(conversation.id)
    setVersion((value) => value + 1)
  }, '')
  const start = () => action.run(async () => {
    const conversation = await api.chat.open()
    setSelectedId(conversation.id)
    setVersion((value) => value + 1)
  }, '')
  const selected = conversations?.find((conversation) => conversation.id === selectedId)
  const filtered = (conversations || []).filter((conversation) => (!unreadOnly || conversation.unread > 0)
    && conversation.customerName.toLocaleLowerCase('vi').includes(query.trim().toLocaleLowerCase('vi')))
  return <div className={isCustomer ? 'staff-workspace sf-customer-chat' : 'sf-chat-page'}>
    <header className="sf-heading"><h1>Hỗ trợ & Chat</h1>
      <button type="button" disabled={action.busy} onClick={() => setVersion((value) => value + 1)}>Làm mới</button>
    </header>
    <StaffNotice notice={action.notice} />
    {error && <p className="sf-notice" role="alert">{error}</p>}
    {!conversations && !error && <p role="status">Đang tải hội thoại…</p>}
    {conversations && <div className="sf-chat-layout">
      <aside className="sf-panel sf-conversation-list">
        {!isCustomer && <input aria-label="Tìm hội thoại theo khách" placeholder="Tên khách hàng"
          value={query} onChange={(event) => setQuery(event.target.value)} />}
        <label className="sf-checkbox"><input type="checkbox" checked={unreadOnly}
          onChange={(event) => setUnreadOnly(event.target.checked)} /> Chưa đọc</label>
        {!filtered.length && <p>Không có hội thoại phù hợp.</p>}
        {isCustomer && !conversations.length && <button type="button" className="sf-primary"
          disabled={action.busy} onClick={start}>Bắt đầu trò chuyện</button>}
        {filtered.map((conversation) => <button className="sf-conversation" type="button" key={conversation.id}
          disabled={action.busy} aria-pressed={selectedId === conversation.id} onClick={() => choose(conversation)}>
          <strong>
            {isCustomer ? conversation.staffName || 'Chờ nhân viên tiếp nhận' : conversation.customerName}
          </strong>
          <span>{conversation.lastMessage || 'Chưa có tin nhắn.'}</span>
          <small>{dateTime(conversation.lastMessageAt)} · {conversation.unread} chưa đọc</small>
          {!isCustomer && !conversation.staffId && <small>Tiếp nhận hội thoại</small>}
        </button>)}
      </aside>
      {selected ? <ChatThread key={selected.id} conversation={selected} user={user}
        onRead={() => setVersion((value) => value + 1)} />
        : <div className="sf-panel sf-state">Chọn hội thoại để bắt đầu.</div>}
    </div>}
  </div>
}

function ChatThread({ conversation, user, onRead }) {
  const [messages, setMessages] = useState(null)
  const [error, setError] = useState('')
  const [draft, setDraft] = useState('')
  const [hasOlder, setHasOlder] = useState(false)
  const [olderBusy, setOlderBusy] = useState(false)
  const [version, setVersion] = useState(0)
  const latest = useRef(null)
  const readThrough = useRef(null)
  const readHandler = useRef(onRead)
  const bottom = useRef(null)
  const action = useStaffAction()
  useEffect(() => { readHandler.current = onRead }, [onRead])
  useEffect(() => {
    let active = true
    let timer
    const poll = async () => {
      try {
        if (!document.hidden) {
          const batch = await api.chat.messages(conversation.id, null, latest.current)
          if (!active) return
          if (latest.current === null) setHasOlder(batch.length === 100)
          if (batch.length) {
            latest.current = batch.at(-1).id
            setMessages((items) => [...new Map([...(items || []), ...batch].map((item) => [item.id, item])).values()]
              .sort((a, b) => a.id - b.id))
          } else {
            setMessages((items) => items || [])
          }
          if (latest.current && latest.current !== readThrough.current && !document.hidden) {
            const cursor = latest.current
            await api.chat.read(conversation.id, cursor)
            if (!active) return
            readThrough.current = cursor
            readHandler.current()
          }
          if (active) setError('')
        }
      } catch (err) {
        if (active) setError(err.message || 'Không thể tải tin nhắn.')
      } finally {
        if (active) timer = setTimeout(poll, 5000)
      }
    }
    poll()
    return () => { active = false; clearTimeout(timer) }
  }, [conversation.id, version])
  const lastId = messages?.at(-1)?.id
  useEffect(() => { bottom.current?.scrollIntoView({ block: 'nearest' }) }, [lastId])

  const older = async () => {
    if (olderBusy || !messages?.length) return
    setOlderBusy(true)
    try {
      const batch = await api.chat.messages(conversation.id, messages[0].id)
      setMessages((items) => [...new Map([...batch, ...items].map((item) => [item.id, item])).values()]
        .sort((a, b) => a.id - b.id))
      setHasOlder(batch.length === 100)
    } catch (err) {
      setError(err.message)
    } finally {
      setOlderBusy(false)
    }
  }
  const send = async (event) => {
    event.preventDefault()
    if (!draft.trim()) return
    if (await action.run(() => api.chat.send(conversation.id, draft.trim()), '')) {
      setDraft('')
      setVersion((value) => value + 1)
    }
  }
  return <section className="sf-panel sf-chat-thread" aria-label="Nội dung hội thoại">
    <h2>{user.roleName === 'Customer' ? conversation.staffName || 'Hỗ trợ khách hàng' : conversation.customerName}</h2>
    {error && <p role="alert">{error} <button type="button" onClick={() => setVersion((value) => value + 1)}>
      Thử lại</button></p>}
    <div className="sf-messages" role="log" aria-live="polite" aria-relevant="additions">
      {hasOlder && <button type="button" disabled={olderBusy} onClick={older}>Tải tin nhắn trước</button>}
      {!messages ? <p role="status">Đang tải tin nhắn…</p> : !messages.length && <p>Chưa có tin nhắn.</p>}
      {messages?.map((message) => <article key={message.id}
        className={`sf-message${message.senderId === user.id ? ' sf-message-own' : ''}`}>
        <strong>{message.senderName}</strong><p>{message.content}</p>
        <time dateTime={message.createdAt}>{dateTime(message.createdAt)}</time>
      </article>)}
      <div ref={bottom} />
    </div>
    <StaffNotice notice={action.notice} />
    <form className="sf-chat-composer" onSubmit={send}>
      <label>Tin nhắn<textarea value={draft} maxLength={4000} rows={3} required disabled={action.busy}
        onChange={(event) => setDraft(event.target.value)} onKeyDown={(event) => {
          if (event.key === 'Enter' && !event.shiftKey && !event.nativeEvent.isComposing) {
            event.preventDefault()
            event.currentTarget.form.requestSubmit()
          }
        }} /></label>
      <button type="submit" className="sf-primary" disabled={action.busy || !draft.trim()}>
        {action.busy ? 'Đang gửi…' : 'Gửi tin nhắn'}
      </button>
    </form>
  </section>
}
