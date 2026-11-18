import { useEffect, useState } from 'react'
import { MessageCircle, Search, ShoppingBag, UserRound, PackageSearch, ArrowRight } from 'lucide-react'
import { api } from '../../services/api'
import { ChatThread } from '../Staff/SupportChat'
import { useStaffAction } from '../Staff/staffHooks'
import { StaffNotice } from '../Staff/StaffState'
import '../Staff/Staff.css'
import '../Staff/SupportChat.css'
import './SupportBot.css'

const suggestions = [
  { label: 'Xem danh mục sản phẩm', route: 'shop', Icon: ShoppingBag },
  { label: 'Tìm sản phẩm', route: 'search', Icon: Search },
  { label: 'Giỏ hàng / thanh toán', route: 'cart', Icon: PackageSearch },
  { label: 'Theo dõi đơn hàng', route: 'orders', Icon: ArrowRight },
  { label: 'Tài khoản của tôi', route: 'account', Icon: UserRound },
]

export default function SupportBot({ user, onNavigate }) {
  const [conversations, setConversations] = useState(null)
  const [requestedId, setRequestedId] = useState(null)
  const [error, setError] = useState('')
  const [version, setVersion] = useState(0)
  const action = useStaffAction()

  useEffect(() => {
    let active = true
    let timer
    const poll = async () => {
      try {
        if (!document.hidden) {
          const data = await api.chat.conversations()
          if (active) {
            setConversations(data)
            if (data[0]) setRequestedId(data[0].id)
            setError('')
          }
        }
      } catch (err) {
        if (active) setError(err.message || 'Không thể tải trạng thái hỗ trợ.')
      } finally {
        if (active) timer = setTimeout(poll, 5000)
      }
    }
    poll()
    return () => { active = false; clearTimeout(timer) }
  }, [version])

  const conversation = conversations?.[0]
  const requestSupport = () => action.run(async () => {
    const opened = await api.chat.open()
    setRequestedId(opened.id)
    setVersion(value => value + 1)
  }, 'Đã gửi yêu cầu. Vui lòng chờ cửa hàng xác nhận.')

  return <div className="staff-workspace sf-customer-chat sf-support-bot">
    <header className="sf-heading">
      <div><p className="sf-bot-eyebrow">KBS / HỖ TRỢ</p><h1>Trợ lý KBS</h1></div>
    </header>
    <section className="sf-panel sf-bot-panel" aria-label="Trợ lý hỗ trợ">
      <div className="sf-bot-greeting">
        <span className="sf-bot-avatar" aria-hidden="true"><MessageCircle size={20} /></span>
        <div><strong>Xin chào{user.fullName ? ` ${user.fullName}` : ''}!</strong>
          <p>Bạn cần hỗ trợ việc gì? Chọn một gợi ý để mở nhanh trang phù hợp.</p></div>
      </div>
      <div className="sf-bot-suggestions">
        {suggestions.map(({ label, route, Icon }) => <button key={route} type="button"
          className="sf-bot-suggestion" onClick={() => onNavigate(route)}>
          <Icon size={18} aria-hidden="true" /><span>{label}</span><ArrowRight size={16} aria-hidden="true" />
        </button>)}
      </div>
      <div className="sf-bot-human">
        <div><h2>Cần nhân viên hỗ trợ?</h2>
          <p>Gửi yêu cầu để trò chuyện trực tiếp với cửa hàng sau khi admin xác nhận.</p></div>
        {!conversation && !requestedId && <button type="button" className="sf-primary"
          disabled={action.busy || conversations === null} onClick={requestSupport}>
          {action.busy ? 'Đang gửi…' : 'Yêu cầu chat với shop'}
        </button>}
        {requestedId && !conversation && <p role="status">Đang chờ cửa hàng xác nhận yêu cầu chat…</p>}
      </div>
      <StaffNotice notice={action.notice} />
      {error && <p className="sf-notice" role="alert">{error}
        <button type="button" onClick={() => setVersion(value => value + 1)}>Thử lại</button>
      </p>}
      {conversation && !conversation.staffId && <p className="sf-notice" role="status">
        Yêu cầu chat đã được gửi. Bạn có thể trò chuyện sau khi admin chấp nhận.
      </p>}
      {conversation?.staffId && <>
        <p className="sf-notice" role="status">
          Cửa hàng đã xác nhận. Bạn đang trò chuyện với {conversation.staffName || 'shop'}.
        </p>
        <ChatThread key={conversation.id} conversation={conversation} user={user} onRead={() => {
          setVersion(value => value + 1)
        }} />
      </>}
    </section>
  </div>
}
