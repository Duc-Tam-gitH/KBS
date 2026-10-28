import { useState } from 'react'
import { Eye, EyeOff } from 'lucide-react'

export default function Register({ onRegister, onNavigate }) {
  const [formData, setFormData] = useState({
    fullName: '',
    username: '',
    email: '',
    password: '',
    confirmPassword: '',
  })
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [showPassword, setShowPassword] = useState(false)
  const [showConfirmPassword, setShowConfirmPassword] = useState(false)

  const handleChange = (event) => {
    setFormData({ ...formData, [event.target.name]: event.target.value })
  }

  const handleSubmit = async (event) => {
    event.preventDefault()
    setError('')

    if (formData.password !== formData.confirmPassword) {
      setError('Mật khẩu xác nhận không khớp.')
      return
    }

    setIsSubmitting(true)

    try {
      await onRegister(formData)
      onNavigate('auth')
    } catch (submitError) {
      setError(submitError.message || 'Không thể tạo tài khoản.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="auth-container">
      <div className="auth-box">
        <p className="eyebrow">KBS / YOUR SPACE</p>
        <h1 className="auth-title">Đăng ký tài khoản</h1>
        <p className="auth-subtitle">Tạo tài khoản của bạn</p>

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <input
              type="text"
              name="fullName"
              className="auth-input"
              placeholder="Họ và tên" aria-label="Họ và tên"
              value={formData.fullName}
              onChange={handleChange}
            />
          </div>
          <div className="form-group">
            <input
              type="text"
              name="username"
              className="auth-input"
              placeholder="Tên đăng nhập" aria-label="Tên đăng nhập"
              value={formData.username}
              onChange={handleChange}
              required
              minLength="3"
            />
          </div>
          <div className="form-group">
            <input
              type="email"
              name="email"
              className="auth-input"
              placeholder="Địa chỉ email" aria-label="Địa chỉ email"
              value={formData.email}
              onChange={handleChange}
              required
            />
          </div>
          <div className="form-group auth-password-field">
            <input
              type={showPassword ? 'text' : 'password'}
              name="password"
              aria-describedby="register-password-help"
              className="auth-input"
              placeholder="Mật khẩu (tối thiểu 8 ký tự chứa chữ cái và số)" aria-label="Mật khẩu (tối thiểu 8 ký tự chứa chữ cái và số)"
              value={formData.password}
              onChange={handleChange}
              required
              minLength="8"
            />
            <button
              type="button"
              className="auth-password-toggle"
              aria-label={showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'}
              onClick={() => setShowPassword((visible) => !visible)}
            >
              {showPassword ? <EyeOff size={20} aria-hidden="true" /> : <Eye size={20} aria-hidden="true" />}
            </button>
          </div>
          <p id="register-password-help" className="auth-password-help">
            Mật khẩu phải có ít nhất 8 ký tự, gồm chữ cái (A–Z, a–z) và số.
          </p>
          <div className="form-group auth-password-field">
            <input
              type={showConfirmPassword ? 'text' : 'password'}
              name="confirmPassword"
              className="auth-input"
              placeholder="Xác nhận mật khẩu" aria-label="Xác nhận mật khẩu"
              value={formData.confirmPassword}
              onChange={handleChange}
              required
              minLength="8"
            />
            <button
              type="button"
              className="auth-password-toggle"
              aria-label={showConfirmPassword ? 'Ẩn mật khẩu xác nhận' : 'Hiện mật khẩu xác nhận'}
              onClick={() => setShowConfirmPassword((visible) => !visible)}
            >
              {showConfirmPassword ? <EyeOff size={20} aria-hidden="true" /> : <Eye size={20} aria-hidden="true" />}
            </button>
          </div>

          <button type="submit" className="auth-button" disabled={isSubmitting}>
            {isSubmitting ? 'Đang tạo tài khoản…' : 'Đăng ký'}
          </button>
        </form>

        {error && <div className="auth-error" role="alert">{error}</div>}

        <button type="button" className="auth-link" onClick={() => onNavigate('auth')}>
          Đã có tài khoản? Đăng nhập
        </button>
      </div>
    </div>
  )
}
