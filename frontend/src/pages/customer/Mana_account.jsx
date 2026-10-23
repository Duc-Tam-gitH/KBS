import { useRef, useState } from 'react'
import {
  ArrowLeft, ArrowRight, Check, Eye, EyeOff, Heart, LockKeyhole,
  LogOut, Mail, Package, Phone, Save, ShieldCheck, UserRound,
  Edit3, XCircle, ChevronRight
} from 'lucide-react'

export default function ManaAccount({ user, onNavigate, onLogout, onSaveProfile, onChangePassword }) {
  const [section, setSection] = useState('profile')
  const [editing, setEditing] = useState(false)
  const [showPasswords, setShowPasswords] = useState(false)
  const [saving, setSaving] = useState(false)
  const [notice, setNotice] = useState(null)
  const submitting = useRef(false)

  if (!user) {
    return (
      <section className="discord-account-page discord-empty-state">
        <UserRound size={48} aria-hidden="true" />
        <h1>Tài khoản của bạn</h1>
        <p>Đăng nhập để xem và quản lý thông tin cá nhân.</p>
        <button type="button" className="discord-btn-primary" onClick={() => onNavigate?.('auth')}>
          Đăng nhập
        </button>
      </section>
    )
  }

  const displayName = user.fullName || user.username
  const initials = displayName.trim().split(/\s+/).slice(-2).map((part) => part[0]).join('').toUpperCase()
  
  const navigateSection = (next) => {
    setSection(next)
    setEditing(false)
    setShowPasswords(false)
    setNotice(null)
  }

  const submit = async (event, kind) => {
    event.preventDefault()
    const handler = kind === 'profile' ? onSaveProfile : onChangePassword
    if (!handler || submitting.current) return
    
    const form = event.currentTarget
    const values = Object.fromEntries(new FormData(form))
    
    if (kind === 'password' && values.newPassword !== values.confirmNewPassword) {
      setNotice({ error: true, text: 'Mật khẩu xác nhận chưa khớp. Vui lòng kiểm tra lại.' })
      return
    }
    
    submitting.current = true
    setSaving(true)
    setNotice(null)
    
    try {
      const result = await handler(values)
      if (result === false) throw new Error('Chưa lưu được thay đổi. Vui lòng thử lại.')
      if (kind === 'password') form.reset()
      
      setEditing(false)
      setShowPasswords(false)
      setNotice({ text: kind === 'profile' ? 'Đã cập nhật thông tin cá nhân.' : 'Đã đổi mật khẩu thành công.' })
    } catch (error) {
      setNotice({ error: true, text: error.message || 'Không thể lưu thay đổi. Vui lòng thử lại.' })
    } finally {
      submitting.current = false
      setSaving(false)
    }
  }

  return (
    <section className="discord-account-page">
      {/* 1. Banner & Avatar (Giống khung Discord) */}
      <div className="discord-profile-banner">
        <div className="discord-avatar-wrapper">
          {/* Nếu user có trường avatar (url ảnh) thì dùng, không thì hiển thị chữ cái đầu */}
          <div className="discord-avatar">
            {user.avatar ? (
              <img src={user.avatar} alt={displayName} />
            ) : (
              <span>{initials}</span>
            )}
          </div>
        </div>
      </div>

      <div className="discord-content-container">
        {/* 2. Thông tin cơ bản */}
        <header className="discord-profile-header">
          <div>
            <h1>{displayName}</h1>
            <p className="discord-username">@{user.username}</p>
            <p className="discord-email"><Mail size={14} /> {user.email}</p>
          </div>
        </header>

        {/* Thanh thông báo */}
        {notice && (
          <div className={`discord-notice ${notice.error ? 'is-error' : 'is-success'}`} role={notice.error ? 'alert' : 'status'}>
            {!notice.error && <Check size={18} aria-hidden="true" />} {notice.text}
          </div>
        )}

        {/* 3. Khu vực chức năng (Card Layout như Discord) */}
        <div className="discord-menu-card">
          
          {/* Nút chỉnh sửa hồ sơ */}
          {section === 'profile' && !editing && (
            <button type="button" className="discord-menu-item" onClick={() => setEditing(true)}>
              <div className="discord-menu-item-left">
                <Edit3 size={18} />
                <span>Sửa Hồ Sơ</span>
              </div>
              <ChevronRight size={18} className="discord-chevron" />
            </button>
          )}

          {/* Nút chuyển sang bảo mật */}
          {section === 'profile' && !editing && (
            <button type="button" className="discord-menu-item" onClick={() => navigateSection('security')}>
              <div className="discord-menu-item-left">
                <LockKeyhole size={18} />
                <span>Mật khẩu & Bảo mật</span>
              </div>
              <ChevronRight size={18} className="discord-chevron" />
            </button>
          )}

          {/* Form chỉnh sửa hồ sơ (Hiện ra khi bấm Edit) */}
          {section === 'profile' && editing && (
            <div className="discord-form-wrapper">
              <div className="discord-form-header">
                <h2>Chỉnh sửa hồ sơ</h2>
                <button type="button" className="discord-close-btn" onClick={() => setEditing(false)}>
                  <XCircle size={20} />
                </button>
              </div>
              <form className="discord-form" onSubmit={(e) => submit(e, 'profile')}>
                <label>
                  <span>Tên đăng nhập (Không thể đổi)</span>
                  <input value={user.username || ''} readOnly disabled />
                </label>
                <label>
                  <span>Họ và tên</span>
                  <input name="fullName" defaultValue={user.fullName || ''} disabled={saving} placeholder="Nhập họ và tên" />
                </label>
                <label>
                  <span>Email</span>
                  <input name="email" type="email" required defaultValue={user.email || ''} disabled={saving} placeholder="ban@example.com" />
                </label>
                <label>
                  <span>Số điện thoại</span>
                  <input name="phoneNumber" type="tel" defaultValue={user.phoneNumber || ''} disabled={saving} placeholder="Chưa cập nhật" />
                </label>
                
                <div className="discord-form-actions">
                  <button type="button" className="discord-btn-secondary" disabled={saving} onClick={() => setEditing(false)}>
                    Hủy
                  </button>
                  <button type="submit" className="discord-btn-primary" disabled={saving || !onSaveProfile}>
                    <Save size={18} /> {saving ? 'Đang lưu…' : 'Lưu thay đổi'}
                  </button>
                </div>
              </form>
            </div>
          )}

          {/* Form đổi mật khẩu (Section security) */}
          {section === 'security' && (
            <div className="discord-form-wrapper">
              <div className="discord-form-header">
                <h2>Đổi mật khẩu</h2>
                <button type="button" className="discord-close-btn" onClick={() => navigateSection('profile')}>
                  <ArrowLeft size={20} />
                </button>
              </div>
              <form className="discord-form" onSubmit={(e) => submit(e, 'password')}>
                <label>
                  <span>Mật khẩu hiện tại</span>
                  <input name="currentPassword" required disabled={saving} type={showPasswords ? 'text' : 'password'} />
                </label>
                <label>
                  <span>Mật khẩu mới</span>
                  <input name="newPassword" required minLength={8} pattern="(?=.*[A-Za-z])(?=.*\d).{8,}" disabled={saving} type={showPasswords ? 'text' : 'password'} />
                </label>
                <label>
                  <span>Xác nhận mật khẩu mới</span>
                  <input name="confirmNewPassword" required minLength={8} disabled={saving} type={showPasswords ? 'text' : 'password'} />
                </label>
                <button type="button" className="discord-password-toggle" onClick={() => setShowPasswords(!showPasswords)}>
                  {showPasswords ? <EyeOff size={16} /> : <Eye size={16} />}
                  {showPasswords ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'}
                </button>
                <div className="discord-form-actions">
                  <button type="submit" className="discord-btn-primary" disabled={saving || !onChangePassword}>
                    <ShieldCheck size={18} /> {saving ? 'Đang cập nhật…' : 'Cập nhật mật khẩu'}
                  </button>
                </div>
              </form>
            </div>
          )}
        </div>

        {/* 4. Các nút liên kết phụ (Đơn hàng, Yêu thích) */}
        <div className="discord-menu-card">
          <button type="button" className="discord-menu-item" onClick={() => onNavigate?.('orders')}>
            <div className="discord-menu-item-left">
              <Package size={18} />
              <span>Đơn hàng của tôi</span>
            </div>
            <ChevronRight size={18} className="discord-chevron" />
          </button>
          
          <button type="button" className="discord-menu-item" onClick={() => onNavigate?.('likes')}>
            <div className="discord-menu-item-left">
              <Heart size={18} />
              <span>Sản phẩm yêu thích</span>
            </div>
            <ChevronRight size={18} className="discord-chevron" />
          </button>
        </div>

        {/* 5. Nút Đăng xuất (Nằm cuối cùng) */}
        <div className="discord-menu-card discord-logout-card">
          <button type="button" className="discord-menu-item discord-logout-item" disabled={saving} onClick={onLogout}>
            <div className="discord-menu-item-left">
              <LogOut size={18} />
              <span>Đăng xuất</span>
            </div>
            <ChevronRight size={18} className="discord-chevron" />
          </button>
        </div>
        
        <div className="discord-footer-nav">
          <button type="button" className="discord-back-link" onClick={() => onNavigate?.('shop')}>
            <ArrowLeft size={16} /> Tiếp tục mua sắm
          </button>
        </div>
      </div>
    </section>
  )
}