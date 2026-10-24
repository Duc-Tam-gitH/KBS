import './Account.css'
import { useRef, useState } from 'react'
import {
  ArrowLeft, Check, Eye, EyeOff, Heart, LockKeyhole,
  LogOut, Mail, Package, Save, ShieldCheck, UserRound,
  Edit3, XCircle, ChevronRight
} from 'lucide-react'

export default function Account({ user, onNavigate, onLogout, onSaveProfile, onChangePassword }) {
  const [section, setSection] = useState('profile')
  const [editing, setEditing] = useState(false)
  const [showPasswords, setShowPasswords] = useState(false)
  const [saving, setSaving] = useState(false)
  const [notice, setNotice] = useState(null)
  const submitting = useRef(false)

  if (!user) {
    return (
      <section className="account-page account-empty-state">
        <UserRound size={48} aria-hidden="true" />
        <h1>Tài khoản của bạn</h1>
        <p>Đăng nhập để xem và quản lý thông tin cá nhân.</p>
        <button type="button" className="account-btn-primary" onClick={() => onNavigate?.('auth')}>
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
    <section className="account-page">
      {/* 1. Banner & Avatar (Giống khung Discord) */}
      <div className="account-profile-banner">
        <div className="account-avatar-wrapper">
          {/* Nếu user có trường avatar (url ảnh) thì dùng, không thì hiển thị chữ cái đầu */}
          <div className="account-avatar">
            {user.avatar ? (
              <img src={user.avatar} alt={displayName} />
            ) : (
              <span>{initials}</span>
            )}
          </div>
        </div>
      </div>

      <div className="account-content-container">
        {/* 2. Thông tin cơ bản */}
        <header className="account-profile-header">
          <div>
            <h1>{displayName}</h1>
            <p className="account-username">@{user.username}</p>
            <p className="account-email"><Mail size={14} /> {user.email}</p>
          </div>
        </header>

        {/* Thanh thông báo */}
        {notice && (
          <div className={`account-notice ${notice.error ? 'is-error' : 'is-success'}`} role={notice.error ? 'alert' : 'status'}>
            {!notice.error && <Check size={18} aria-hidden="true" />} {notice.text}
          </div>
        )}

        {/* 3. Khu vực chức năng (Card Layout như Discord) */}
        <div className="account-menu-card">
          
          {/* Nút chỉnh sửa hồ sơ */}
          {section === 'profile' && !editing && (
            <button type="button" className="account-menu-item" onClick={() => setEditing(true)}>
              <div className="account-menu-item-left">
                <Edit3 size={18} />
                <span>Sửa Hồ Sơ</span>
              </div>
              <ChevronRight size={18} className="account-chevron" />
            </button>
          )}

          {/* Nút chuyển sang bảo mật */}
          {section === 'profile' && !editing && (
            <button type="button" className="account-menu-item" onClick={() => navigateSection('security')}>
              <div className="account-menu-item-left">
                <LockKeyhole size={18} />
                <span>Mật khẩu & Bảo mật</span>
              </div>
              <ChevronRight size={18} className="account-chevron" />
            </button>
          )}

          {/* Form chỉnh sửa hồ sơ (Hiện ra khi bấm Edit) */}
          {section === 'profile' && editing && (
            <div className="account-form-wrapper">
              <div className="account-form-header">
                <h2>Chỉnh sửa hồ sơ</h2>
                <button type="button" className="account-close-btn" aria-label="Đóng chỉnh sửa hồ sơ"
                  disabled={saving} onClick={() => setEditing(false)}>
                  <XCircle size={20} />
                </button>
              </div>
              <form className="account-form" onSubmit={(e) => submit(e, 'profile')}>
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
                
                <div className="account-form-actions">
                  <button type="button" className="account-btn-secondary" disabled={saving} onClick={() => setEditing(false)}>
                    Hủy
                  </button>
                  <button type="submit" className="account-btn-primary" disabled={saving || !onSaveProfile}>
                    <Save size={18} /> {saving ? 'Đang lưu…' : 'Lưu thay đổi'}
                  </button>
                </div>
              </form>
            </div>
          )}

          {/* Form đổi mật khẩu (Section security) */}
          {section === 'security' && (
            <div className="account-form-wrapper">
              <div className="account-form-header">
                <h2>Đổi mật khẩu</h2>
                <button type="button" className="account-close-btn" aria-label="Quay lại hồ sơ"
                  disabled={saving} onClick={() => navigateSection('profile')}>
                  <ArrowLeft size={20} />
                </button>
              </div>
              <form className="account-form" onSubmit={(e) => submit(e, 'password')}>
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
                <button type="button" className="account-password-toggle" onClick={() => setShowPasswords(!showPasswords)}>
                  {showPasswords ? <EyeOff size={16} /> : <Eye size={16} />}
                  {showPasswords ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'}
                </button>
                <div className="account-form-actions">
                  <button type="submit" className="account-btn-primary" disabled={saving || !onChangePassword}>
                    <ShieldCheck size={18} /> {saving ? 'Đang cập nhật…' : 'Cập nhật mật khẩu'}
                  </button>
                </div>
              </form>
            </div>
          )}
        </div>

        {/* 4. Các nút liên kết phụ (Đơn hàng, Yêu thích) */}
        <div className="account-menu-card">
          <button type="button" className="account-menu-item" onClick={() => onNavigate?.('orders')}>
            <div className="account-menu-item-left">
              <Package size={18} />
              <span>Đơn hàng của tôi</span>
            </div>
            <ChevronRight size={18} className="account-chevron" />
          </button>
          
          <button type="button" className="account-menu-item" onClick={() => onNavigate?.('likes')}>
            <div className="account-menu-item-left">
              <Heart size={18} />
              <span>Sản phẩm yêu thích</span>
            </div>
            <ChevronRight size={18} className="account-chevron" />
          </button>
        </div>

        {/* 5. Nút Đăng xuất (Nằm cuối cùng) */}
        <div className="account-menu-card account-logout-card">
          <button type="button" className="account-menu-item account-logout-item" disabled={saving} onClick={onLogout}>
            <div className="account-menu-item-left">
              <LogOut size={18} />
              <span>Đăng xuất</span>
            </div>
            <ChevronRight size={18} className="account-chevron" />
          </button>
        </div>
        
        <div className="account-footer-nav">
          <button type="button" className="account-back-link" onClick={() => onNavigate?.('shop')}>
            <ArrowLeft size={16} /> Tiếp tục mua sắm
          </button>
        </div>
      </div>
    </section>
  )
}
